package com.portfolio.connectedroom;

import android.annotation.SuppressLint;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCallback;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothProfile;
import android.content.Context;
import android.os.Build;
import android.os.Handler;
import android.util.Log;
import java.util.Arrays;
import java.util.UUID;

@SuppressLint("MissingPermission")
final class GattDoorConnection extends BluetoothGattCallback {
    private static final int READ_INTERVAL_MILLIS = 350;
    private static final int OPERATION_TIMEOUT_MILLIS = 20000;
    private final Handler handler;
    private final BridgeEvents events;
    private final UUID serviceUuid;
    private final UUID characteristicUuid;
    private BluetoothGatt gatt;
    private BluetoothGattCharacteristic characteristic;
    private byte[] previous;
    private boolean connected;
    private boolean busy;
    private Integer pendingWrite;
    private byte writingValue;
    private final Runnable watchdog;
    private final Runnable readNext;

    /** Configures one GATT connection, serialized operation loop and watchdog. */
    GattDoorConnection(Handler handler, BridgeEvents events, UUID service, UUID characteristic) {
        this.handler = handler;
        this.events = events;
        serviceUuid = service;
        characteristicUuid = characteristic;
        watchdog = () -> fail("Tiempo de espera GATT agotado. Reconecta el periferico.");
        readNext = this::read;
    }

    /** Starts an explicit LE connection whose callbacks run on the supplied handler. */
    void connect(Context context, BluetoothDevice device) {
        events.status("connecting", "Periferico encontrado. Conectando GATT...");
        gatt = device.connectGatt(context, false, this, BluetoothDevice.TRANSPORT_LE,
                BluetoothDevice.PHY_LE_1M_MASK, handler);
        if (gatt == null) fail("Android no pudo crear la conexion GATT.");
        else armWatchdog();
    }

    /** Starts discovery on connection and reports disconnection or revoked permissions. */
    @Override public void onConnectionStateChange(BluetoothGatt source, int status, int state) {
        if (source != gatt) return;
        if (status != BluetoothGatt.GATT_SUCCESS || state == BluetoothProfile.STATE_DISCONNECTED) {
            fail("Conexion BLE perdida (codigo " + status + ").");
        } else if (state == BluetoothProfile.STATE_CONNECTED) {
            try {
                if (!gatt.discoverServices()) fail("No se pudo iniciar el descubrimiento GATT.");
                else armWatchdog();
            } catch (SecurityException error) {
                fail("Se perdio el permiso Bluetooth durante el descubrimiento.");
            }
        }
    }

    /** Requires the configured Read and Write characteristic before starting the operation loop. */
    @Override public void onServicesDiscovered(BluetoothGatt source, int status) {
        if (source != gatt) return;
        if (status != BluetoothGatt.GATT_SUCCESS) {
            fail("Descubrimiento GATT fallo: " + status);
            return;
        }
        BluetoothGattService service = gatt.getService(serviceUuid);
        characteristic = service == null ? null : service.getCharacteristic(characteristicUuid);
        int required = BluetoothGattCharacteristic.PROPERTY_READ | BluetoothGattCharacteristic.PROPERTY_WRITE;
        if (characteristic == null || (characteristic.getProperties() & required) != required) {
            fail("El periferico debe exponer la caracteristica con Read y Write.");
            return;
        }
        read();
    }

    /** Routes the platform read callback while avoiding duplicate delivery across Android versions. */
    @Override public void onCharacteristicRead(BluetoothGatt source, BluetoothGattCharacteristic item,
            byte[] value, int status) {
        handleRead(source, value, status);
    }

    /** Routes the platform read callback while avoiding duplicate delivery across Android versions. */
    @SuppressWarnings("deprecation")
    @Override public void onCharacteristicRead(BluetoothGatt source, BluetoothGattCharacteristic item, int status) {
        // API 33+ dispatches both callbacks; only handle its immutable-value overload there.
        if (Build.VERSION.SDK_INT < 33) handleRead(source, item.getValue(), status);
    }

    /** Publishes changed incoming values and schedules the next serialized GATT operation. */
    private void handleRead(BluetoothGatt source, byte[] value, int status) {
        if (source != gatt) return;
        busy = false;
        handler.removeCallbacks(watchdog);
        if (status != BluetoothGatt.GATT_SUCCESS || value == null) {
            fail("Lectura GATT fallo: " + status + ". Revisa permisos Read del periferico.");
            return;
        }
        if (!connected) {
            connected = true;
            events.status("connected", "Conectado a Windows.");
        }
        if (!Arrays.equals(previous, value)) {
            events.value(value);
            previous = value.clone();
        }
        handler.postDelayed(readNext, pendingWrite == null ? READ_INTERVAL_MILLIS : 0);
    }

    /** Coalesces rapid peer commands while keeping at most one GATT operation in flight. */
    void requestWrite(int value) {
        if (!connected || value < 0 || value > 1) {
            fail("Comando no valido o BLE desconectado.");
            return;
        }
        // Coalesce rapid taps to the latest desired state, keeping at most one GATT operation in flight.
        pendingWrite = value;
        if (!busy) {
            handler.removeCallbacks(readNext);
            read();
        }
    }

    /** Acknowledges peer acceptance and resumes independent incoming reads. */
    @Override public void onCharacteristicWrite(BluetoothGatt source, BluetoothGattCharacteristic item, int status) {
        if (source != gatt) return;
        busy = false;
        handler.removeCallbacks(watchdog);
        if (status != BluetoothGatt.GATT_SUCCESS) { fail("Escritura GATT fallo: " + status); return; }
        events.acknowledged(writingValue);
        handler.post(readNext); // Reads carry independent commands addressed to this Android, not an echo of writes.
    }

    /** Uses the immutable-value Android API where available and the legacy API on older versions. */
    @SuppressWarnings("deprecation")
    private boolean write(byte value) {
        writingValue = value;
        byte[] payload = new byte[] { value };
        if (Build.VERSION.SDK_INT >= 33)
            return gatt.writeCharacteristic(characteristic, payload, BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT) == 0;
        characteristic.setWriteType(BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT);
        characteristic.setValue(payload);
        return gatt.writeCharacteristic(characteristic);
    }

    /** Runs the next queued write or incoming read under a bounded operation watchdog. */
    private void read() {
        if (gatt == null || characteristic == null || busy) return;
        try {
            busy = true;
            boolean started;
            if (pendingWrite != null) {
                byte value = pendingWrite.byteValue();
                pendingWrite = null;
                started = write(value);
            } else started = gatt.readCharacteristic(characteristic);
            if (!started) fail("Android rechazo la operacion GATT.");
            else armWatchdog();
        } catch (SecurityException error) {
            fail("Se perdio el permiso Bluetooth. Revisa Ajustes y reconecta.");
        }
    }

    /** Replaces the deadline for the currently outstanding GATT operation. */
    private void armWatchdog() {
        handler.removeCallbacks(watchdog);
        handler.postDelayed(watchdog, OPERATION_TIMEOUT_MILLIS);
    }

    /** Closes native resources before publishing a terminal connection explanation. */
    private void fail(String message) {
        close();
        events.status("error", message);
    }

    /** Cancels scheduled work and releases GATT while tolerating revoked Bluetooth permissions. */
    void close() {
        handler.removeCallbacks(watchdog);
        handler.removeCallbacks(readNext);
        BluetoothGatt previousGatt = gatt;
        gatt = null;
        characteristic = null;
        connected = false;
        busy = false;
        pendingWrite = null;
        previous = null;
        if (previousGatt != null) {
            try { previousGatt.disconnect(); }
            catch (SecurityException error) { Log.w("ConnectedRoomBLE", "Permission revoked while disconnecting", error); }
            finally {
                try { previousGatt.close(); }
                catch (SecurityException error) { Log.w("ConnectedRoomBLE", "Permission revoked while closing GATT", error); }
            }
        }
    }
}
