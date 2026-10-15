using DHelper.Hardware;
using DHelper.Mode;

namespace DHelper.UI;

internal sealed class FansPowerForm : Form
{
    private readonly ModeControl _modes;
    private readonly ToolTip _tip = new() { ShowAlways = true };

    public FansPowerForm(ILaptopHal hal, ModeControl modes, Icon? icon = null)
    {
        _modes = modes;
        UiTheme.StyleForm(this);
        Text = "Fans + Power";
        if (icon != null) Icon = icon;
        ClientSize = new Size(420, 420);
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

        root.Controls.Add(UiTheme.SectionLabel("Windows power"));
        var powerCard = UiTheme.Section(100);
        powerCard.Width = 380;
        var note = new Label
        {
            Text = "Pairs thermal mode with Windows power overlay\n(Best performance / Balanced / Better battery).",
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(4, 4)
        };
        var apply = UiTheme.Pill("Apply Windows power for current mode", new Size(300, 36));
        apply.Location = new Point(4, 52);
        apply.Click += (_, _) =>
        {
            WindowsSensors.SetWindowsPowerOverlay(_modes.Current);
            MessageBox.Show(this, $"Windows power overlay synced for {_modes.Current}.", "Fans + Power",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        powerCard.Controls.Add(note);
        powerCard.Controls.Add(apply);
        root.Controls.Add(powerCard);

        root.Controls.Add(UiTheme.SectionLabel("Thermal"));
        var thermalCard = UiTheme.Section(70);
        thermalCard.Width = 380;
        var thermal = new Label
        {
            Text = hal.Capabilities.ThermalControl
                ? $"Backend: {hal.Capabilities.PrimaryBackend}\nCurrent mode: {_modes.Current}"
                : "No thermal backend — install Optimizer or CCTK.",
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Location = new Point(4, 8)
        };
        thermalCard.Controls.Add(thermal);
        root.Controls.Add(thermalCard);

        root.Controls.Add(UiTheme.SectionLabel("Fan curve / PPT"));
        var fanCard = UiTheme.Section(120);
        fanCard.Width = 380;
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        foreach (var name in new[] { "CPU curve", "GPU curve", "PPT limit", "Boost clocks" })
        {
            var b = UiTheme.Pill(name, new Size(110, 40));
            UiTheme.SetDisabled(b, _tip, "Fan curves / PPT not available on Dell Pro (no AWCC)");
            row.Controls.Add(b);
        }
        var hint = new Label
        {
            Text = "Visible for G-Helper layout parity — disabled on PC16250.",
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Margin = new Padding(4, 8, 0, 0)
        };
        fanCard.Controls.Add(row);
        root.Controls.Add(fanCard);
        root.Controls.Add(hint);

        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }
}
