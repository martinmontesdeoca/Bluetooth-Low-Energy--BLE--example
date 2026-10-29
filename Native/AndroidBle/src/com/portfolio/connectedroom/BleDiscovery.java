package com.portfolio.connectedroom;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.le.BluetoothLeScanner;
import android.bluetooth.le.ScanCallback;
import android.bluetooth.le.ScanFilter;
import android.bluetooth.le.ScanResult;
import android.bluetooth.le.ScanSettings;
import android.os.Handler;
import android.os.ParcelUuid;
import android.util.Log;
import java.util.Collections;
import java.util.UUID;

final class BleDiscovery {
    private static final int SCAN_TIMEOUT_MILLIS = 15000;
    interface Listener {
        /** Receives the matching peripheral after discovery has stopped. */
        void found(BluetoothDevice device);
        /** Receives an explicit scan failure or deadline explanation. */
        void failed(String message);
    }

    private final Handler handler;
    private BluetoothLeScanner scanner;
    private ScanCallback callback;
    private final Runnable timeout;

    /** Binds one discovery attempt to its handler and deadline listener. */
    BleDiscovery(Handler handler, Listener listener) {
        this.handler = handler;
        timeout = () -> {
            stop();
            listener.failed("No se encontro el servicio BLE en 15 segundos. Revisa el periferico.");
        };
    }

    /** Scans for the required service UUID and registers a bounded discovery deadline. */
    void start(BluetoothAdapter adapter, UUID service, Listener listener) {
        scanner = adapter.getBluetoothLeScanner();
        if (scanner == null) {
            listener.failed("Bluetooth no esta disponible. Enciende la radio.");
            return;
        }
        callback = new ScanCallback() {
            /** Selects the first matching result only while this callback still owns the scanner. */
            @Override public void onScanResult(int type, ScanResult result) {
                handler.post(() -> {
                    if (callback != this) return;
                    stop();
                    listener.found(result.getDevice());
                });
            }
            /** Reports an active scan failure without disrupting a newer GATT connection. */
            @Override public void onScanFailed(int code) {
                handler.post(() -> {
                    if (callback != this) return;
                    stop();
                    listener.failed("No se pudo escanear BLE: codigo " + code);
                });
            }
        };
        ScanFilter filter = new ScanFilter.Builder().setServiceUuid(new ParcelUuid(service)).build();
        scanner.startScan(Collections.singletonList(filter), new ScanSettings.Builder()
                .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY).build(), callback);
        handler.postDelayed(timeout, SCAN_TIMEOUT_MILLIS);
    }

    /** Detaches the active scan and deadline, tolerating a radio that became unavailable. */
    void stop() {
        handler.removeCallbacks(timeout);
        BluetoothLeScanner previousScanner = scanner;
        ScanCallback previousCallback = callback;
        callback = null;
        scanner = null;
        if (previousScanner != null && previousCallback != null) {
            try { previousScanner.stopScan(previousCallback); }
            catch (IllegalStateException | SecurityException error) {
                Log.w("ConnectedRoomBLE", "Radio unavailable while stopping discovery", error);
            }
        }
    }
}
