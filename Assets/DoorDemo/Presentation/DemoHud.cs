using System;
using DoorDemo.Application;
using UnityEngine;

namespace DoorDemo.Presentation
{
    public sealed class DemoHud : MonoBehaviour
    {
        private DoorController controller;
        private DemoSettings settings;
        private SessionMonitor monitor;
        private Action<bool> changeMode;
        private Action<bool> simulate;
        private bool simulation;
        private bool details;
        private Vector2 scroll;
        private string warning = string.Empty;
        private string feedback = string.Empty;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle small;
        private GUIStyle panel;
        private GUIStyle button;
        private Texture2D background;

        /// <summary>Injects configuration and user actions without constructing a transport inside the view.</summary>
        public void Initialize(DemoSettings configuration, Action<bool> onChangeMode, Action<bool> onSimulate)
        {
            settings = configuration;
            changeMode = onChangeMode;
            simulate = onSimulate;
        }

        /// <summary>Rebinds session events and clears feedback when the composition root replaces the controller.</summary>
        public void Bind(DoorController value, SessionMonitor sessionMonitor, bool isSimulation)
        {
            if (controller != null)
            {
                controller.InvalidPayload -= OnInvalidPayload;
                controller.CommandRejected -= OnInvalidPayload;
                controller.CommandAcknowledged -= OnAcknowledged;
            }
            controller = value;
            monitor = sessionMonitor;
            simulation = isSimulation;
            warning = string.Empty;
            feedback = string.Empty;
            controller.InvalidPayload += OnInvalidPayload;
            controller.CommandRejected += OnInvalidPayload;
            controller.CommandAcknowledged += OnAcknowledged;
        }

        /// <summary>Shows a validation or rejected-command explanation to the operator.</summary>
        private void OnInvalidPayload(string message) => warning = message;
        /// <summary>Shows peer acceptance without treating it as a local door command.</summary>
        private void OnAcknowledged(Domain.DoorState state) => feedback =
            simulation ? "Orden simulada (sin BLE)" :
                "Windows recibio: " + (state == Domain.DoorState.Open ? "ABRIR" : "CERRAR");

        /// <summary>Applies safe-area scaling and restores shared GUI state even when an input action exits GUI processing.</summary>
        private void OnGUI()
        {
            if (controller == null)
                return;
            EnsureStyles();
            Matrix4x4 previousMatrix = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            float scale = Mathf.Clamp(Screen.height / 800f, 0.65f, 1.4f);
            Rect safeArea = Screen.safeArea;
#if UNITY_ANDROID && !UNITY_EDITOR
            scale = Mathf.Max(0.65f, safeArea.height / 400f);
#endif
            GUI.matrix = Matrix4x4.TRS(new Vector3(safeArea.x, Screen.height - safeArea.yMax, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));
            float width = safeArea.width / scale;
            float height = safeArea.height / scale;
            try
            {
                DrawPanel(width, height);
                DrawActivity(width, height);
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.enabled = previousEnabled;
            }
        }

        /// <summary>Draws the primary mobile panel and its focused control sections.</summary>
        private void DrawPanel(float width, float height)
        {
            GUILayout.BeginArea(new Rect(16, 16, Mathf.Min(320, width - 32), height - 32), panel);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Space(4);
            GUILayout.Label("CONNECTED ROOM", heading);
            GUILayout.Label(simulation ? "MODO SIMULADO" : "BLUETOOTH REAL · " + UnityEngine.Application.platform, small);
            GUILayout.Space(8);
            DrawConnection();
            DrawRemoteControls();
            DrawDetails();
            if (!string.IsNullOrEmpty(warning))
            {
                GUILayout.Space(12);
                GUILayout.Label(warning, body);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>Presents connection status, received state and the connection or mode action.</summary>
        private void DrawConnection()
        {
            ConnectionState state = controller.Connection.State;
            bool active = state == ConnectionState.Searching || state == ConnectionState.Connecting ||
                state == ConnectionState.Connected;
            if (GUILayout.Button(active ? "Desconectar" : "Conectar", button, GUILayout.Height(52)))
            {
                warning = string.Empty;
                if (active) controller.Disconnect(); else controller.Connect();
                GUIUtility.ExitGUI();
            }
            GUILayout.Space(8);
            GUILayout.Label(StateLabel(), heading);
            GUILayout.Label(controller.Connection.Message, body);
            GUILayout.Space(8);
            if (GUILayout.Button(simulation ? "Usar Bluetooth real" : "Modo de prueba sin BLE", button, GUILayout.Height(48)))
            {
                changeMode(!simulation);
                GUIUtility.ExitGUI();
            }
        }

        /// <summary>Enables peer commands only when the selected transport supports connected control.</summary>
        private void DrawRemoteControls()
        {
            GUI.enabled = controller.CanControl;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Abre Windows", button, GUILayout.Height(48))) simulate(true);
            if (GUILayout.Button("Cierra Windows", button, GUILayout.Height(48))) simulate(false);
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            if (!string.IsNullOrEmpty(feedback)) GUILayout.Label(feedback, small);
        }

        /// <summary>Keeps protocol identifiers and setup guidance behind an explicit details toggle.</summary>
        private void DrawDetails()
        {
            if (GUILayout.Button(details ? "Ocultar detalles" : "Detalles de conexion", button, GUILayout.Height(40)))
            {
                details = !details;
                GUIUtility.ExitGUI();
            }
            if (details)
            {
                GUILayout.Label("Abre el sensor Windows y pulsa Iniciar BLE. Luego usa Abrir / Cerrar en la PC.", body);
                GUILayout.Label("SERVICIO: " + settings.ServiceUuid, small);
                GUILayout.Label("CARACTERISTICA: " + settings.CharacteristicUuid, small);
            }
        }

        /// <summary>Distinguishes a known local door state from the absence of a valid reading.</summary>
        private string StateLabel()
        {
            if (controller.Connection.State != ConnectionState.Connected || !controller.HasReading)
                return "SIN DATO DEL SENSOR";
            return controller.Door == Domain.DoorState.Open ? "MOVIL: ABIERTA" : "MOVIL: CERRADA";
        }

        /// <summary>Shows a compact recent history without covering controls on narrow screens.</summary>
        private void DrawActivity(float width, float height)
        {
            if (width < 720)
                return;
            float panelWidth = Mathf.Min(390, width - 368);
            GUILayout.BeginArea(new Rect(width - panelWidth - 16, height - 152, panelWidth, 136), panel);
            GUILayout.Label("SALA DE ATENCION / DEMO", small);
            GUILayout.Label(monitor.Openings + " aperturas  ·  " + monitor.RecordedEvents + " eventos", body);
            int skip = Math.Max(0, Math.Min(6, monitor.RecordedEvents) - 3);
            foreach (SessionActivity entry in monitor.Activity)
            {
                if (skip-- > 0) continue;
                GUILayout.Label(entry.Timestamp.ToLocalTime().ToString("HH:mm:ss") + "  " + entry.Description, small);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        /// <summary>Creates readable UI styles and owns the background texture used by the panels.</summary>
        private void EnsureStyles()
        {
            if (heading != null)
                return;
            button = new GUIStyle(GUI.skin.button) { fontSize = 18, wordWrap = true };
            panel = new GUIStyle(GUI.skin.box);
            background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            background.SetPixel(0, 0, new Color(0.025f, 0.045f, 0.075f, 0.97f));
            background.Apply(false, true);
            panel.normal.background = background;
            panel.border = new RectOffset();
            panel.padding = new RectOffset(10, 10, 8, 8);
            heading = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
            heading.normal.textColor = new Color(0.3f, 0.95f, 0.86f);
            body = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            body.normal.textColor = new Color(0.86f, 0.91f, 0.95f);
            small = new GUIStyle(body) { fontSize = 15 };
            small.normal.textColor = new Color(0.65f, 0.75f, 0.83f);
        }

        /// <summary>Releases the generated texture and detaches every controller callback.</summary>
        private void OnDestroy()
        {
            if (background != null) Destroy(background);
            if (controller != null)
            {
                controller.InvalidPayload -= OnInvalidPayload;
                controller.CommandRejected -= OnInvalidPayload;
                controller.CommandAcknowledged -= OnAcknowledged;
            }
        }
    }
}
