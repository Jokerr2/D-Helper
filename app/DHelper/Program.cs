using DHelper.Battery;
using DHelper.Display;
using DHelper.Hardware;
using DHelper.Helpers;
using DHelper.Input;
using DHelper.Mode;
using DHelper.Overlay;
using DHelper.UI;
using Microsoft.Win32;

namespace DHelper;

internal static class Program
{
    private static NotifyIcon? _tray;
    private static SettingsForm? _settings;
    private static CompositeLaptopHal? _hal;
    private static ModeControl? _modes;
    private static BatteryControl? _battery;
    private static DisplayControl? _display;
    private static WindowsSensors? _sensors;
    private static HardwareOverlay? _overlay;
    private static InputDispatcher? _input;
    private static Icon? _appIcon;
    private static bool _lastOnAc;
    private static bool _balloonShown;
    private static bool _chargeAppliedThisSession;

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            MessageBox.Show(e.ExceptionObject.ToString(), "D-Helper error");

        if (!ProcessHelper.EnsureSingleInstance())
            return;

        AppConfig.Load();
        AppConfig.Set("start_count", AppConfig.Get("start_count", 0) + 1);

        _appIcon = UiTheme.CreateAppIcon();
        _hal = new CompositeLaptopHal();
        _hal.Probe(); // File.Exists only — cheap
        _sensors = new WindowsSensors();
        _modes = new ModeControl(_hal);
        _battery = new BatteryControl(_hal);
        _display = new DisplayControl();

        // Config-only loads — no Dell CLI at startup.
        _modes.LoadFromConfig();
        _battery.LoadFromConfig();

        if (StartupHelper.IsChargeOnlyArg(args))
        {
            _battery.ApplySaved(out _);
            AppConfig.SaveNow();
            return;
        }

        _overlay = new HardwareOverlay(_sensors, _modes);
        _settings = new SettingsForm(_hal, _modes, _battery, _display, _sensors, _overlay, _appIcon);

        // Hotkeys off by default (global hook cost); enable in config if wanted.
        if (AppConfig.Get("hotkeys", false))
        {
            _input = new InputDispatcher(_modes, ShowSettings);
            _input.Start();
        }

        _tray = new NotifyIcon
        {
            Text = "D-Helper — click to open",
            Icon = _appIcon,
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _tray.MouseUp += OnTrayMouseUp;

        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        _lastOnAc = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;

        // Background — never block tray startup on Dell CLI.
        _ = Task.Run(() => SetAutoModes(force: true, applyCharge: true));

        bool firstRun = AppConfig.Get("start_count", 1) <= 1;
        bool needsSetup = !_hal.Capabilities.ThermalControl;
        if (firstRun || needsSetup)
        {
            if (needsSetup)
                _settings.Toast("Install Dell Optimizer or Command Configure — open ···");
            ShowSettings();
        }
        else
        {
            ShowTrayBalloon();
        }

        Application.ApplicationExit += (_, _) => Cleanup();
        Application.Run(new ApplicationContext());
    }

    private static void OnTrayMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            ShowSettings();
    }

    private static void ShowTrayBalloon()
    {
        if (_balloonShown || _tray == null) return;
        _balloonShown = true;
        _tray.BalloonTipTitle = "D-Helper";
        _tray.BalloonTipText = "Running in the system tray. Click the icon to open.";
        _tray.BalloonTipIcon = ToolTipIcon.Info;
        _tray.ShowBalloonTip(2000);
    }

    private static ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.BackColor = UiTheme.Surface;
        menu.ForeColor = UiTheme.Text;
        menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());

        menu.Items.Add("Open D-Helper", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quiet", null, (_, _) => { _ = _modes?.ApplyAsync(ThermalMode.Quiet); });
        menu.Items.Add("Balanced", null, (_, _) => { _ = _modes?.ApplyAsync(ThermalMode.Optimized); });
        menu.Items.Add("Cool", null, (_, _) => { _ = _modes?.ApplyAsync(ThermalMode.Cool); });
        menu.Items.Add("Turbo", null, (_, _) => { _ = _modes?.ApplyAsync(ThermalMode.Ultra); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) =>
        {
            Cleanup();
            Application.Exit();
        });
        return menu;
    }

    private static void ShowSettings()
    {
        if (_settings == null) return;
        _settings.PositionNearTray();
        _settings.Show();
        _settings.WindowState = FormWindowState.Normal;
        _settings.BringToFront();
        _settings.Activate();
        // Cache-only UI refresh — never spawn Dell CLI just to open the window.
        _settings.RefreshFromCache();
    }

    private static void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode is not (PowerModes.Resume or PowerModes.StatusChange)) return;

        _ = Task.Run(async () =>
        {
            await Task.Delay(500).ConfigureAwait(false);
            try { SetAutoModes(force: false, applyCharge: false); }
            catch { /* ignore */ }
        });
    }

    private static void SetAutoModes(bool force, bool applyCharge)
    {
        var onAc = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
        if (!force && onAc == _lastOnAc) return;
        _lastOnAc = onAc;

        // Fire-and-forget async firmware writes — UI stays responsive.
        _modes?.ApplyForPowerState(onAc);
        _display?.ApplyForPowerState(onAc);

        if (applyCharge || !_chargeAppliedThisSession)
        {
            _ = _battery?.ApplySavedAsync();
            _chargeAppliedThisSession = true;
        }

        void Ui()
        {
            _settings?.Toast(onAc ? "AC profile applied" : "Battery profile applied");
            if (_tray != null && _modes != null)
                _tray.Text = $"D-Helper — {_modes.Current}";
        }

        if (_settings is { IsDisposed: false, IsHandleCreated: true })
        {
            try { _settings.BeginInvoke(Ui); }
            catch { Ui(); }
        }
        else Ui();
    }

    private static void Cleanup()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _input?.Dispose();
        _overlay?.Dispose();
        _sensors?.Dispose();
        _hal?.Dispose();
        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        _appIcon?.Dispose();
        AppConfig.SaveNow();
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => UiTheme.AccentSoft;
        public override Color MenuItemSelectedGradientBegin => UiTheme.AccentSoft;
        public override Color MenuItemSelectedGradientEnd => UiTheme.AccentSoft;
        public override Color MenuItemBorder => UiTheme.Border;
        public override Color ToolStripDropDownBackground => UiTheme.Surface;
        public override Color ImageMarginGradientBegin => UiTheme.Surface;
        public override Color ImageMarginGradientMiddle => UiTheme.Surface;
        public override Color ImageMarginGradientEnd => UiTheme.Surface;
        public override Color SeparatorDark => UiTheme.Border;
        public override Color SeparatorLight => UiTheme.Border;
    }
}
