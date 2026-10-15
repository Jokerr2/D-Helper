using GHelper.Helpers;
using Microsoft.Win32;
using System.Diagnostics;

namespace GHelper.Battery
{
    public static class BatteryControl
    {

        static bool _chargeFull = AppConfig.Is("charge_full");
        static int _applyGeneration;

        public static bool chargeFull
        {
            get
            {
                return _chargeFull;
            }
            set
            {
                AppConfig.Set("charge_full", value ? 1 : 0);
                _chargeFull = value;
            }
        }

        public static void ToggleBatteryLimitFull()
        {
            if (chargeFull) SetBatteryChargeLimit();
            else SetBatteryLimitFull();
        }

        public static void SetBatteryLimitFull()
        {
            chargeFull = true;
            ApplyLimitAsync(100, full: true);
            Program.settingsForm.VisualiseBatteryFull();
        }

        public static void UnSetBatteryLimitFull()
        {
            chargeFull = false;
            Logger.WriteLine("Battery fully charged");
            Program.settingsForm.Invoke(Program.settingsForm.VisualiseBatteryFull);
        }

        public static void AutoBattery(bool init = false)
        {
            if (chargeFull && !init) SetBatteryLimitFull();
            else SetBatteryChargeLimit();
        }

        public static void SetAsusChargeLimit(int value)
        {
            if (Program.acpi.IsDellPlatform) return;
            if (!ProcessHelper.IsUserAdministrator()) return;
            const string keyPath = @"SOFTWARE\ASUS\ASUS System Control Interface\AsusOptimization\ASUS Keyboard Hotkeys";
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath, writable: true);
                key?.SetValue("ChargingRate", value, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set ChargingRate: {ex.Message}");
            }
        }

        public static void SetBatteryChargeLimit(int setLimit = -1)
        {
            int limit = setLimit;
            if (limit < 0) limit = AppConfig.Get("charge_limit");
            if (limit < 40 || limit > 100) return;

            if (!Program.acpi.IsDellPlatform && AppConfig.IsChargeLimit6080())
            {
                if (limit > 85) limit = 100;
                else if (limit >= 80) limit = 80;
                else if (limit < 60) limit = 60;
            }

            if (setLimit > 0) SetAsusChargeLimit(limit);

            AppConfig.Set("charge_limit", limit);
            chargeFull = false;

            // Optimistic UI first so the slider/label never desync while CLI runs
            try { Program.settingsForm?.VisualiseBattery(limit); } catch { /* form not ready */ }

            ApplyLimitAsync(limit, full: false);
        }

        static void ApplyLimitAsync(int limit, bool full)
        {
            int gen = Interlocked.Increment(ref _applyGeneration);
            if (Program.acpi.IsDellPlatform)
            {
                Task.Run(() =>
                {
                    if (gen != _applyGeneration) return;
                    Program.acpi.DeviceSet(AsusACPI.BatteryLimit, limit, full ? "BatteryFull" : "BatteryLimit");
                });
                return;
            }

            Program.acpi.DeviceSet(AsusACPI.BatteryLimit, limit, full ? "BatteryFull" : "BatteryLimit");
        }

        public static void BatteryReport()
        {
            var reportDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            try
            {
                var cmd = new Process();
                cmd.StartInfo.WorkingDirectory = reportDir;
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.FileName = "powershell";
                cmd.StartInfo.Arguments = "powercfg /batteryreport; explorer battery-report.html";
                cmd.Start();
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex.Message);
            }
        }

    }
}
