using System;
using System.IO;
using DoorDemo.Application;
using DoorDemo.Infrastructure;
using UnityEngine;

namespace DoorDemo.Presentation
{
    /// <summary>Composition root: the only place that selects concrete transports.</summary>
    public sealed class DemoBootstrap : MonoBehaviour
    {
        [SerializeField] private DemoSettings settings;
        private DoorController controller;
        private SessionMonitor monitor;
        private SimulatedDoorTransport simulated;
        private DoorView view;
        private DemoHud hud;

        /// <summary>Assigns the scene configuration before Unity starts the composition root.</summary>
        public void Configure(DemoSettings configuration) => settings = configuration;

        /// <summary>Validates configuration and composes the scene, view and selected transport.</summary>
        private void Start()
        {
            if (settings == null)
                throw new InvalidOperationException("Asigna DoorDemoSettings al punto de composicion de la escena.");
            settings.Validate();
            UnityEngine.Application.runInBackground = true;
            UnityEngine.Application.targetFrameRate = 60;
            view = gameObject.AddComponent<DemoSceneFactory>().Create(settings.DoorSpeed);
            hud = gameObject.AddComponent<DemoHud>();
            hud.Initialize(settings, SetMode, open => controller.RequestDoor(
                open ? Domain.DoorState.Open : Domain.DoorState.Closed));
            SetMode(false);
        }

        /// <summary>Replaces the session and injects a simulated or platform transport without leaking subscriptions.</summary>
        private void SetMode(bool useSimulation)
        {
            if (controller != null)
            {
                controller.DoorChanged -= view.Show;
                controller.ConnectionChanged -= view.ShowConnection;
                monitor.Dispose();
                controller.Dispose();
            }
            view.Show(Domain.DoorState.Closed);
            simulated = useSimulation ? new SimulatedDoorTransport() : null;
            IDoorTransport transport;
            if (useSimulation)
                transport = simulated;
            else
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                transport = new AndroidDoorTransport(settings.ServiceUuid, settings.CharacteristicUuid);
#else
#if UNITY_EDITOR
                string bridgeRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Artifacts"));
#else
                string bridgeRoot = UnityEngine.Application.streamingAssetsPath;
#endif
                transport = new WindowsBridgeTransport(Path.Combine(bridgeRoot,
                    "BleBridge", "BleBridge.exe"), settings.ServiceUuid, settings.CharacteristicUuid);
#endif
            }
            controller = new DoorController(transport, transport as IDoorCommandWriter);
            monitor = new SessionMonitor(controller, () => DateTimeOffset.UtcNow);
            controller.DoorChanged += view.Show;
            controller.ConnectionChanged += view.ShowConnection;
            view.ShowConnection(controller.Connection);
            hud.Bind(controller, monitor, useSimulation);
        }

        /// <summary>Pumps native events from Unity main-thread updates.</summary>
        private void Update() => controller?.Pump();

        /// <summary>Disconnects an established foreground-only session when the mobile application is suspended.</summary>
        private void OnApplicationPause(bool paused)
        {
            // Foreground demo: release an established radio session when the mobile app is suspended.
            if (paused && controller != null && controller.Connection.State == ConnectionState.Connected)
                controller.Disconnect();
        }

        /// <summary>Unsubscribes presentation callbacks and releases the active session.</summary>
        private void OnDestroy()
        {
            if (controller == null)
                return;
            controller.DoorChanged -= view.Show;
            controller.ConnectionChanged -= view.ShowConnection;
            monitor.Dispose();
            controller.Dispose();
        }
    }
}
