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
    private readonly Icon _appIcon;
    private readonly ToolTip _tip = new() { ShowAlways = true };
    private readonly System.Windows.Forms.Timer _sensorTimer;
    private readonly System.Windows.Forms.Timer _batteryDebounce;

    private readonly Label _modelLabel;
    private readonly Label _sensorCpu;
    private readonly Label _sensorBatt;
    private readonly Label _sensorPower;
    private readonly Label _sensorHz;
    private readonly Label _sensorFan;
    private readonly Dictionary<ThermalMode, Button> _modeButtons = new();
    private readonly DarkSelect _chargeSelect;
    private readonly TrackBar _chargeStop;
    private readonly Label _chargeStopValue;
    private readonly CheckBox _startup;
    private readonly Label _toastLabel;
    private readonly FlowLayoutPanel _root;
    private readonly List<Control> _fullWidth = new();
    private FansPowerForm? _fansForm;
    private ExtraForm? _extraForm;
    private UpdatesForm? _updatesForm;
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
        MinimumSize = new Size(420, 700);
        ClientSize = new Size(440, 780);
        Padding = new Padding(0);

        _root = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(14, 12, 10, 12),
            BackColor = UiTheme.Bg
        };
        Controls.Add(_root);

        (_modelLabel, _sensorCpu, _sensorBatt, _sensorPower, _sensorHz, _sensorFan) = BuildHeaderSensors();
        BuildPerformance();
        BuildGpuRow();
        BuildScreenRow();
        BuildKeyboardRow();
        (_chargeSelect, _chargeStop, _chargeStopValue) = BuildBattery();
        BuildLightingDisabled();
        (_startup, _toastLabel) = BuildFooter();

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
            if (AppConfig.Get("overlay", false)) _overlay.SetEnabled(true);
        };
        Resize += (_, _) => LayoutAdaptive();
    }

    private (Label, Label, Label, Label, Label, Label) BuildHeaderSensors()
    {
        var card = UiTheme.Section(78);
        TrackWidth(card);

        var model = new Label
        {
            Text = "Dell Pro 16 PC16250",
            Font = UiTheme.TitleFont,
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Location = new Point(4, 2)
        };
        var row = new FlowLayoutPanel
        {
            Location = new Point(0, 36),
            Height = 34,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent
        };
        var cpu = UiTheme.Chip("CPU —");
        var batt = UiTheme.Chip("BAT —");
        var power = UiTheme.Chip("—");
        var hz = UiTheme.Chip("— Hz");
        var fan = UiTheme.Chip("FAN —");
        row.Controls.AddRange(new Control[] { cpu, batt, power, hz, fan });
        card.Controls.Add(model);
        card.Controls.Add(row);
        card.Resize += (_, _) => row.Width = Math.Max(200, card.ClientSize.Width - 8);
        _root.Controls.Add(card);
        return (model, cpu, batt, power, hz, fan);
    }

    private void BuildPerformance()
    {
        var title = UiTheme.SectionLabel("Performance");
        TrackWidth(title);
        _root.Controls.Add(title);

        var card = UiTheme.Section(108);
        TrackWidth(card);

        var modeRow = new FlowLayoutPanel
        {
            Location = new Point(0, 0),
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var modes = new (ThermalMode Mode, string Name, Color Border)[]
        {
            (ThermalMode.Quiet, "Silent", UiTheme.ColorSilent),
            (ThermalMode.Optimized, "Balanced", UiTheme.ColorBalanced),
            (ThermalMode.Cool, "Cool", UiTheme.ColorCool),
            (ThermalMode.Ultra, "Turbo", UiTheme.ColorTurbo)
        };

        foreach (var (mode, name, border) in modes)
        {
            var btn = UiTheme.Pill(name, new Size(84, 44), border);
            btn.Tag = border;
            btn.Click += (_, _) => ApplyMode(mode);
            _modeButtons[mode] = btn;
            modeRow.Controls.Add(btn);
        }

        var fans = UiTheme.Pill("Fans + Power", new Size(120, 40));
        fans.Location = new Point(0, 56);
        fans.Click += (_, _) => OpenFans();

        card.Controls.Add(modeRow);
        card.Controls.Add(fans);
        card.Resize += (_, _) =>
        {
            modeRow.Width = Math.Max(200, card.ClientSize.Width);
            StretchModePills(modeRow.Width);
        };
        _root.Controls.Add(card);
    }

    private void BuildGpuRow()
    {
        var title = UiTheme.SectionLabel("GPU Mode");
        TrackWidth(title);
        _root.Controls.Add(title);

        var card = UiTheme.Section(56);
        TrackWidth(card);
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        foreach (var name in new[] { "Eco", "Standard", "Ultimate", "Optimized" })
        {
            var b = UiTheme.Pill(name, new Size(88, 40));
            UiTheme.SetDisabled(b, _tip, "iGPU only — GPU MUX not available on Dell Pro");
            row.Controls.Add(b);
        }
        card.Controls.Add(row);
        _root.Controls.Add(card);
    }

    private void BuildScreenRow()
    {
        var title = UiTheme.SectionLabel("Screen");
        TrackWidth(title);
        _root.Controls.Add(title);

        var card = UiTheme.Section(56);
        TrackWidth(card);
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };
        foreach (var name in new[] { "60 Hz", "Max Hz", "Auto", "Overdrive", "MiniLED" })
        {
            var b = UiTheme.Pill(name, new Size(72, 40));
            if (name is "60 Hz" or "Max Hz" or "Auto")
            {
                b.Enabled = true;
                b.Click += (_, _) => ApplyScreenQuick(name);
                _tip.SetToolTip(b, "Uses Windows refresh-rate control");
            }
            else
            {
                UiTheme.SetDisabled(b, _tip, "Not available on Dell Pro");
            }
            row.Controls.Add(b);
        }
        card.Controls.Add(row);
        _root.Controls.Add(card);
    }

    private void BuildKeyboardRow()
    {
        var title = UiTheme.SectionLabel("Laptop");
        TrackWidth(title);
        _root.Controls.Add(title);

        var card = UiTheme.Section(56);
        TrackWidth(card);
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        var extra = UiTheme.Pill("Extra", new Size(88, 40));
        extra.Click += (_, _) => OpenExtra();

        var fn = UiTheme.Pill("Fn-Lock", new Size(88, 40));
        if (_hal.Capabilities.FnLockControl)
        {
            fn.Click += async (_, _) =>
            {
                bool next = !AppConfig.Get("fn_lock", false);
                Toast(next ? "Fn-Lock on…" : "Fn-Lock off…");
                var ok = await Task.Run(() => _hal.SetFnLock(next, out var err)).ConfigureAwait(true);
                if (ok)
                {
                    AppConfig.Set("fn_lock", next);
                    UiTheme.SetPillActive(fn, next);
                    Toast(next ? "Fn-Lock enabled" : "Fn-Lock disabled");
                }
                else Toast(string.IsNullOrWhiteSpace(_hal.Capabilities.LastError) ? "Fn-Lock failed" : _hal.Capabilities.LastError);
            };
            UiTheme.SetPillActive(fn, AppConfig.Get("fn_lock", false));
        }
        else
        {
            UiTheme.SetDisabled(fn, _tip, "Needs Dell Command | Configure (cctk)");
        }

        var overlay = UiTheme.Pill("Overlay", new Size(88, 40));
        overlay.Click += (_, _) =>
        {
            bool on = !AppConfig.Get("overlay", false);
            AppConfig.Set("overlay", on);
            _overlay.SetEnabled(on);
            UiTheme.SetPillActive(overlay, on);
            Toast(on ? "Overlay on" : "Overlay off");
        };
        UiTheme.SetPillActive(overlay, AppConfig.Get("overlay", false));

        var lighting = UiTheme.Pill("Lighting", new Size(88, 40));
        UiTheme.SetDisabled(lighting, _tip, "RGB / Anime Matrix not available on Dell Pro");

        row.Controls.AddRange(new Control[] { extra, fn, overlay, lighting });
        card.Controls.Add(row);
        _root.Controls.Add(card);
    }

    private (DarkSelect, TrackBar, Label) BuildBattery()
    {
        var title = UiTheme.SectionLabel("Battery charge limit");
        TrackWidth(title);
        _root.Controls.Add(title);

        var card = UiTheme.Section(130);
        TrackWidth(card);

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
            Location = new Point(4, 8)
        };

        var hint = new Label
        {
            Text = "Stop charging at",
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(4, 48)
        };

        var track = new TrackBar
        {
            Minimum = 55,
            Maximum = 100,
            TickFrequency = 5,
            Value = 80,
            Height = 36,
            Location = new Point(0, 72),
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

        var full = UiTheme.Pill("100%", new Size(56, 28));
        full.Font = UiTheme.SmallFont;
        full.Click += (_, _) =>
        {
            select.SelectedItem = "Custom";
            track.Value = 100;
            ScheduleBatteryApply();
        };

        card.Controls.AddRange(new Control[] { select, value, hint, track, full });
        card.Resize += (_, _) =>
        {
            select.Width = Math.Max(140, card.ClientSize.Width - 70);
            track.Width = Math.Max(140, card.ClientSize.Width - 12);
            value.Left = card.ClientSize.Width - value.PreferredWidth - 8;
            value.Top = 10;
            full.Left = card.ClientSize.Width - full.Width - 4;
            full.Top = 44;
        };
        _root.Controls.Add(card);
        return (select, track, value);
    }

    private void BuildLightingDisabled()
    {
        var title = UiTheme.SectionLabel("Visual / Matrix");
        TrackWidth(title);
        _root.Controls.Add(title);

        var card = UiTheme.Section(52);
        TrackWidth(card);
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };
        foreach (var name in new[] { "Visual mode", "Anime Matrix", "Peripherals" })
        {
            var b = UiTheme.Pill(name, new Size(110, 36));
            UiTheme.SetDisabled(b, _tip, "Not available on Dell Pro");
            row.Controls.Add(b);
        }
        card.Controls.Add(row);
        _root.Controls.Add(card);
    }

    private (CheckBox, Label) BuildFooter()
    {
        var card = UiTheme.Section(96);
        TrackWidth(card);

        var startup = UiTheme.Toggle("Start with Windows", StartupHelper.IsEnabled());
        startup.Location = new Point(4, 4);

        var updates = UiTheme.Pill("Updates", new Size(96, 36));
        updates.Location = new Point(4, 40);
        updates.Click += (_, _) => OpenUpdates();

        var quit = UiTheme.Pill("Quit", new Size(72, 36));
        quit.Click += (_, _) =>
        {
            Hide();
            Application.Exit();
        };

        var toast = new Label
        {
            Height = 36,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(110, 42),
            AutoEllipsis = true
        };

        card.Controls.AddRange(new Control[] { startup, updates, quit, toast });
        card.Resize += (_, _) =>
        {
            quit.Left = card.ClientSize.Width - quit.Width - 4;
            quit.Top = 40;
            toast.Left = updates.Right + 10;
            toast.Width = Math.Max(80, quit.Left - toast.Left - 8);
            toast.Top = 42;
        };
        _root.Controls.Add(card);
        return (startup, toast);
    }

    private void WireEvents()
    {
        _startup.CheckedChanged += (_, _) => StartupHelper.SetEnabled(_startup.Checked);
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

    private void OpenFans()
    {
        if (_fansForm == null || _fansForm.IsDisposed)
            _fansForm = new FansPowerForm(_hal, _modes, _appIcon);
        _fansForm.Show();
        _fansForm.BringToFront();
    }

    private void OpenExtra()
    {
        if (_extraForm == null || _extraForm.IsDisposed)
            _extraForm = new ExtraForm(_hal, _modes, _display, _overlay, _appIcon, Program.SyncHotkeys);
        _extraForm.Show();
        _extraForm.BringToFront();
    }

    private void OpenUpdates()
    {
        if (_updatesForm == null || _updatesForm.IsDisposed)
            _updatesForm = new UpdatesForm(_hal, _appIcon);
        _updatesForm.Show();
        _updatesForm.BringToFront();
    }

    private void ApplyScreenQuick(string name)
    {
        try
        {
            if (name == "60 Hz")
            {
                _display.SetRefreshRate(60);
                Toast("60 Hz");
            }
            else if (name == "Max Hz")
            {
                var max = _display.GetMaxRefreshRate();
                _display.SetRefreshRate(max);
                Toast($"Max {max} Hz");
            }
            else
            {
                AppConfig.Set("auto_refresh", true);
                _display.ApplyForPowerState(SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online);
                Toast("Auto refresh by power state");
            }
            UpdateSensors();
        }
        catch (Exception ex)
        {
            Toast(ex.Message);
        }
    }

    public void Toast(string message)
    {
        _toast = DellErrors.Humanize(message);
        bool bad = _toast.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
                   _toast.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
                   _toast.Contains("denied", StringComparison.OrdinalIgnoreCase) ||
                   _toast.Contains("Install", StringComparison.OrdinalIgnoreCase) ||
                   _toast.Contains("Administrator", StringComparison.OrdinalIgnoreCase) ||
                   _toast.Contains("password", StringComparison.OrdinalIgnoreCase);
        _toastLabel.ForeColor = bad ? UiTheme.Danger : UiTheme.Muted;
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
            _startup.Checked = StartupHelper.IsEnabled();
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
        _sensorFan.Text = s.FanRpm.HasValue ? $"FAN {s.FanRpm}" : "FAN —";
        _modelLabel.Text = "Dell Pro 16 PC16250";
    }

    private void ApplyMode(ThermalMode mode)
    {
        if (!_hal.Capabilities.ThermalControl)
        {
            Toast("Install Dell Optimizer or Command Configure");
            return;
        }
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
            Toast("Install Dell Command | Configure for battery control");
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
        {
            var border = btn.Tag as Color? ?? UiTheme.Accent;
            UiTheme.SetPillActive(btn, m == mode, border);
        }
    }

    private static string DisplayName(ThermalMode mode) => mode switch
    {
        ThermalMode.Quiet => "Silent",
        ThermalMode.Optimized => "Balanced",
        ThermalMode.Cool => "Cool",
        ThermalMode.Ultra => "Turbo",
        _ => mode.ToString()
    };

    private void TrackWidth(Control c) => _fullWidth.Add(c);
    private int ContentWidth() => Math.Max(360, ClientSize.Width - 40);

    private void StretchModePills(int width)
    {
        if (_modeButtons.Count == 0) return;
        int gap = 6;
        int n = _modeButtons.Count;
        int btnW = Math.Max(64, (width - gap * (n - 1) - 8) / n);
        int i = 0;
        foreach (var btn in _modeButtons.Values)
        {
            btn.Width = btnW;
            btn.Margin = new Padding(0, 0, i < n - 1 ? gap : 0, 0);
            i++;
        }
    }

    private void LayoutAdaptive()
    {
        int w = ContentWidth();
        foreach (var c in _fullWidth) c.Width = w;
        StretchModePills(w);
    }

    private void FitToContent()
    {
        var wa = Screen.FromControl(this).WorkingArea;
        float scale = DeviceDpi / 96f;
        int preferredW = Math.Clamp((int)(440 * scale), 420, Math.Min(520, wa.Width - 32));
        int preferredH = Math.Clamp((int)(780 * scale), 700, Math.Min(900, wa.Height - 48));
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
            _tip.Dispose();
            _fansForm?.Dispose();
            _extraForm?.Dispose();
            _updatesForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}
