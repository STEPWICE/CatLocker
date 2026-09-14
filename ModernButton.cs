using System.Drawing.Drawing2D;

namespace CatLocker;

internal sealed class ModernButton : Button
{
    private bool hovering;
    private bool pressing;

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Size = new Size(104, 34);
        Cursor = Cursors.Hand;
        Primary = false;
    }

    public bool Primary { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        hovering = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hovering = false;
        pressing = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        pressing = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        pressing = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        Color fill = GetFillColor();
        Color border = Primary ? fill : ModernTheme.Border;
        Color text = Primary ? Color.White : ModernTheme.TextPrimary;

        using GraphicsPath path = ModernTheme.CreateRoundedRectangle(bounds, 8);
        using SolidBrush fillBrush = new(fill);
        using Pen borderPen = new(border, 1F);
        pevent.Graphics.FillPath(fillBrush, path);
        pevent.Graphics.DrawPath(borderPen, path);

        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            Font,
            bounds,
            text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private Color GetFillColor()
    {
        if (Primary)
        {
            if (pressing)
            {
                return ModernTheme.AccentPressed;
            }

            return hovering ? ModernTheme.AccentHover : ModernTheme.Accent;
        }

        if (pressing)
        {
            return Color.FromArgb(229, 233, 241);
        }

        return hovering ? Color.FromArgb(241, 245, 251) : Color.White;
    }
}
