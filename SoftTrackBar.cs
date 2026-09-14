using System.Drawing.Drawing2D;

namespace CatLocker;

/// <summary>
/// Owner-drawn slider matching the soft theme in both light and dark modes.
/// Rounded track (<see cref="ModernTheme.TrackBack"/>), filled portion
/// (<see cref="ModernTheme.TrackFill"/>), white thumb with accent ring.
/// Replaces the system TrackBar, which cannot be themed for dark mode.
/// </summary>
internal sealed class SoftTrackBar : Control
{
    private const int TrackHeight = 6;
    private const int ThumbDiameter = 18;
    private const int SidePad = 14;

    private int minimum;
    private int maximum = 100;
    private int sliderValue = 80;
    private bool hovering;
    private bool dragging;

    public SoftTrackBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.Selectable |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        TabStop = true;
        Size = new Size(224, 28);
        Cursor = Cursors.Hand;
    }

    public event EventHandler? ValueChanged;

    public int Minimum
    {
        get => minimum;
        set
        {
            minimum = value;
            if (maximum < minimum)
            {
                maximum = minimum;
            }

            Value = sliderValue;
            Invalidate();
        }
    }

    public int Maximum
    {
        get => maximum;
        set
        {
            maximum = value;
            if (minimum > maximum)
            {
                minimum = maximum;
            }

            Value = sliderValue;
            Invalidate();
        }
    }

    public int Value
    {
        get => sliderValue;
        set
        {
            int clamped = Math.Clamp(value, minimum, maximum);
            if (clamped == sliderValue)
            {
                return;
            }

            sliderValue = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int TickFrequency { get; set; } = 10;

    public float FillRatio => maximum == minimum ? 0f : (sliderValue - minimum) / (float)(maximum - minimum);

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int trackY = (Height - TrackHeight) / 2;
        int trackLeft = SidePad;
        int trackWidth = Math.Max(1, Width - SidePad * 2);
        int thumbX = trackLeft + (int)(FillRatio * trackWidth);
        int thumbR = ThumbDiameter / 2;
        int thumbY = Height / 2;

        // Track.
        Rectangle backRect = new(trackLeft, trackY, trackWidth, TrackHeight);
        using (GraphicsPath backPath = ModernTheme.CreateRoundedRectangle(backRect, TrackHeight / 2))
        using (SolidBrush backBrush = new(ModernTheme.TrackBack))
        {
            e.Graphics.FillPath(backBrush, backPath);
        }

        // Filled portion.
        int fillWidth = Math.Max(1, thumbX - trackLeft);
        Rectangle fillRect = new(trackLeft, trackY, fillWidth, TrackHeight);
        using (GraphicsPath fillPath = ModernTheme.CreateRoundedRectangle(fillRect, TrackHeight / 2))
        using (SolidBrush fillBrush = new(ModernTheme.TrackFill))
        {
            e.Graphics.FillPath(fillBrush, fillPath);
        }

        // Hover glow.
        if (hovering || dragging || Focused)
        {
            using SolidBrush glow = new(Color.FromArgb(48, ModernTheme.Accent));
            e.Graphics.FillEllipse(glow, thumbX - thumbR - 4, thumbY - thumbR - 4, ThumbDiameter + 8, ThumbDiameter + 8);
        }

        // Shadow + thumb.
        using (SolidBrush shadow = new(Color.FromArgb(36, Color.Black)))
        {
            e.Graphics.FillEllipse(shadow, thumbX - thumbR, thumbY - thumbR + 2, ThumbDiameter, ThumbDiameter);
        }

        using (SolidBrush thumbBrush = new(Color.White))
        {
            e.Graphics.FillEllipse(thumbBrush, thumbX - thumbR, thumbY - thumbR, ThumbDiameter, ThumbDiameter);
        }

        using (Pen ring = new(dragging ? ModernTheme.AccentPressed : ModernTheme.Accent, 2F))
        {
            e.Graphics.DrawEllipse(ring, thumbX - thumbR + 1, thumbY - thumbR + 1, ThumbDiameter - 2, ThumbDiameter - 2);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hovering = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hovering = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            dragging = true;
            Capture = true;
            Focus();
            SetValueFromX(e.X);
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (dragging)
        {
            SetValueFromX(e.X);
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        dragging = false;
        Capture = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        int step = Math.Max(1, TickFrequency);
        switch (e.KeyCode)
        {
            case Keys.Left:
            case Keys.Down:
                Value -= step;
                e.Handled = true;
                break;
            case Keys.Right:
            case Keys.Up:
                Value += step;
                e.Handled = true;
                break;
            case Keys.Home:
                Value = Minimum;
                e.Handled = true;
                break;
            case Keys.End:
                Value = Maximum;
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    private void SetValueFromX(int x)
    {
        int trackWidth = Math.Max(1, Width - SidePad * 2);
        float ratio = Math.Clamp((x - SidePad) / (float)trackWidth, 0f, 1f);
        Value = minimum + (int)Math.Round(ratio * (maximum - minimum));
    }
}
