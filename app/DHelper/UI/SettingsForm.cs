using DHelper.Battery;
using DHelper.Display;
using DHelper.Hardware;
using DHelper.Helpers;
using DHelper.Mode;
using DHelper.Overlay;

namespace DHelper.UI;

internal sealed class SettingsForm : Form
{
    private readonly ILaptopHal _hal;
    private readonly ModeControl _modes;
    private readonly BatteryControl _battery;
    private readonly DisplayControl _display;
    private readonly WindowsSensors _sensors;
    private readonly HardwareOverlay _overlay;
    private readonly System.Windows.Forms.Timer _sensorTimer;
    private readonly Icon _appIcon;

    private readonly Label _sensorCpu;
    private readonly Label _sensorBatt;
    private readonly Label _sensorPower;
    private readonly Label _sensorHz;
    private readonly Label _toastLabel;
    private readonly Label _chargeHint;
    private readonly Dictionary<ThermalMode, Button> _modeButtons = new();
    private readonly DarkSelect _chargeSelect;
    private readonly TrackBar _chargeStop;
    private readonly Label _chargeStopValue;
    private readonly CheckBox _autoThermal;
    private readonly CheckBox _autoRefresh;
    private readonly CheckBox _startup;
    private readonly CheckBox _overlayChk;
    private readonly DarkSelect _modeAc;
    private readonly DarkSelect _modeDc;
    private readonly FlowLayoutPanel _root;
    private readonly List<Control> _fullWidth = new();
    private readonly System.Windows.Forms.Timer _batteryDebounce;
    private bool _suppressBatteryApply;
    private string _toast = "";

    public SettingsForm(
        ILaptopHal hal,
        ModeControl modes,
        BatteryControl battery,
        DisplayControl display,
        WindowsSensors sensors,
        HardwareOverlay overlay,
        Icon appIcon)
    {
        _hal = hal;
        _modes = modes;
        _battery = battery;
        _display = display;
        _sensors = sensors;
        _overlay = overlay;
        _appIcon = appIcon;

        UiTheme.StyleForm(this);
        Text = "D-Helper";
        Icon = _appIcon;
        MinimumSize = new Size(400, 660);
        ClientSize = new Size(420, 720);
        Padding = new Padding(0);

        _root = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(22, 20, 18, 18),
            BackColor = UiTheme.Bg
        };
        Controls.Add(_root);

        BuildHeader();
        (_sensorCpu, _sensorBatt, _sensorPower, _sensorHz) = BuildSensors();
        BuildModes();
        (_chargeSelect, _chargeStop, _chargeStopValue, _chargeHint) = BuildBattery();
        (_autoThermal, _autoRefresh, _startup, _overlayChk, _modeAc, _modeDc) = BuildAutomation();
        _toastLabel = BuildToast();

        WireEvents();

        _sensorTimer = new System.Windows.Forms.Timer { Interval = 4000 };
        _sensorTimer.Tick += (_, _) => UpdateSensors();

        _batteryDebounce = new System.Windows.Forms.Timer { Interval = 250 };
        _batteryDebounce.Tick += (_, _) =>
        {
            _batteryDebounce.Stop();
            _ = ApplyBatteryAsync();
        };

        VisibleChanged += (_, _) =>
        {
            if (Visible) { UpdateSensors(); _sensorTimer.Start(); }
            else _sensorTimer.Stop();
        };
        Load += (_, _) =>
        {
            FitToContent();
            PositionNearTray();
            RefreshFromCache();
            if (_overlayChk.Checked) _overlay.SetEnabled(true);
        };
        Resize += (_, _) => LayoutAdaptive();
    }

    private void BuildHeader()
    {
        var header = new Panel
        {
            Height = 58,
            Margin = new Padding(0, 0, 0, 10),
            BackColor = Color.Transparent
        };
        TrackWidth(header);

        var title = new Label
        {
            Text = "D-Helper",
            Font = UiTheme.TitleFont,
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Location = new Point(0, 2)
        };
        var subtitle = new Label
        {
            Text = "Dell Pro · click the tray icon to open",
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(0, 34)
        };
        var more = UiTheme.Pill("···", new Size(42, 34));
        more.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        more.Click += (_, _) =>
        {
            using var f = new ExtraForm(_hal, _appIcon);
            f.ShowDialog(this);
        };
        header.Resize += (_, _) => more.Left = Math.Max(0, header.ClientSize.Width - more.Width);

        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(more);
        more.Left = header.Width - more.Width;
        _root.Controls.Add(header);
    }

    private (Label, Label, Label, Label) BuildSensors()
    {
        var card = UiTheme.Card(58);
        TrackWidth(card);

        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 2, 0, 0)
        };

        var cpu = Chip("CPU —");
        var batt = Chip("BAT —");
        var power = Chip("—");
        var hz = Chip("— Hz");
        row.Controls.AddRange(new Control[] { cpu, batt, power, hz });
        card.Controls.Add(row);
        _root.Controls.Add(card);
        return (cpu, batt, power, hz);
    }

    private void BuildModes()
    {
        var label = UiTheme.SectionLabel("Performance");
        TrackWidth(label);
        _root.Controls.Add(label);

        var modeRow = new FlowLayoutPanel
        {
            Height = 54,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(0)
        };
        TrackWidth(modeRow);

        var modes = new[]
        {
            (ThermalMode.Quiet, "Quiet"),
            (ThermalMode.Optimized, "Balanced"),
            (ThermalMode.Cool, "Cool"),
            (ThermalMode.Ultra, "Turbo")
        };

        foreach (var (mode, name) in modes)
        {
            var btn = UiTheme.Pill(name, new Size(80, 50));
            btn.Margin = new Padding(0, 0, 8, 0);
            btn.Click += (_, _) => ApplyMode(mode);
            _modeButtons[mode] = btn;
            modeRow.Controls.Add(btn);
        }
        _root.Controls.Add(modeRow);
    }

    private (DarkSelect, TrackBar, Label, Label) BuildBattery()
    {
        var label = UiTheme.SectionLabel("Battery limit");
        TrackWidth(label);
        _root.Controls.Add(label);

        var card = UiTheme.Card(148);
        TrackWidth(card);

        var title = new Label
        {
            Text = "Charge mode",
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(14, 12)
        };
        var value = new Label
        {
            Text = "80%",
            Font = UiTheme.TitleFont,
            ForeColor = UiTheme.Accent,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var select = new DarkSelect(
            new[] { "Custom", "PrimarilyAC", "Adaptive", "ExpressCharge", "Standard" },
            ContentWidth() - 28)
        {
            Location = new Point(14, 38)
        };

        var hint = new Label
        {
            Text = "Stop charging at",
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(14, 82)
        };

        var track = new TrackBar
        {
            Minimum = 55,
            Maximum = 100,
            TickFrequency = 5,
            Value = 80,
            Height = 40,
            Location = new Point(8, 102),
            BackColor = UiTheme.Surface,
            TickStyle = TickStyle.None
        };
        track.ValueChanged += (_, _) =>
        {
            value.Text = $"{track.Value}%";
            ScheduleBatteryApply();
        };

        select.SelectionChanged += (_, _) =>
        {
            bool custom = select.SelectedItem == "Custom";
            track.Enabled = custom;
            value.Visible = custom;
            hint.Text = custom ? "Stop charging at" : "Preset charging mode";
            ScheduleBatteryApply();
        };

        card.Controls.AddRange(new Control[] { title, value, select, hint, track });
        card.Resize += (_, _) =>
        {
            select.Width = Math.Max(140, card.ClientSize.Width - 28);
            track.Width = Math.Max(140, card.ClientSize.Width - 20);
            value.Left = card.ClientSize.Width - value.PreferredWidth - 14;
            value.Top = 10;
        };

        _root.Controls.Add(card);
        return (select, track, value, hint);
    }

    private (CheckBox, CheckBox, CheckBox, CheckBox, DarkSelect, DarkSelect) BuildAutomation()
    {
        var label = UiTheme.SectionLabel("Automation");
        TrackWidth(label);
        _root.Controls.Add(label);

        var card = UiTheme.Card(228);
        TrackWidth(card);

        var autoThermal = UiTheme.Toggle("Switch mode with AC / battery", AppConfig.Get("auto_thermal", true));
        autoThermal.Location = new Point(14, 14);

        var acLbl = new Label
        {
            Text = "On AC",
            ForeColor = UiTheme.Muted,
            Font = UiTheme.SmallFont,
            AutoSize = true,
            Location = new Point(14, 52)
        };
        var modeAc = new DarkSelect(new[] { "Quiet", "Optimized", "Cool", "Ultra" }, 150)
        {
            Location = new Point(14, 72)
        };

        var dcLbl = new Label
        {
            Text = "On battery",
            ForeColor = UiTheme.Muted,
            Font = UiTheme.SmallFont,
            AutoSize = true,
            Location = new Point(180, 52)
        };
        var modeDc = new DarkSelect(new[] { "Quiet", "Optimized", "Cool", "Ultra" }, 150)
        {
            Location = new Point(180, 72)
        };

        modeAc.SelectedItem = AppConfig.Get("thermal_mode_ac", "Optimized");
        modeDc.SelectedItem = AppConfig.Get("thermal_mode_dc", "Quiet");

        var autoRefresh = UiTheme.Toggle("Auto refresh rate (max on AC · 60 Hz on battery)", AppConfig.Get("auto_refresh", true));
        autoRefresh.Location = new Point(14, 120);
        var startup = UiTheme.Toggle("Start with Windows", StartupHelper.IsEnabled());
        startup.Location = new Point(14, 154);
        var overlay = UiTheme.Toggle("On-screen metrics overlay", AppConfig.Get("overlay", false));
        overlay.Location = new Point(14, 188);

        card.Controls.AddRange(new Control[]
        {
            autoThermal, acLbl, modeAc, dcLbl, modeDc, autoRefresh, startup, overlay
        });
        card.Resize += (_, _) =>
        {
            int half = Math.Max(120, (card.ClientSize.Width - 40) / 2);
            modeAc.Width = half;
            modeDc.Width = half;
            modeDc.Left = 14 + half + 12;
            dcLbl.Left = modeDc.Left;
        };

        _root.Controls.Add(card);
        return (autoThermal, autoRefresh, startup, overlay, modeAc, modeDc);
    }

    private Label BuildToast()
    {
        var toast = new Label
        {
            Height = 30,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(2, 10, 0, 0)
        };
        TrackWidth(toast);
        _root.Controls.Add(toast);
        return toast;
    }

    private void WireEvents()
    {
        _autoThermal.CheckedChanged += (_, _) => AppConfig.Set("auto_thermal", _autoThermal.Checked);
        _autoRefresh.CheckedChanged += (_, _) => AppConfig.Set("auto_refresh", _autoRefresh.Checked);
        _startup.CheckedChanged += (_, _) => StartupHelper.SetEnabled(_startup.Checked);
        _overlayChk.CheckedChanged += (_, _) =>
        {
            AppConfig.Set("overlay", _overlayChk.Checked);
            _overlay.SetEnabled(_overlayChk.Checked);
        };
        _modeAc.SelectionChanged += (_, _) =>
        {
            if (_modeAc.SelectedItem != null)
                AppConfig.Set("thermal_mode_ac", _modeAc.SelectedItem);
        };
        _modeDc.SelectionChanged += (_, _) =>
        {
            if (_modeDc.SelectedItem != null)
                AppConfig.Set("thermal_mode_dc", _modeDc.SelectedItem);
        };
        _modes.ModeChanged += (mode, err) =>
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(() =>
                {
                    HighlightMode(mode);
                    Toast(err ?? $"Switched to {DisplayName(mode)}");
                });
            }
            catch (InvalidOperationException) { /* closing */ }
        };
    }

    public void Toast(string message)
    {
        _toast = message;
        _toastLabel.ForeColor = message.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
                                message.Contains("Install", StringComparison.OrdinalIgnoreCase) ||
                                message.Contains("error", StringComparison.OrdinalIgnoreCase)
            ? UiTheme.Danger
            : UiTheme.Muted;
        _toastLabel.Text = _toast;
    }

    public void RefreshFromCache()
    {
        _suppressBatteryApply = true;
        try
        {
            HighlightMode(_modes.Current);
            _chargeSelect.SelectedItem = _battery.CurrentMode.ToString();
            _chargeStop.Value = Math.Clamp(_battery.CustomStop, 55, 100);
            _chargeStopValue.Text = $"{_chargeStop.Value}%";
            bool custom = _battery.CurrentMode == ChargeMode.Custom;
            _chargeStop.Enabled = custom;
            _chargeStopValue.Visible = custom;
            UpdateSensors();
            if (!_hal.Capabilities.ThermalControl && string.IsNullOrEmpty(_toast))
                Toast("Install Dell Optimizer or Command Configure");
        }
        finally
        {
            _suppressBatteryApply = false;
        }
    }

    private void UpdateSensors()
    {
        var s = _sensors.Read();
        _sensorCpu.Text = s.CpuTempC.HasValue ? $"CPU {s.CpuTempC}°" : "CPU —";
        _sensorBatt.Text = $"BAT {s.BatteryPercent}%";
        _sensorPower.Text = s.OnAc ? "AC" : "DC";
        _sensorHz.Text = $"{_display.GetCurrentRefreshRate()} Hz";
    }

    private void ApplyMode(ThermalMode mode)
    {
        if (!_hal.Capabilities.ThermalControl)
        {
            Toast("Install Dell Optimizer or Command Configure");
            return;
        }

        // Instant feedback — firmware write runs in the background.
        HighlightMode(mode);
        Toast($"Switched to {DisplayName(mode)}");
        _ = ApplyModeAsync(mode);
    }

    private async Task ApplyModeAsync(ThermalMode mode)
    {
        var (ok, error) = await _modes.ApplyAsync(mode).ConfigureAwait(true);
        if (!ok && !IsDisposed)
            Toast(string.IsNullOrWhiteSpace(error) ? "Mode change failed" : error);
    }

    private void ScheduleBatteryApply()
    {
        if (_suppressBatteryApply) return;
        _batteryDebounce.Stop();
        _batteryDebounce.Start();
    }

    private async Task ApplyBatteryAsync()
    {
        if (_suppressBatteryApply) return;
        if (!_hal.Capabilities.BatteryControl)
        {
            Toast("Install Dell Optimizer or Command Configure");
            return;
        }
        if (!Enum.TryParse<ChargeMode>(_chargeSelect.SelectedItem, out var mode))
            mode = ChargeMode.Custom;

        int stop = _chargeStop.Value;
        int start = Math.Max(50, stop - 30);
        if (stop - start < 5) start = Math.Max(50, stop - 5);

        Toast(mode == ChargeMode.Custom ? $"Charge limit {stop}%" : $"Charge → {mode}");
        var (ok, error) = await _battery.ApplyAsync(mode, start, stop).ConfigureAwait(true);
        if (!ok && !IsDisposed)
            Toast(string.IsNullOrWhiteSpace(error) ? "Battery change failed" : error);
    }

    private void HighlightMode(ThermalMode mode)
    {
        foreach (var (m, btn) in _modeButtons)
            UiTheme.SetPillActive(btn, m == mode);
    }

    private static string DisplayName(ThermalMode mode) => mode switch
    {
        ThermalMode.Quiet => "Quiet",
        ThermalMode.Optimized => "Balanced",
        ThermalMode.Cool => "Cool",
        ThermalMode.Ultra => "Turbo",
        _ => mode.ToString()
    };

    private static Label Chip(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = UiTheme.Text,
        Font = UiTheme.SmallFont,
        BackColor = UiTheme.SurfaceAlt,
        Padding = new Padding(10, 7, 10, 7),
        Margin = new Padding(0, 0, 8, 0)
    };

    private void TrackWidth(Control c) => _fullWidth.Add(c);

    private int ContentWidth() => Math.Max(340, ClientSize.Width - 52);

    private void LayoutAdaptive()
    {
        int w = ContentWidth();
        foreach (var c in _fullWidth)
            c.Width = w;

        // Stretch mode pills evenly across width
        if (_modeButtons.Count == 4)
        {
            int gap = 8;
            int btnW = Math.Max(70, (w - gap * 3) / 4);
            int i = 0;
            foreach (var btn in _modeButtons.Values)
            {
                btn.Width = btnW;
                btn.Margin = new Padding(0, 0, i < 3 ? gap : 0, 0);
                i++;
            }
        }
    }

    private void FitToContent()
    {
        var wa = Screen.FromControl(this).WorkingArea;
        float scale = DeviceDpi / 96f;
        int preferredW = Math.Clamp((int)(420 * scale), 400, Math.Min(480, wa.Width - 32));
        int preferredH = Math.Clamp((int)(720 * scale), 660, Math.Min(820, wa.Height - 48));
        ClientSize = new Size(preferredW, preferredH);
        LayoutAdaptive();
    }

    public void PositionNearTray()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        var wa = screen.WorkingArea;
        Left = wa.Right - Width - 16;
        Top = wa.Bottom - Height - 16;
        if (Left < wa.Left + 8) Left = wa.Left + 8;
        if (Top < wa.Top + 8) Top = wa.Top + 8;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sensorTimer.Dispose();
            _batteryDebounce.Dispose();
        }
        base.Dispose(disposing);
    }
}
