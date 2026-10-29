using ConnectedRoom.Native;
using DoorDemo.Domain;

namespace ConnectedRoom.Windows;

public sealed class MainForm : Form
{
    private readonly GattSensorHost host;
    private readonly DoorPanel door = new() { Dock = DockStyle.Fill };
    private readonly Label status = new() { AutoSize = true, Text = "Servidor detenido" };
    private readonly Label traffic = new() { AutoSize = true, Text = "Lecturas: 0  ·  Escrituras: 0" };
    private readonly Button start = MakeButton("Iniciar BLE");
    private readonly Button stop = MakeButton("Detener BLE");
    private readonly Button open = MakeButton("Abrir movil");
    private readonly Button close = MakeButton("Cerrar movil");
    private readonly ListBox history = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
    private bool closing;

    /// <summary>Takes ownership of an injected GATT host and composes the Windows view and event bindings.</summary>
    public MainForm(GattSensorHost host)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        ConfigureWindow();
        Controls.Add(CreateLayout());
        BindEvents();
    }

    /// <summary>Applies window sizing, theme and initial disabled states before the host starts.</summary>
    private void ConfigureWindow()
    {
        Text = "Connected Room · Windows / BLE";
        ClientSize = new Size(1100, 800);
        MinimumSize = new Size(920, 640);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(12, 21, 32);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 12);
        stop.Enabled = open.Enabled = close.Enabled = false;
        history.BackColor = Color.FromArgb(20, 32, 46);
        history.ForeColor = Color.Gainsboro;
    }

    /// <summary>Builds the responsive controls, local door preview and bounded traffic history area.</summary>
    private Control CreateLayout()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 2, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        var controls = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        controls.Controls.Add(new Label { Text = "CONNECTED ROOM", AutoSize = true, Font = new Font("Segoe UI", 22, FontStyle.Bold), Margin = new Padding(0, 0, 0, 18) });
        controls.Controls.Add(new Label { Text = "Windows ↔ Android por BLE", AutoSize = true, Margin = new Padding(0, 0, 0, 20) });
        controls.Controls.Add(start);
        controls.Controls.Add(stop);
        controls.Controls.Add(open);
        controls.Controls.Add(close);
        status.MaximumSize = new Size(325, 100);
        status.Margin = new Padding(0, 18, 0, 12);
        controls.Controls.Add(status);
        controls.Controls.Add(traffic);
        controls.Controls.Add(new Label { Text = "Los botones controlan el movil.\nEsta puerta responde a Android.", AutoSize = true, Margin = new Padding(0, 20, 0, 0) });
        layout.Controls.Add(controls, 0, 0);
        layout.Controls.Add(door, 1, 0);
        layout.Controls.Add(history, 0, 1);
        layout.SetColumnSpan(history, 2);
        return layout;
    }

    /// <summary>Routes peer actions and native callbacks through the form main thread.</summary>
    private void BindEvents()
    {
        start.Click += async (_, _) => await StartAsync();
        stop.Click += (_, _) => StopServer();
        open.Click += (_, _) => host.OutgoingDoor.Set(DoorState.Open, "Windows → Android");
        close.Click += (_, _) => host.OutgoingDoor.Set(DoorState.Closed, "Windows → Android");
        host.OutgoingDoor.Changed += (state, origin) => OnUi(() => AddEvent(origin + ": " + state));
        host.Message += message => OnUi(() => AddEvent(message));
        host.AdvertisingChanged += active => OnUi(() =>
        {
            status.Text = active ? "BLE activo · esperando / conectado a Android" : "BLE sin anuncio activo";
            status.ForeColor = active ? Color.MediumAquamarine : Color.Coral;
        });
        host.TrafficChanged += (reads, writes) => OnUi(() => traffic.Text = $"Lecturas: {reads}  ·  Escrituras: {writes}");
        host.ReceivedDoor.Changed += (state, origin) => OnUi(() =>
        {
            door.ShowDoor(host.ReceivedDoor.Value == DoorState.Open);
            AddEvent($"{origin}: puerta {(state == DoorState.Open ? "abierta" : "cerrada")}");
        });
    }

    /// <summary>Stops accepting native UI updates and disposes the owned host during window shutdown.</summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        closing = true;
        try { host.Dispose(); }
        finally { base.OnFormClosed(e); }
    }

    /// <summary>Starts the GATT server and surfaces setup failures while respecting form lifetime.</summary>
    private async Task StartAsync()
    {
        start.Enabled = false;
        status.Text = "Creando servicio BLE...";
        try
        {
            await host.StartAsync();
            if (closing) return;
            stop.Enabled = open.Enabled = close.Enabled = true;
        }
        catch (Exception error)
        {
            if (closing) return;
            try { host.Stop(); }
            catch (Exception cleanupError) { AddEvent("Error al liberar BLE: " + cleanupError.Message); }
            status.Text = error.Message;
            AddEvent("Error: " + error.Message);
            start.Enabled = true;
        }
    }

    /// <summary>Stops advertising and restores controls even when native shutdown reports an error.</summary>
    private void StopServer()
    {
        try
        {
            host.Stop();
            status.Text = "Servidor detenido";
            AddEvent("Servidor detenido; Android debe reconectar al reiniciarlo.");
        }
        catch (Exception error)
        {
            status.Text = "Error al detener BLE";
            AddEvent(error.Message);
        }
        finally
        {
            start.Enabled = true;
            stop.Enabled = open.Enabled = close.Enabled = false;
        }
    }

    /// <summary>Marshals callbacks to the UI thread and tolerates only handle destruction during shutdown.</summary>
    private void OnUi(Action action)
    {
        if (closing || IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(() => { if (!closing && !IsDisposed) action(); }); }
            catch (InvalidOperationException) when (closing || IsDisposed || !IsHandleCreated)
            {
                // A queued native callback may outlive the form's handle during shutdown.
            }
        }
        else action();
    }

    /// <summary>Prepends a timestamped diagnostic while bounding the visible history.</summary>
    private void AddEvent(string message)
    {
        history.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + message);
        while (history.Items.Count > 100) history.Items.RemoveAt(history.Items.Count - 1);
    }

    /// <summary>Creates consistently sized peer-command and connection controls.</summary>
    private static Button MakeButton(string text) => new()
    {
        Text = text,
        Size = new Size(320, 54),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(28, 55, 72),
        ForeColor = Color.WhiteSmoke,
        Margin = new Padding(0, 0, 0, 10)
    };
}
