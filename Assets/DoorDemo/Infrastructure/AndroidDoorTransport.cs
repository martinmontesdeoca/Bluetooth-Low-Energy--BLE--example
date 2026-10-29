#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Linq;
using DoorDemo.Application;
using DoorDemo.Domain;
using UnityEngine;
using UnityEngine.Android;

namespace DoorDemo.Infrastructure
{
    /// <summary>JNI adapter; native callbacks queue JSON and Pump delivers it on the Unity thread.</summary>
    public sealed class AndroidDoorTransport : IDoorTransport, IDoorCommandWriter
    {
        private readonly string service;
        private readonly string characteristic;
        private AndroidJavaObject bridge;
        private PermissionCallbacks permissions;
        private int generation;
        private bool disposed;

        /// <summary>Validates the GATT identity without starting the Android radio or requesting permissions.</summary>
        public AndroidDoorTransport(string service, string characteristic)
        {
            this.service = Guid.Parse(service).ToString();
            this.characteristic = Guid.Parse(characteristic).ToString();
        }

        public event Action<byte[]> ValueReceived;
        public event Action<ConnectionStatus> StatusChanged;
        public event Action<DoorState> CommandAcknowledged;

        /// <summary>Requests platform permissions and ignores callbacks belonging to cancelled connection attempts.</summary>
        public void Connect()
        {
            int attempt = ++generation;
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            string[] required = version.GetStatic<int>("SDK_INT") >= 31
                ? new[] { "android.permission.BLUETOOTH_SCAN", "android.permission.BLUETOOTH_CONNECT" }
                : new[] { Permission.FineLocation };
            string[] missing = required.Where(permission => !Permission.HasUserAuthorizedPermission(permission)).ToArray();
            if (missing.Length == 0)
            {
                StartNative();
                return;
            }
            StatusChanged?.Invoke(new ConnectionStatus(ConnectionState.Searching, "Solicitando permisos Bluetooth..."));
            permissions = new PermissionCallbacks();
            bool scheduled = false;
            permissions.PermissionGranted += _ =>
            {
                if (!disposed && !scheduled && attempt == generation &&
                    required.All(Permission.HasUserAuthorizedPermission))
                {
                    scheduled = true;
                    StartNative();
                }
            };
            permissions.PermissionDenied += _ =>
            {
                if (attempt != generation || disposed) return;
                generation++;
                Error("Permiso Bluetooth denegado. Concedelo en Ajustes y vuelve a conectar.");
            };
            Permission.RequestUserPermissions(missing, permissions);
        }

        /// <summary>Creates the JNI bridge on demand and starts native service discovery.</summary>
        private void StartNative()
        {
            try
            {
                if (bridge == null)
                {
                    using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                    bridge = new AndroidJavaObject("com.portfolio.connectedroom.AndroidBleBridge", activity);
                }
                StatusChanged?.Invoke(new ConnectionStatus(ConnectionState.Searching, "Iniciando BLE nativo Android..."));
                bridge.Call("connect", service, characteristic);
            }
            catch (AndroidJavaException error) { Error("BLE Android: " + error.Message); }
        }

        /// <summary>Decodes a bounded batch of native events and delivers commands, status and acknowledgements in Unity.</summary>
        public void Pump()
        {
            if (bridge == null) return;
            try
            {
                for (int i = 0; i < 128; i++)
                {
                    string line = bridge.Call<string>("poll");
                    if (line == null) break;
                    BridgeMessage message = BridgeMessage.Parse(line);
                    if (message.type == "value") ValueReceived?.Invoke(message.ToPayload());
                    else if (message.type == "ack")
                    {
                        if (!DoorCommand.TryParse(message.ToPayload(), out DoorState state))
                            throw new FormatException("Confirmacion de comando invalida.");
                        CommandAcknowledged?.Invoke(state);
                    }
                    else StatusChanged?.Invoke(message.ToStatus());
                }
            }
            catch (Exception error) when (error is AndroidJavaException || error is FormatException || error is ArgumentException)
            {
                Error("Lectura del puente Android: " + error.Message);
            }
        }

        /// <summary>Publishes an explicit transport failure with context for the operator.</summary>
        private void Error(string message) => StatusChanged?.Invoke(new ConnectionStatus(ConnectionState.Error, message));

        /// <summary>Invokes the Java command writer for the peer door and reports JNI failures.</summary>
        public void WriteDoorState(DoorState state)
        {
            if (bridge == null) { Error("No hay una conexion BLE activa."); return; }
            try { bridge.Call("setDoor", state == DoorState.Open ? 1 : 0); }
            catch (AndroidJavaException error) { Error("Escritura BLE: " + error.Message); }
        }

        /// <summary>Invalidates pending permission callbacks and requests native GATT cleanup.</summary>
        public void Disconnect()
        {
            generation++;
            bridge?.Call("disconnect");
            StatusChanged?.Invoke(new ConnectionStatus(ConnectionState.Disconnected, "Desconectado"));
        }

        /// <summary>Releases the Java object even when native disposal reports an error.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            generation++;
            try { bridge?.Call("dispose"); }
            catch (AndroidJavaException error) { Error("Cierre BLE Android: " + error.Message); }
            finally
            {
                bridge?.Dispose();
                bridge = null;
                permissions = null;
            }
        }
    }
}
#endif
