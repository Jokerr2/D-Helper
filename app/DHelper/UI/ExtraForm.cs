using System.Diagnostics;
using DHelper.Hardware;

namespace DHelper.UI;

internal sealed class ExtraForm : Form
{
    public ExtraForm(ILaptopHal hal, Icon? icon = null)
    {
        UiTheme.StyleForm(this);
        Text = "Extras";
        if (icon != null) Icon = icon;
        ClientSize = new Size(360, 340);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;

        var caps = hal.Capabilities;
        var body = new Label
        {
            Location = new Point(18, 16),
            Size = new Size(324, 150),
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Text,
            Text =
                "Connected backends\n\n" +
                $"Primary   {caps.PrimaryBackend}\n" +
                $"Optimizer {(caps.OptimizerAvailable ? "ready" : "missing")}\n" +
                $"CCTK      {(caps.CctkAvailable ? "ready" : "missing")}\n\n" +
                "Not available on this Dell Pro\n" +
                "Fan curves · GPU MUX · RGB"
        };

        Controls.Add(body);
        Controls.Add(Link("Dell drivers for PC16250", 18, 180,
            "https://www.dell.com/support/home/en-us/product-support/product/dell-pro-pc16250-laptop/drivers"));
        Controls.Add(Link("Dell Optimizer", 18, 220,
            "https://www.dell.com/support/kbdoc/en-us/000273253/dell-optimizer-6-0-overview-and-common-questions"));
        Controls.Add(Link("Dell Command | Configure", 18, 260,
            "https://www.dell.com/support/kbdoc/en-us/000178000/dell-command-configure"));

        var close = UiTheme.Pill("Done", new Size(80, 30));
        close.Location = new Point(262, 300);
        close.DialogResult = DialogResult.OK;
        Controls.Add(close);
        AcceptButton = close;
    }

    private static Button Link(string text, int x, int y, string url)
    {
        var b = UiTheme.Pill(text, new Size(324, 32));
        b.Location = new Point(x, y);
        b.TextAlign = ContentAlignment.MiddleLeft;
        b.Padding = new Padding(10, 0, 0, 0);
        b.Click += (_, _) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        return b;
    }
}
