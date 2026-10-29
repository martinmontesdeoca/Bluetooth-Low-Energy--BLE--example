package com.portfolio.connectedroom;

import org.json.JSONException;
import org.json.JSONObject;
import java.util.concurrent.ArrayBlockingQueue;

/** Bounded queue: Unity drains it from its main thread; callbacks never touch Unity objects. */
final class BridgeEvents {
    private final ArrayBlockingQueue<String> messages = new ArrayBlockingQueue<>(128);

    /** Encodes a native connectivity transition for the managed adapter. */
    void status(String state, String message) {
        try {
            emit(new JSONObject().put("type", "status").put("state", state).put("message", message).toString());
        } catch (JSONException error) {
            throw new IllegalStateException("Cannot serialize BLE status", error);
        }
    }

    /** Encodes a received command as hexadecimal while preserving its binary meaning. */
    void value(byte[] value) {
        StringBuilder hex = new StringBuilder();
        for (byte item : value) {
            hex.append(Character.forDigit((item & 255) >>> 4, 16));
            hex.append(Character.forDigit(item & 15, 16));
        }
        emit("{\"type\":\"value\",\"hex\":\"" + hex + "\"}");
    }

    /** Enqueues output within a fixed capacity and latches an actionable overflow status. */
    private void emit(String message) {
        if (!messages.offer(message)) {
            messages.clear();
            messages.offer("{\"type\":\"status\",\"state\":\"error\",\"message\":\"Cola BLE saturada. Reconecta.\"}");
        }
    }

    /** Removes one event without blocking the Unity rendering thread. */
    String poll() { return messages.poll(); }
    /** Reports peer acceptance without echoing the command into the local door state. */
    void acknowledged(byte value) { emit("{\"type\":\"ack\",\"hex\":\"0" + value + "\"}"); }
    /** Discards events belonging to a cancelled or replaced native session. */
    void clear() { messages.clear(); }
}
