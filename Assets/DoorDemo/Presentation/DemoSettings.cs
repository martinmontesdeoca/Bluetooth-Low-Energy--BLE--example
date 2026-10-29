using System;
using DoorDemo.Domain;
using UnityEngine;

namespace DoorDemo.Presentation
{
    [CreateAssetMenu(menuName = "Door Demo/Settings", fileName = "DoorDemoSettings")]
    public sealed class DemoSettings : ScriptableObject
    {
        [SerializeField] private string serviceUuid = DoorProtocol.ServiceUuid;
        [SerializeField] private string characteristicUuid = DoorProtocol.CharacteristicUuid;
        [SerializeField, Min(10)] private float doorSpeed = 110;

        public string ServiceUuid => serviceUuid;
        public string CharacteristicUuid => characteristicUuid;
        public float DoorSpeed => doorSpeed;

        /// <summary>Rejects empty GATT identities and non-finite or non-positive animation speeds.</summary>
        public void Validate()
        {
            if (!Guid.TryParse(serviceUuid, out Guid service) || service == Guid.Empty ||
                !Guid.TryParse(characteristicUuid, out Guid characteristic) || characteristic == Guid.Empty)
                throw new InvalidOperationException("Los UUID de DoorDemoSettings deben ser validos y no vacios.");
            if (doorSpeed <= 0 || float.IsNaN(doorSpeed) || float.IsInfinity(doorSpeed))
                throw new InvalidOperationException("La velocidad de la puerta debe ser positiva y finita.");
        }
    }
}
