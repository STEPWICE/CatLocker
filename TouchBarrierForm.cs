namespace CatLocker;

/// <summary>
/// Invisible fullscreen click-eater, one per screen, shown only in Keyboard+Mouse mode.
/// WH_MOUSE_LL does not see touch/pen (WM_TOUCH/WM_POINTER), so this transparent
/// topmost window acts as a second barrier: taps land on it instead of real apps.
/// Keyboard unlock still works — the LL keyboard hook is focus-independent.
/// Opacity 1% keeps it hit-testable while visually absent.
/// </summary>
internal sealed class TouchBarrierForm : Form
{
    public TouchBarrierForm(Rectangle bounds)
    {
        Bounds = bounds;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ControlBox = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        Opacity = 0.01;
        Cursor = Cursors.No;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            // Never steal focus, never show in Alt+Tab. NOT WS_EX_TRANSPARENT (must eat clicks).
            cp.ExStyle |= 0x08000000 | 0x00000080;
            return cp;
        }
    }

    // Swallow everything: no base interaction, no focus, no drag.
    protected override void OnMouseDown(MouseEventArgs e)
    {
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
    }

    protected override void OnClick(EventArgs e)
    {
    }

    protected override void OnDoubleClick(EventArgs e)
    {
    }
}
