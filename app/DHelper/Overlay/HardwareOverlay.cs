using DHelper.Hardware;
using DHelper.Mode;

namespace DHelper.Overlay;

internal sealed class HardwareOverlay : Form
{
    private readonly WindowsSensors _sensors;
    private readonly ModeControl _modes;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Label _label;

    public HardwareOverlay(WindowsSensors sensors, ModeControl modes)
    {
        _sensors = sensors;
        _modes = modes;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(20, 24, 28);
        Opacity = 0.85;
        Width = 260;
        Height = 72;
        Location = new Point(20, 20);

        _label = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5f),
            Padding = new Padding(12),
            Text = "D-Helper"
        };
        Controls.Add(_label);

        _timer = new System.Windows.Forms.Timer { Interval = 2500 };
        _timer.Tick += (_, _) => RefreshText();
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            Show();
            _timer.Start();
            RefreshText();
        }
        else
        {
            _timer.Stop();
            Hide();
        }
    }

    private void RefreshText()
    {
        var s = _sensors.Read();
        var temp = s.CpuTempC.HasValue ? $"{s.CpuTempC}°C" : "—";
        var power = s.OnAc ? "AC" : "Battery";
        _label.Text = $"CPU {temp}   Batt {s.BatteryPercent}%\n{power}   {_modes.Current}";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            SetEnabled(false);
            AppConfig.Set("overlay", false);
        }
        base.OnFormClosing(e);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80;
            return cp;
        }
    }
}
