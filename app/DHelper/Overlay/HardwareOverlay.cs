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
        BackColor = Color.FromArgb(18, 22, 26);
        Opacity = 0.88;
        Width = 280;
        Height = 86;
        Location = new Point(20, 20);

        _label = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.25f),
            Padding = new Padding(12, 10, 12, 10),
            Text = "D-Helper"
        };
        Controls.Add(_label);

        _timer = new System.Windows.Forms.Timer { Interval = 1500 };
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
        var fan = s.FanRpm.HasValue ? $"FAN {s.FanRpm}" : "FAN —";
        var mode = _modes.Current switch
        {
            ThermalMode.Quiet => "Silent",
            ThermalMode.Optimized => "Balanced",
            ThermalMode.Cool => "Cool",
            ThermalMode.Ultra => "Turbo",
            _ => _modes.Current.ToString()
        };
        _label.Text = $"CPU {temp}   BAT {s.BatteryPercent}%\n{power}   {fan}   {mode}";
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
            cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
            return cp;
        }
    }
}
