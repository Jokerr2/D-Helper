using DHelper.Display;
using DHelper.Hardware;
using DHelper.Helpers;
using DHelper.Mode;
using DHelper.Overlay;

namespace DHelper.UI;

internal sealed class ExtraForm : Form
{
    private readonly ILaptopHal _hal;
    private readonly HardwareOverlay _overlay;
    private readonly Action? _onHotkeysChanged;

    public ExtraForm(
        ILaptopHal hal,
        ModeControl modes,
        DisplayControl display,
        HardwareOverlay overlay,
        Icon? icon = null,
        Action? onHotkeysChanged = null)
    {
        _ = modes;
        _ = display;
        _hal = hal;
        _overlay = overlay;
        _onHotkeysChanged = onHotkeysChanged;

        UiTheme.StyleForm(this);
        Text = "Extra";
        if (icon != null) Icon = icon;
        ClientSize = new Size(420, 520);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = true;

        var root = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(16, 14, 14, 14),
            BackColor = UiTheme.Bg
        };
        Controls.Add(root);

        root.Controls.Add(UiTheme.SectionLabel("General"));
        var gen = UiTheme.Section(130);
        gen.Width = 380;

        var startup = UiTheme.Toggle("Start with Windows", StartupHelper.IsEnabled());
        startup.Location = new Point(4, 4);
        startup.CheckedChanged += (_, _) => StartupHelper.SetEnabled(startup.Checked);

        var hotkeys = UiTheme.Toggle("Hotkeys (Ctrl+Shift+F5 cycle · F12 open)", AppConfig.Get("hotkeys", true));
        hotkeys.Location = new Point(4, 36);
        hotkeys.CheckedChanged += (_, _) =>
        {
            AppConfig.Set("hotkeys", hotkeys.Checked);
            _onHotkeysChanged?.Invoke();
        };

        var overlayChk = UiTheme.Toggle("On-screen metrics overlay", AppConfig.Get("overlay", false));
        overlayChk.Location = new Point(4, 68);
        overlayChk.CheckedChanged += (_, _) =>
        {
            AppConfig.Set("overlay", overlayChk.Checked);
            _overlay.SetEnabled(overlayChk.Checked);
        };

        var fn = UiTheme.Toggle("Fn-Lock", AppConfig.Get("fn_lock", false));
        fn.Location = new Point(4, 100);
        fn.Enabled = hal.Capabilities.FnLockControl;
        if (!fn.Enabled)
            fn.Text = "Fn-Lock (needs CCTK)";
        fn.CheckedChanged += async (_, _) =>
        {
            if (!hal.Capabilities.FnLockControl) return;
            var ok = await Task.Run(() => _hal.SetFnLock(fn.Checked, out _)).ConfigureAwait(true);
            if (ok) AppConfig.Set("fn_lock", fn.Checked);
            else fn.Checked = AppConfig.Get("fn_lock", false);
        };

        gen.Controls.AddRange(new Control[] { startup, hotkeys, overlayChk, fn });
        root.Controls.Add(gen);

        root.Controls.Add(UiTheme.SectionLabel("Automation"));
        var auto = UiTheme.Section(160);
        auto.Width = 380;

        var autoThermal = UiTheme.Toggle("Switch mode with AC / battery", AppConfig.Get("auto_thermal", true));
        autoThermal.Location = new Point(4, 4);
        autoThermal.CheckedChanged += (_, _) => AppConfig.Set("auto_thermal", autoThermal.Checked);

        var acLbl = new Label { Text = "On AC", ForeColor = UiTheme.Muted, Font = UiTheme.SmallFont, AutoSize = true, Location = new Point(4, 40) };
        var modeAc = new DarkSelect(new[] { "Quiet", "Optimized", "Cool", "Ultra" }, 160) { Location = new Point(4, 60) };
        modeAc.SelectedItem = AppConfig.Get("thermal_mode_ac", "Optimized");
        modeAc.SelectionChanged += (_, _) =>
        {
            if (modeAc.SelectedItem != null) AppConfig.Set("thermal_mode_ac", modeAc.SelectedItem);
        };

        var dcLbl = new Label { Text = "On battery", ForeColor = UiTheme.Muted, Font = UiTheme.SmallFont, AutoSize = true, Location = new Point(190, 40) };
        var modeDc = new DarkSelect(new[] { "Quiet", "Optimized", "Cool", "Ultra" }, 160) { Location = new Point(190, 60) };
        modeDc.SelectedItem = AppConfig.Get("thermal_mode_dc", "Quiet");
        modeDc.SelectionChanged += (_, _) =>
        {
            if (modeDc.SelectedItem != null) AppConfig.Set("thermal_mode_dc", modeDc.SelectedItem);
        };

        var autoRefresh = UiTheme.Toggle("Auto refresh rate (max on AC · 60 on battery)", AppConfig.Get("auto_refresh", true));
        autoRefresh.Location = new Point(4, 104);
        autoRefresh.CheckedChanged += (_, _) => AppConfig.Set("auto_refresh", autoRefresh.Checked);

        auto.Controls.AddRange(new Control[] { autoThermal, acLbl, modeAc, dcLbl, modeDc, autoRefresh });
        root.Controls.Add(auto);

        root.Controls.Add(UiTheme.SectionLabel("Backend status"));
        var caps = hal.Capabilities;
        var status = UiTheme.Section(110);
        status.Width = 380;
        status.Controls.Add(new Label
        {
            Text =
                $"Primary   {caps.PrimaryBackend}\n" +
                $"Optimizer {(caps.OptimizerAvailable ? "ready" : "missing")}\n" +
                $"CCTK      {(caps.CctkAvailable ? "ready" : "missing")}\n" +
                $"Battery   CCTK PrimaryBattChargeCfg (no Optimizer ChargingMode)",
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Location = new Point(4, 8)
        });
        root.Controls.Add(status);

        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }
}
