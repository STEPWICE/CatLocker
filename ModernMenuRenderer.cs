using System.Drawing.Drawing2D;

namespace CatLocker;

internal sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
{
    // Cached: creating a Font per item-paint leaks GDI handles.
    private static readonly Font MenuFont = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

    public ModernMenuRenderer()
        : base(new ModernColorTable())
    {
        RoundedEdges = true;
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using Pen pen = new(ModernTheme.Border, 1F);
        Rectangle rectangle = new(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        e.Graphics.DrawRectangle(pen, rectangle);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? ModernTheme.TextPrimary : ModernTheme.TextMuted;
        e.TextFont = MenuFont;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        Rectangle rectangle = new(Point.Empty, e.Item.Size);
        if (e.Item.Selected && e.Item.Enabled)
        {
            // Soft pill highlight, inset so adjacent items keep air.
            Rectangle pill = Rectangle.Inflate(rectangle, -3, -2);
            if (pill.Width > 0 && pill.Height > 0)
            {
                using GraphicsPath path = ModernTheme.CreateRoundedRectangle(pill, ModernTheme.RadiusSmall);
                using SolidBrush brush = new(ModernTheme.MenuSelected);
                e.Graphics.FillPath(brush, path);
            }

            return;
        }

        using SolidBrush background = new(ModernTheme.MenuBackground);
        e.Graphics.FillRectangle(background, rectangle);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        // Soft rounded check background instead of the harsh default square.
        Rectangle check = new(4, 4, 18, 18);
        using GraphicsPath path = ModernTheme.CreateRoundedRectangle(check, 6);
        using SolidBrush brush = new(ModernTheme.MenuCheckBackground);
        e.Graphics.FillPath(brush, path);
        using Pen pen = new(ModernTheme.Accent, 1.6F);
        // Simple check mark.
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawLines(pen, new[]
        {
            new Point(9, 12),
            new Point(12, 15),
            new Point(17, 9)
        });
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using Pen pen = new(ModernTheme.SoftBorder, 1F);
        int y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 32, y, e.Item.Width - 10, y);
    }
}

internal sealed class ModernColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => ModernTheme.MenuBackground;
    public override Color ImageMarginGradientBegin => ModernTheme.MenuBackground;
    public override Color ImageMarginGradientMiddle => ModernTheme.MenuBackground;
    public override Color ImageMarginGradientEnd => ModernTheme.MenuBackground;
    public override Color MenuItemSelected => ModernTheme.MenuSelected;
    public override Color MenuItemBorder => ModernTheme.MenuSelected;
    public override Color CheckBackground => ModernTheme.MenuCheckBackground;
    public override Color CheckSelectedBackground => ModernTheme.MenuCheckBackground;
    public override Color CheckPressedBackground => ModernTheme.MenuCheckBackground;
    public override Color SeparatorDark => ModernTheme.SoftBorder;
    public override Color SeparatorLight => ModernTheme.SoftBorder;
}
