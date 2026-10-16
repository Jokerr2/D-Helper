using GHelper.Dell;
using System.Diagnostics;

namespace GHelper.Battery
{
    public static class BatteryControl
    {

        static bool _chargeFull = AppConfig.Is("charge_full");
        static int _applyGeneration;
        static long _lastDellBatteryApply;

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

        /// <summary>Persisted BIOS charge policy for Dell (Standard / Adaptive / Custom / …).</summary>
        public static ChargeMode GetDellChargeMode()
        {
            var s = AppConfig.GetString("dell_charge_mode", "");
            if (!string.IsNullOrEmpty(s) && Enum.TryParse(s, true, out ChargeMode mode))
                return mode;
            return ChargeMode.Custom;
        }

        public static void SetDellChargeMode(ChargeMode mode)
        {
            AppConfig.Set("dell_charge_mode", mode.ToString());
        }

        public static void ToggleBatteryLimitFull()
        {
            if (chargeFull) SetBatteryChargeLimit();
            else SetBatteryLimitFull();
        }

        public static void SetBatteryLimitFull()
        {
            chargeFull = true;
            if (Program.acpi.IsDellPlatform)
                SetDellChargeMode(ChargeMode.Custom);
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
            // Dell CCTK is slow — skip duplicate AutoBattery within 60s (boot Charge + resume + monitor-on)
            if (Program.acpi.IsDellPlatform)
            {
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (Math.Abs(now - _lastDellBatteryApply) < 60000)
                {
                    Logger.WriteLine("AutoBattery skipped (Dell debounce)");
                    return;
                }
                _lastDellBatteryApply = now;
            }

            if (chargeFull && !init) SetBatteryLimitFull();
            else SetBatteryChargeLimit();
        }

        /// <summary>ASUS Optimization registry hook — removed for Dell-only.</summary>
        public static void SetAsusChargeLimit(int value) { }

        public static void SetBatteryChargeLimit(int setLimit = -1)
        {
            int limit = setLimit;
            if (limit < 0) limit = AppConfig.Get("charge_limit");
            if (limit < 40 || limit > 100) return;

            AppConfig.Set("charge_limit", limit);
            chargeFull = false;

            // Explicit slider value → Custom charge policy (do not wipe Adaptive/etc from AutoBattery)
            if (setLimit > 0)
                SetDellChargeMode(ChargeMode.Custom);

            // Optimistic UI first so the slider/label never desync while CLI runs
            try { Program.settingsForm?.VisualiseBattery(limit); } catch { /* form not ready */ }

            ApplyLimitAsync(limit, full: false);
        }

        static void ApplyLimitAsync(int limit, bool full)
        {
            int gen = Interlocked.Increment(ref _applyGeneration);
            Task.Run(() =>
            {
                if (gen != _applyGeneration) return;
                Program.acpi.DeviceSet(AsusACPI.BatteryLimit, limit, full ? "BatteryFull" : "BatteryLimit");
            });
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
