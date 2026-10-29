using System;
using DoorDemo.Application;
using UnityEngine;

namespace DoorDemo.Infrastructure
{
    /// <summary>Version 1 native message contract shared by the Windows helper and Android JNI bridge.</summary>
    [Serializable]
    public sealed class BridgeMessage
    {
        public string type;
        public string state;
        public string message;
        public string hex;

        /// <summary>Validates native message types and normalizes JSON decoding failures to FormatException.</summary>
        public static BridgeMessage Parse(string line)
        {
            try
            {
                var result = JsonUtility.FromJson<BridgeMessage>(line);
                if (result == null || (result.type != "status" && result.type != "value" && result.type != "ack"))
                    throw new FormatException("Mensaje desconocido del puente BLE.");
                return result;
            }
            catch (ArgumentException error)
            {
                throw new FormatException("JSON invalido del puente BLE.", error);
            }
        }

        /// <summary>Maps known native status strings to the application connection contract.</summary>
        public ConnectionStatus ToStatus()
        {
            switch (state)
            {
                case "searching": return new ConnectionStatus(ConnectionState.Searching, message);
                case "connecting": return new ConnectionStatus(ConnectionState.Connecting, message);
                case "connected": return new ConnectionStatus(ConnectionState.Connected, message);
                case "disconnected": return new ConnectionStatus(ConnectionState.Disconnected, message);
                case "error": return new ConnectionStatus(ConnectionState.Error, message);
                default: throw new FormatException("Estado desconocido del puente BLE.");
            }
        }

        /// <summary>Decodes a bounded hexadecimal payload and rejects invalid encoding.</summary>
        public byte[] ToPayload()
        {
            if (hex == null || hex.Length % 2 != 0 || hex.Length > 1024)
                throw new FormatException("Payload hexadecimal invalido.");
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }
    }
}
