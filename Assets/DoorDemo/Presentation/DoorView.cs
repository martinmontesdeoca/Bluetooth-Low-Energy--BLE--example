using DoorDemo.Domain;
using DoorDemo.Application;
using UnityEngine;

namespace DoorDemo.Presentation
{
    public sealed class DoorView : MonoBehaviour
    {
        private Transform hinge;
        private Renderer indicator;
        private float speed;
        private float targetAngle;

        /// <summary>Binds the hinge and indicator and initializes the received door view.</summary>
        public void Initialize(Transform doorHinge, Renderer statusIndicator, float degreesPerSecond)
        {
            hinge = doorHinge;
            indicator = statusIndicator;
            speed = degreesPerSecond;
            Show(DoorState.Closed);
        }

        /// <summary>Targets the hinge animation and indicator color for a received local door state.</summary>
        public void Show(DoorState state)
        {
            targetAngle = state == DoorState.Open ? -100 : 0;
            indicator.sharedMaterial.color = state == DoorState.Open
                ? new Color(0.15f, 1f, 0.65f) : new Color(1f, 0.4f, 0.18f);
        }

        /// <summary>Marks the indicator unknown when no established connection can supply readings.</summary>
        public void ShowConnection(ConnectionStatus status)
        {
            if (status.State != ConnectionState.Connected)
                indicator.sharedMaterial.color = new Color(0.4f, 0.48f, 0.55f);
        }

        /// <summary>Advances the hinge toward its target using elapsed frame time.</summary>
        private void Update()
        {
            if (hinge != null)
                hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation,
                    Quaternion.Euler(0, targetAngle, 0), speed * Time.deltaTime);
        }
    }
}
