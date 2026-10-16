using GHelper.Battery;
using GHelper.Display;
using GHelper.Gpu;
using GHelper.Helpers;
using GHelper.Input;
using GHelper.Mode;
using GHelper.Overlay;
using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using static NativeMethods;

namespace GHelper
{

    static class Program
    {
        public static NotifyIcon trayIcon;
        public static AsusACPI acpi;

        public static SettingsForm settingsForm;

        public static ModeControl modeControl;
        public static GPUModeControl gpuControl;
        public static ClamshellModeControl clamshellControl;

        public static ToastForm toast;

        public static HardwareOverlay? hardwareOverlay;

        public static IntPtr unRegPowerNotify, unRegPowerNotifyLid, unRegPowerNotifyEnergy, unRegSuspendResume;
        public static int WM_TASKBARCREATED = 0;

        private static long lastAuto;
        private static readonly object autoLock = new();

        public static InputDispatcher? inputDispatcher;

        // The main entry point for the application
        public static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            AppDomain.CurrentDomain.UnhandledException += (s, e) => Logger.WriteLine("Unhandled: " + e.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (s, e) => { Logger.WriteLine("Unobserved: " + e.Exception); e.SetObserved(); };

            string action = "";
            if (args.Length > 0) action = args[0];

            if (action == "charge")
            {
                Charge();
                return;
            }

            string language = AppConfig.GetString("language");
            try
            {
                if (language != null && language.Length > 0)
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                else
                {
                    var culture = CultureInfo.CurrentUICulture;
                    if (culture.ToString() == "kr") culture = CultureInfo.GetCultureInfo("ko");
                    Thread.CurrentThread.CurrentUICulture = culture;
                }
            } catch
            {
                Logger.WriteLine("Unknown Language: " + language);
            }

            Logger.WriteLine("----------------------");
            Logger.WriteLine("App launched: " + AppConfig.GetModel() + " :" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + CultureInfo.CurrentUICulture + (ProcessHelper.IsUserAdministrator() ? "." : ""));

            ProcessHelper.CheckAlreadyRunning();
            ProcessHelper.SetPriority();

            CleanupLegacyFiles();

            var startCount = AppConfig.Get("start_count") + 1;
            AppConfig.Set("start_count", startCount);
            Logger.WriteLine("Start Count: " + startCount);

            // Probe ACPI / Dell bridge before constructing ModeControl / Settings
            acpi = new AsusACPI();

            if (!acpi.IsConnected())
            {
                MessageBox.Show(
                    "D-Helper needs Dell Optimizer and/or Dell Command | Configure.\n\nInstall them from Dell Support, then run D-Helper as Administrator.",
                    "D-Helper — Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            settingsForm = new SettingsForm();
            modeControl = new ModeControl();
            gpuControl = new GPUModeControl(settingsForm);
            clamshellControl = new ClamshellModeControl();
            toast = new ToastForm();

            hardwareOverlay = new HardwareOverlay();

            Application.EnableVisualStyles();

            trayIcon = new NotifyIcon
            {
                Text = "D-Helper",
                // Clone — never assign shared Resources icon (VisualiseIcon must be free to dispose tray icons)
                Icon = GHelper.UI.IconHelper.CloneIcon(Properties.Resources.standard),
                Visible = true
            };

            var trayRetry = new System.Windows.Forms.Timer { Interval = 5000 };
            trayRetry.Tick += (_, _) => { trayRetry.Dispose(); trayIcon.Visible = false; trayIcon.Visible = true; };
            trayRetry.Start();

            WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");
            Logger.WriteLine($"Tray Icon: {trayIcon.Visible} | {WM_TASKBARCREATED}");

            Modes.InitDellCool();

            settingsForm.SetContextMenu();
            trayIcon.MouseClick += TrayIcon_MouseClick;
            trayIcon.MouseMove += TrayIcon_MouseMove;


            inputDispatcher = new InputDispatcher();

            settingsForm.InitDellPlatform();

            ScreenControl.InitScreen();

            SetAutoModes(init: true);

            powerSettleTimer.Elapsed += OnPowerSettled;

            // Subscribing for system power change events
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;

            clamshellControl.RegisterDisplayEvents();
            clamshellControl.ToggleLidAction();

            // Subscribing for monitor power on events
            unRegPowerNotify = NativeMethods.RegisterPowerSettingNotification(settingsForm.Handle, PowerSettingGuid.ConsoleDisplayState, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
            unRegPowerNotifyLid = NativeMethods.RegisterPowerSettingNotification(settingsForm.Handle, PowerSettingGuid.LIDSWITCH_STATE_CHANGE, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
            unRegPowerNotifyEnergy = NativeMethods.RegisterPowerSettingNotification(settingsForm.Handle, PowerSettingGuid.EnergySaverStatus, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
            unRegSuspendResume = NativeMethods.RegisterSuspendResumeNotification(settingsForm.Handle, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);


            if (Environment.CurrentDirectory.Trim('\\') == Application.StartupPath.Trim('\\') || action.Length > 0)
            {
                SettingsToggle(false);
            }

            switch (action)
            {
                case "cpu":
                    Startup.ReScheduleAdmin();
                    settingsForm.FansToggle();
                    break;
                case "gpu":
                    Startup.ReScheduleAdmin();
                    settingsForm.FansToggle(1);
                    break;
                case "services":
                    settingsForm.extraForm = new Extra();
                    settingsForm.extraForm.Show();
                    settingsForm.extraForm.ServiesToggle();
                    break;
                case "uv":
                    Startup.ReScheduleAdmin();
                    settingsForm.FansToggle(2);
                    modeControl.SetRyzen();
                    break;
                case "colors":
                    Task.Run(async () =>
                    {
                        await ColorProfileHelper.InstallProfile();
                        settingsForm.Invoke(delegate
                        {
                            settingsForm.InitVisual();
                        });
                    });
                    break;
                default:
                    Task.Run(Startup.StartupCheck);
                    break;
            }

            if (AppConfig.IsOverlay())
                hardwareOverlay?.StartOverlay();

            Application.ApplicationExit += OnExit;
            Application.Run();
        }


        private static void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            gpuControl.StandardModeFix();
            modeControl.ShutdownReset();
            BatteryControl.AutoBattery();
            InputDispatcher.ShutdownStatusLed();
        }

        private static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLogon || e.Reason == SessionSwitchReason.SessionUnlock || e.Reason == SessionSwitchReason.ConsoleConnect)
            {
                Logger.WriteLine("Session:" + e.Reason.ToString());
                ProcessHelper.KillSmartDisplayControl();
                Task.Delay(2000).ContinueWith(_ =>
                {
                    ScreenControl.AutoScreen();
                    if (Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastAuto) < 10000) return;
                    modeControl.AutoCPUTemp();
                });
            }
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                Logger.WriteLine("Session:" + e.Reason.ToString());
            }
        }

        static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {

            switch (e.Category)
            {
                case UserPreferenceCategory.General:
                    if (!settingsForm.InitTheme()) return;

                    Debug.WriteLine("Theme Changed");
                    settingsForm.InitContextMenuTheme();
                    settingsForm.VisualiseIcon(true);
                    settingsForm.VisualiseFnLock();
                    settingsForm.VisualiseBatteryFull();

                    if (settingsForm.fansForm is not null && settingsForm.fansForm.Text != "")
                        settingsForm.fansForm.InitTheme();

                    if (settingsForm.extraForm is not null && settingsForm.extraForm.Text != "")
                        settingsForm.extraForm.InitTheme();

                    if (settingsForm.updatesForm is not null && settingsForm.updatesForm.Text != "")
                        settingsForm.updatesForm.InitTheme();

                    break;
            }
        }



        public static bool SetAutoModes(bool powerChanged = false, bool init = false, bool wakeup = false)
        {
            int skipDelay = wakeup ? 10000 : 3000;

            if (init) gpuControl.CaptureNvBootState();

            lock (autoLock)
            {
                if (Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastAuto) < skipDelay) return false;
                lastAuto = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }

            currentSource = ReadPowerSource();
            Logger.WriteLine("AutoSetting for " + SystemInformation.PowerStatus.PowerLineStatus.ToString());

            // Dell fast-path: no ASUS Aura/GPU/Matrix/WMI — keep UI responsive
            BatteryControl.AutoBattery(init);
            modeControl.AutoPerformance(powerChanged);
            try { ScreenControl.AutoScreen(); } catch (Exception ex) { Logger.WriteLine("AutoScreen: " + ex.Message); }
            return true;
        }

        public enum PowerSource { Battery, Barrel, USBC }

        public static PowerSource currentSource = PowerSource.Battery;
        private static PowerLineStatus lastLineStatus = SystemInformation.PowerStatus.PowerLineStatus;
        private static readonly System.Timers.Timer powerSettleTimer = new() { AutoReset = false };

        public static PowerSource ReadPowerSource()
        {
            if (SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online)
                return PowerSource.Battery;

            int chargerMode = acpi?.DeviceGet(AsusACPI.ChargerMode) ?? 0;
            if (chargerMode > 0 && (chargerMode & AsusACPI.ChargerBarrel) == 0)
                return PowerSource.USBC;

            return PowerSource.Barrel;
        }

        public static bool usbcProfile = AppConfig.Is("usbc_profile");

        public static int PerformanceKey() =>
            usbcProfile ? (int)ReadPowerSource() : (int)SystemInformation.PowerStatus.PowerLineStatus;

        public static void SchedulePowerCheck()
        {
            if (AppConfig.Is("disable_power_event")) return;
            powerSettleTimer.Interval = Math.Max(AppConfig.Get("charger_delay"), 2000);
            powerSettleTimer.Stop();
            powerSettleTimer.Start();
        }

        private static void OnPowerSettled(object? sender, System.Timers.ElapsedEventArgs e)
        {
            PowerSource source = ReadPowerSource();
            if (source == currentSource) return;

            Logger.WriteLine($"Power source: {currentSource} -> {source}");
            currentSource = source;
            SetAutoModes(powerChanged: true);
        }

        public static void OnChargerEvent() => SchedulePowerCheck();

        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                Logger.WriteLine("Power Mode Changed:" + e.Mode.ToString());
                gpuControl.StandardModeFix();
                modeControl.ShutdownReset();
                InputDispatcher.ShutdownStatusLed();
                return;
            }

            PowerLineStatus status = SystemInformation.PowerStatus.PowerLineStatus;
            if (status != lastLineStatus)
            {
                lastLineStatus = status;
                Logger.WriteLine($"Power Mode {e.Mode}: {status}");
            }

            SchedulePowerCheck();
        }

        public static void SettingsToggle(bool checkForFocus = true, bool trayClick = false)
        {
            if (settingsForm.Visible)
            {
                // If helper window is not on top, this just focuses on the app again
                // Pressing the ghelper button again will hide the app
                if (checkForFocus && !settingsForm.HasAnyFocus(trayClick) && !AppConfig.Is("topmost"))
                {
                    settingsForm.ShowAll();
                }
                else
                {
                    settingsForm.HideAll();
                }
            }
            else
            {
                var screen = Screen.PrimaryScreen;
                if (screen is null) screen = Screen.FromControl(settingsForm);

                settingsForm.WindowState = FormWindowState.Normal;

                settingsForm.Location = screen.WorkingArea.Location;
                settingsForm.Left = screen.WorkingArea.Right - 10 - settingsForm.Width;
                settingsForm.Top = screen.WorkingArea.Top > screen.Bounds.Top ? screen.WorkingArea.Top + 10 : screen.WorkingArea.Bottom - 10 - settingsForm.Height;

                settingsForm.Show();
                settingsForm.ShowAll();

                settingsForm.Left = screen.WorkingArea.Right - 10 - settingsForm.Width;
                settingsForm.Top = screen.WorkingArea.Top > screen.Bounds.Top ? screen.WorkingArea.Top + 10 : screen.WorkingArea.Bottom - 10 - settingsForm.Height;

                settingsForm.VisualiseGPUMode();
            }
        }

        static void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                SettingsToggle(trayClick: true);

        }

        static void TrayIcon_MouseMove(object? sender, MouseEventArgs e)
        {
            settingsForm.RefreshSensors();
        }

        static void OnExit(object sender, EventArgs e)
        {
            if (trayIcon is not null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            clamshellControl.UnregisterDisplayEvents();
            NativeMethods.UnregisterPowerSettingNotification(unRegPowerNotify);
            NativeMethods.UnregisterPowerSettingNotification(unRegPowerNotifyLid);
            NativeMethods.UnregisterPowerSettingNotification(unRegPowerNotifyEnergy);
            NativeMethods.UnregisterSuspendResumeNotification(unRegSuspendResume);
        }

        static void Charge()
        {
            try
            {
                acpi = new AsusACPI();
                // Dell: main app AutoBattery applies CCTK — boot charge task would double-write (~13s each)
                Logger.WriteLine("------- Startup Battery Limit skipped (Dell; main app applies) -------");
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Startup Battery Limit Error: " + ex.Message);
            }

            Application.Exit();
        }

        static void CleanupLegacyFiles()
        {
            string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            string[] legacyFiles = ["WinRing0x64.sys", "WinRing0x64.dll"];

            foreach (string fileName in legacyFiles)
            {
                string filePath = Path.Combine(appDir, fileName);
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        Logger.WriteLine($"Deleted legacy file: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine($"Failed to delete legacy file {fileName}: {ex.Message}");
                    }
                }
            }
        }

    }
}
