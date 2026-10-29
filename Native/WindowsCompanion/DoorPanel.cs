using System.Drawing.Drawing2D;

namespace ConnectedRoom.Windows;

public sealed class DoorPanel : Panel
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private float progress;
    private float target;

    /// <summary>Initializes a double-buffered local-door animation and its owned frame timer.</summary>
    public DoorPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(12, 21, 32);
        timer.Tick += (_, _) =>
        {
            progress += Math.Clamp(target - progress, -0.035f, 0.035f);
            Invalidate();
            if (Math.Abs(target - progress) < 0.001f) timer.Stop();
        };
    }

    /// <summary>Targets the Windows door animation from a command received for this PC.</summary>
    public void ShowDoor(bool open)
    {
        target = open ? 1 : 0;
        timer.Start();
    }

    /// <summary>Renders the local door, frame and state label with disposable drawing resources.</summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float height = Math.Min(Height - 90, Width * 0.85f);
        float width = height * 0.6f;
        float x = (Width - width) / 2;
        float y = (Height - height) / 2;
        using var frame = new Pen(Color.FromArgb(85, 115, 135), 15);
        using var room = new SolidBrush(Color.FromArgb(24, 41, 56));
        graphics.FillRectangle(room, x, y, width, height);
        graphics.DrawRectangle(frame, x, y, width, height);
        float leafWidth = width * (1 - progress * 0.75f);
        float depth = progress * 38;
        PointF[] leaf = { new(x, y), new(x + leafWidth, y + depth),
            new(x + leafWidth, y + height - depth), new(x, y + height) };
        using var door = new SolidBrush(Color.FromArgb(28, 165, 184));
        graphics.FillPolygon(door, leaf);
        using var handle = new Pen(Color.WhiteSmoke, 6);
        graphics.DrawLine(handle, x + leafWidth - 20, y + height / 2 - 12,
            x + leafWidth - 20, y + height / 2 + 12);
        using var indicator = new SolidBrush(target > 0 ? Color.MediumAquamarine : Color.Coral);
        graphics.FillRectangle(indicator, x + width * 0.35f, y - 20, width * 0.3f, 6);
        using var font = new Font("Segoe UI", 16, FontStyle.Bold);
        using var text = new SolidBrush(Color.WhiteSmoke);
        string label = target > 0 ? "PC: PUERTA ABIERTA" : "PC: PUERTA CERRADA";
        var size = graphics.MeasureString(label, font);
        graphics.DrawString(label, font, text, (Width - size.Width) / 2, Height - 40);
    }

    /// <summary>Stops and releases the animation timer before base control cleanup.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}
