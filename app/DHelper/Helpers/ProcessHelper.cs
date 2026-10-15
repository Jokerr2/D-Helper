namespace DHelper.Helpers;

internal static class ProcessHelper
{
    private static Mutex? _mutex;

    public static bool EnsureSingleInstance()
    {
        _mutex = new Mutex(true, @"Local\DHelper.SingleInstance", out bool created);
        if (created) return true;

        MessageBox.Show("D-Helper is already running.\nClick the tray icon to open it.", "D-Helper",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }
}
