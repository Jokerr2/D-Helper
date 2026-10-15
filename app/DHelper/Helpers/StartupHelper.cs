using Microsoft.Win32;

namespace DHelper.Helpers;

internal static class StartupHelper
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DHelper";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(ValueName) != null;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true) ??
                        Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            var exe = Environment.ProcessPath ?? Application.ExecutablePath;
            key.SetValue(ValueName, $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
        AppConfig.Set("startup", enabled);
    }

    /// <summary>
    /// Optional one-shot charge apply for Task Scheduler / CLI: DHelper.exe --charge
    /// </summary>
    public static bool IsChargeOnlyArg(string[] args) =>
        args.Any(a => a.Equals("--charge", StringComparison.OrdinalIgnoreCase) ||
                      a.Equals("charge", StringComparison.OrdinalIgnoreCase));
}
