namespace DHelper.UI;

/// <summary>Dark-themed dropdown replacement (avoids WinForms ComboBox light chrome).</summary>
internal sealed class DarkSelect : Button
{
    private readonly string[] _items;
    private int _index;
    private readonly ContextMenuStrip _menu;

    public event EventHandler? SelectionChanged;

    public DarkSelect(string[] items, int width)
    {
        _items = items;
        _index = 0;
        FlatStyle = FlatStyle.Flat;
        BackColor = UiTheme.SurfaceAlt;
        ForeColor = UiTheme.Text;
        Font = UiTheme.BodyFont;
        Size = new Size(width, 32);
        TextAlign = ContentAlignment.MiddleLeft;
        Padding = new Padding(10, 0, 28, 0);
        Cursor = Cursors.Hand;
        FlatAppearance.BorderSize = 1;
        FlatAppearance.BorderColor = UiTheme.Border;
        FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 58, 66);

        _menu = new ContextMenuStrip
        {
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.Text,
            ShowImageMargin = false,
            Renderer = new ToolStripProfessionalRenderer(new DarkTable())
        };
        foreach (var item in _items)
        {
            var capture = item;
            _menu.Items.Add(capture, null, (_, _) =>
            {
                SelectedItem = capture;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        Click += (_, _) =>
        {
            _menu.Show(this, new Point(0, Height));
        };
        UpdateText();
    }

    public string? SelectedItem
    {
        get => _index >= 0 && _index < _items.Length ? _items[_index] : null;
        set
        {
            var i = Array.FindIndex(_items, x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
            if (i < 0) return;
            _index = i;
            UpdateText();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Chevron
        using var pen = new Pen(UiTheme.Muted, 1.5f);
        int x = Width - 18;
        int y = Height / 2;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.DrawLines(pen, new[]
        {
            new Point(x - 4, y - 2),
            new Point(x, y + 2),
            new Point(x + 4, y - 2)
        });
    }

    private void UpdateText() => Text = SelectedItem ?? "";

    private sealed class DarkTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => UiTheme.AccentSoft;
        public override Color MenuItemBorder => UiTheme.Border;
        public override Color ToolStripDropDownBackground => UiTheme.Surface;
        public override Color ImageMarginGradientBegin => UiTheme.Surface;
        public override Color ImageMarginGradientMiddle => UiTheme.Surface;
        public override Color ImageMarginGradientEnd => UiTheme.Surface;
        public override Color SeparatorDark => UiTheme.Border;
    }
}
