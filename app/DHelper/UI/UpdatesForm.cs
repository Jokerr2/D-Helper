using System.Diagnostics;
using DHelper.Hardware;

namespace DHelper.UI;

internal sealed class UpdatesForm : Form
{
    public UpdatesForm(ILaptopHal hal, Icon? icon = null)
    {
        UiTheme.StyleForm(this);
        Text = "Updates";
        if (icon != null) Icon = icon;
        ClientSize = new Size(420, 360);
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

        root.Controls.Add(UiTheme.SectionLabel("Dell Pro 16 PC16250"));
        var intro = new Label
        {
            Text = "Open Dell Support for BIOS and drivers.\n(ASUS scrape is not used — this is a Dell machine.)",
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Margin = new Padding(2, 0, 0, 12),
            MaximumSize = new Size(380, 0)
        };
        root.Controls.Add(intro);

        root.Controls.Add(Link("Drivers & downloads (PC16250)",
            "https://www.dell.com/support/home/en-us/product-support/product/dell-pro-pc16250-laptop/drivers"));
        root.Controls.Add(Link("BIOS / firmware",
            "https://www.dell.com/support/home/en-us/product-support/product/dell-pro-pc16250-laptop/drivers"));
        root.Controls.Add(Link("Dell Optimizer",
            "https://www.dell.com/support/kbdoc/en-us/000273253/dell-optimizer-6-0-overview-and-common-questions"));
        root.Controls.Add(Link("Dell Command | Configure",
            "https://www.dell.com/support/kbdoc/en-us/000178000/dell-command-configure"));
        root.Controls.Add(Link("SupportAssist",
            "https://www.dell.com/support/contents/en-us/article/product-support/self-support-knowledgebase/software-and-downloads/supportassist"));

        var backend = new Label
        {
            Text = $"Current backend: {hal.Capabilities.PrimaryBackend}",
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Margin = new Padding(2, 16, 0, 0)
        };
        root.Controls.Add(backend);

        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    private static Button Link(string text, string url)
    {
        var b = UiTheme.Pill(text, new Size(380, 36));
        b.TextAlign = ContentAlignment.MiddleLeft;
        b.Padding = new Padding(12, 0, 0, 0);
        b.Margin = new Padding(0, 0, 0, 8);
        b.Click += (_, _) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        return b;
    }
}
