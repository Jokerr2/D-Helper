namespace DHelper.UI;

internal static class UiTheme
{
    public static readonly Color Bg = Color.FromArgb(22, 24, 28);
    public static readonly Color Surface = Color.FromArgb(34, 38, 44);
    public static readonly Color SurfaceAlt = Color.FromArgb(42, 47, 54);
    public static readonly Color Border = Color.FromArgb(58, 64, 72);
    public static readonly Color Text = Color.FromArgb(236, 239, 244);
    public static readonly Color Muted = Color.FromArgb(140, 148, 160);
    public static readonly Color Accent = Color.FromArgb(46, 168, 176);
    public static readonly Color AccentSoft = Color.FromArgb(28, 72, 78);
    public static readonly Color Danger = Color.FromArgb(200, 90, 90);
    public static readonly Color ColorSilent = Color.FromArgb(72, 160, 120);
    public static readonly Color ColorBalanced = Color.FromArgb(80, 140, 200);
    public static readonly Color ColorTurbo = Color.FromArgb(200, 110, 70);
    public static readonly Color ColorCool = Color.FromArgb(90, 170, 190);

    public const string UnavailableTip = "Not available on Dell Pro";

    public static Font TitleFont { get; } = new("Segoe UI Semibold", 13f);
    public static Font BodyFont { get; } = new("Segoe UI", 9.25f);
    public static Font SmallFont { get; } = new("Segoe UI", 8.5f);
    public static Font ModeFont { get; } = new("Segoe UI Semibold", 9f);
    public static Font SectionFont { get; } = new("Segoe UI Semibold", 9f);

    public static void SpanForm(Form form)
    {
        form.BackColor = Bg;
        form.ForeColor = Text;
        form.Font = BodyFont;
        form.FormBorderStyle = FormBorderStyle.FixedSingle;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ShowInTaskbar = true;
        form.StartPosition = FormStartPosition.Manual;
        form.AutoScaleMode = AutoScaleMode.Dpi;
    }

    public static void StyleForm(Form form) => SpanForm(form);

    public static Button Pill(string text, Size size, Color? borderColor = null)
    {
        var b = new Button
        {
            Text = text,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            BackColor = SurfaceAlt,
            ForeColor = Text,
            Font = ModeFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 6, 0),
            Padding = Padding.Empty,
            TabStop = true
        };
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = borderColor ?? Border;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 58, 66);
        b.FlatAppearance.MouseDownBackColor = AccentSoft;
        return b;
    }

    public static void SetPillActive(Button b, bool active, Color? accentBorder = null)
    {
        b.BackColor = active ? AccentSoft : SurfaceAlt;
        b.ForeColor = active ? Color.FromArgb(200, 240, 245) : Text;
        b.FlatAppearance.BorderColor = active ? (accentBorder ?? Accent) : Border;
    }

    public static void SetDisabled(Control c, ToolTip tip, string reason = UnavailableTip)
    {
        c.Enabled = false;
        tip.SetToolTip(c, reason);
    }

    public static CheckBox Toggle(string text, bool value) => new()
    {
        Text = text,
        Checked = value,
        AutoSize = true,
        ForeColor = Text,
        Font = BodyFont,
        Margin = new Padding(0, 6, 0, 0),
        Cursor = Cursors.Hand,
        BackColor = Color.Transparent
    };

    public static Label SectionLabel(string text) => new()
    {
        Text = text,
        Font = SectionFont,
        ForeColor = Text,
        AutoSize = true,
        Margin = new Padding(2, 10, 0, 6)
    };

    public static Panel Section(int height) => new()
    {
        Width = 360,
        Height = height,
        BackColor = Surface,
        Margin = new Padding(0, 0, 0, 8),
        Padding = new Padding(12, 10, 12, 10)
    };

    public static Panel Card(int height) => Section(height);

    public static Label Chip(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Text,
        Font = SmallFont,
        BackColor = SurfaceAlt,
        Padding = new Padding(8, 5, 8, 5),
        Margin = new Padding(0, 0, 6, 0)
    };

    public static Icon CreateAppIcon()
    {
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var bg = new SolidBrush(Color.FromArgb(28, 32, 38));
            using var accent = new SolidBrush(Accent);
            using var textBrush = new SolidBrush(Color.White);
            g.FillEllipse(bg, 1, 1, 30, 30);
            g.FillEllipse(accent, 7, 7, 18, 18);
            using var font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("D", font, textBrush, new RectangleF(0, 1, 32, 32), sf);
        }
        var hIcon = bmp.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        var clone = (Icon)icon.Clone();
        DestroyIcon(hIcon);
        bmp.Dispose();
        return clone;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
}
