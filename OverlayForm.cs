using System.Drawing.Drawing2D;

namespace CatLocker;

internal sealed class OverlayForm : Form
{
    private readonly Label iconLabel = new();
    private readonly Label messageLabel = new();
    private AppSettings settings;
    private bool disposedManaged;

    public OverlayForm(AppSettings settings)
    {
        this.settings = settings.Clone().Normalize();

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = ModernTheme.OverlayBackground;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        Size = new Size(640, 96);
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        iconLabel.AutoSize = false;
        iconLabel.BackColor = Color.Transparent;
        iconLabel.Dock = DockStyle.Left;
        iconLabel.Width = 76;
        iconLabel.Font = new Font("Segoe UI Emoji", 20F, FontStyle.Regular, GraphicsUnit.Point);
        iconLabel.ForeColor = Color.White;
        iconLabel.Text = "🐱";
        iconLabel.TextAlign = ContentAlignment.MiddleCenter;

        messageLabel.Dock = DockStyle.Fill;
        messageLabel.ForeColor = Color.White;
        messageLabel.BackColor = Color.Transparent;
        messageLabel.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        messageLabel.Padding = new Padding(0, 0, 26, 0);
        messageLabel.TextAlign = ContentAlignment.MiddleLeft;

        Controls.Add(messageLabel);
        Controls.Add(iconLabel);

        ApplySettings(this.settings);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            // WS_EX_NOACTIVATE: never steal focus. WS_EX_TOOLWINDOW: hide from Alt+Tab.
            cp.ExStyle |= 0x08000000 | 0x00000080;
            return cp;
        }
    }

    public void ApplySettings(AppSettings newSettings)
    {
        settings = newSettings.Clone().Normalize();
        Opacity = settings.OverlayOpacity / 100d;
        BackColor = ModernTheme.OverlayBackground;
        messageLabel.Text = settings.LockMode.ToOverlayText(settings.GetHotkeyDisplay());
        UpdateLocation();
        UpdateRegion();
        Invalidate();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateLocation();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using Pen borderPen = new(ModernTheme.OverlayBorder, 1F);
        Rectangle borderRectangle = new(0, 0, Width - 1, Height - 1);
        using GraphicsPath borderPath = ModernTheme.CreateRoundedRectangle(borderRectangle, ModernTheme.RadiusLarge);
        e.Graphics.DrawPath(borderPen, borderPath);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    private void UpdateLocation()
    {
        // Show on the screen with the cursor, not always on the primary screen.
        Screen screen;
        try
        {
            screen = Screen.FromPoint(Cursor.Position);
        }
        catch
        {
            screen = Screen.PrimaryScreen ?? Screen.FromControl(this);
        }

        Rectangle workingArea = screen.WorkingArea;
        int x = workingArea.Left + (workingArea.Width - Width) / 2;
        int y = workingArea.Top + (workingArea.Height - Height) * settings.OverlayVerticalPosition / 100;
        x = Math.Clamp(x, workingArea.Left, Math.Max(workingArea.Left, workingArea.Right - Width));
        y = Math.Clamp(y, workingArea.Top, Math.Max(workingArea.Top, workingArea.Bottom - Height));
        Location = new Point(x, y);
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using GraphicsPath path = ModernTheme.CreateRoundedRectangle(new Rectangle(0, 0, Width, Height), ModernTheme.RadiusLarge);
        Region? old = Region;
        Region = new Region(path);
        old?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposedManaged)
        {
            disposedManaged = true;
            iconLabel.Font.Dispose();
            messageLabel.Font.Dispose();
        }

        base.Dispose(disposing);
    }
}
