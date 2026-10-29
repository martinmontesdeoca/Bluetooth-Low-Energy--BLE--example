namespace ConnectedRoom.Windows;

internal static class Program
{
    /// <summary>Starts one desktop companion instance on the Windows UI thread.</summary>
    [STAThread]
    private static void Main()
    {
        using var instance = new Mutex(true, "ConnectedRoom.Windows.Companion", out bool created);
        if (!created)
        {
            MessageBox.Show("Connected Room ya esta abierta.");
            return;
        }
        ApplicationConfiguration.Initialize();
        System.Windows.Forms.Application.Run(new MainForm(new ConnectedRoom.Native.GattSensorHost()));
    }
}
