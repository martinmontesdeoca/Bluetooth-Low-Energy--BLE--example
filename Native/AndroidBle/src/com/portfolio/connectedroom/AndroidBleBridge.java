package com.portfolio.connectedroom;

import android.app.Activity;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothManager;
import android.content.Context;
import android.content.pm.PackageManager;
import android.location.LocationManager;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import java.util.UUID;

/** Public JNI entry point. All radio operations and callbacks are serialized on Android's main looper. */
public final class AndroidBleBridge {
    private final Activity activity;
    private final Handler handler = new Handler(Looper.getMainLooper());
    private final BridgeEvents events = new BridgeEvents();
    private BleDiscovery discovery;
    private GattDoorConnection connection;
    private int generation;

    /** Binds this JNI session to the foreground Activity and Android main looper. */
    public AndroidBleBridge(Activity activity) { this.activity = activity; }

    /** Validates radio prerequisites and starts a generation-scoped discovery attempt. */
    public void connect(String service, String characteristic) {
        handler.post(() -> {
            try {
                stop();
                events.clear();
                int attempt = generation;
                if (!activity.getPackageManager().hasSystemFeature(PackageManager.FEATURE_BLUETOOTH_LE))
                    throw new IllegalStateException("Este telefono no admite BLE.");
                BluetoothManager manager = (BluetoothManager) activity.getSystemService(Context.BLUETOOTH_SERVICE);
                BluetoothAdapter adapter = manager == null ? null : manager.getAdapter();
                if (adapter == null || !adapter.isEnabled())
                    throw new IllegalStateException("Enciende Bluetooth en el telefono y vuelve a conectar.");
                if (Build.VERSION.SDK_INT < 31) {
                    LocationManager location = (LocationManager) activity.getSystemService(Context.LOCATION_SERVICE);
                    if (location == null || (!location.isProviderEnabled(LocationManager.GPS_PROVIDER) &&
                            !location.isProviderEnabled(LocationManager.NETWORK_PROVIDER)))
                        throw new IllegalStateException("Android 8-11 necesita Ubicacion activada para escanear BLE.");
                }
                UUID serviceId = UUID.fromString(service);
                UUID characteristicId = UUID.fromString(characteristic);
                events.status("searching", "Buscando el servicio BLE del periferico...");
                BleDiscovery.Listener listener = new BleDiscovery.Listener() {
                    /** Connects only when the matching advertisement still belongs to this attempt. */
                    @Override public void found(BluetoothDevice device) {
                        if (attempt != generation) return;
                        try {
                            connection = new GattDoorConnection(handler, events, serviceId, characteristicId);
                            connection.connect(activity, device);
                        } catch (RuntimeException error) {
                            stop();
                            events.status("error", "Conexion GATT: " + error.toString());
                        }
                    }
                    /** Reports discovery failure only while this bridge still owns the attempt. */
                    @Override public void failed(String message) {
                        if (attempt == generation) events.status("error", message);
                    }
                };
                discovery = new BleDiscovery(handler, listener);
                discovery.start(adapter, serviceId, listener);
            } catch (RuntimeException error) {
                stop();
                events.status("error", error.getMessage() == null ? error.toString() : error.getMessage());
            }
        });
    }

    /** Returns one queued native event for the Unity main thread, or null when empty. */
    public String poll() { return events.poll(); }

    /** Queues a command addressed to the Windows peer on the Android radio thread. */
    public void setDoor(int value) {
        handler.post(() -> {
            if (connection == null) events.status("error", "Conecta BLE antes de enviar un comando.");
            else connection.requestWrite(value);
        });
    }

    /** Stops radio work and replaces stale events with a disconnected status. */
    public void disconnect() {
        handler.post(() -> {
            stop();
            events.clear();
            events.status("disconnected", "BLE desconectado");
        });
    }

    /** Releases radio resources on their owning looper. */
    public void dispose() { handler.post(this::stop); }

    /** Invalidates old callbacks and closes discovery and the active GATT session. */
    private void stop() {
        generation++;
        if (discovery != null) discovery.stop();
        discovery = null;
        if (connection != null) connection.close();
        connection = null;
    }
}
