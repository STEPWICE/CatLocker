using System.Drawing.Drawing2D;

namespace CatLocker;

/// <summary>
/// Single source of truth for CatLocker's soft modern look:
/// light airy surfaces (or calm dark ones), one blue accent, pill shapes, hairline borders.
/// All colors are live getters so flipping <see cref="DarkMode"/> re-themes open UI.
/// </summary>
internal static class ModernTheme
{
    public static bool DarkMode { get; set; }

    private static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);
    private static Color C(int a, int r, int g, int b) => Color.FromArgb(a, r, g, b);

    // Surfaces
    public static Color WindowBackground => DarkMode ? C(28, 34, 45) : C(242, 244, 249);
    public static Color CardBackground => DarkMode ? C(42, 50, 64) : C(255, 255, 255);
    public static Color CardElevated => DarkMode ? C(50, 60, 76) : C(250, 252, 255);
    public static Color OverlayBackground => DarkMode ? C(52, 64, 84) : C(35, 43, 58);
    public static Color OverlayBorder => C(110, 255, 255, 255);

    // Text
    public static Color TextPrimary => DarkMode ? C(232, 237, 245) : C(30, 41, 59);
    public static Color TextSecondary => DarkMode ? C(148, 163, 184) : C(100, 116, 139);
    public static Color TextMuted => DarkMode ? C(100, 116, 139) : C(148, 163, 184);
    public static Color TextOnAccent => C(255, 255, 255);

    // Lines
    public static Color Border => DarkMode ? C(58, 70, 88) : C(222, 228, 240);
    public static Color SoftBorder => DarkMode ? C(48, 58, 76) : C(232, 238, 247);

    // Accent (works on both themes) + states
    public static Color Accent => C(74, 125, 255);
    public static Color AccentHover => C(59, 110, 240);
    public static Color AccentPressed => C(47, 91, 215);
    public static Color AccentSoft => DarkMode ? C(38, 52, 88) : C(227, 235, 255);
    public static Color AccentMuted => C(147, 180, 255);
    public static Color AccentDeep => C(106, 155, 255);

    // Status
    public static Color Success => C(34, 197, 94);
    public static Color SuccessSoft => DarkMode ? C(24, 60, 44) : C(220, 252, 231);
    public static Color Warning => C(245, 158, 11);
    public static Color WarningSoft => DarkMode ? C(70, 54, 24) : C(254, 243, 199);
    public static Color Danger => C(239, 107, 107);
    public static Color DangerSoft => DarkMode ? C(70, 36, 38) : C(254, 226, 226);

    // Tray menu
    public static Color MenuBackground => DarkMode ? C(42, 50, 64) : C(255, 255, 255);
    public static Color MenuSelected => DarkMode ? C(52, 68, 100) : C(234, 240, 255);
    public static Color MenuCheckBackground => DarkMode ? C(52, 68, 100) : C(227, 235, 255);

    // Tray glyph palette — soft but distinguishable at 16px.
    public static Color GlyphLock => C(74, 125, 255);
    public static Color GlyphMode => C(245, 158, 11);
    public static Color GlyphSettings => DarkMode ? C(148, 163, 184) : C(100, 116, 139);
    public static Color GlyphStartup => C(34, 197, 94);
    public static Color GlyphExit => C(239, 107, 107);

    // Radii
    public const int RadiusSmall = 8;
    public const int RadiusMedium = 14;
    public const int RadiusLarge = 18;
    public const int RadiusHero = 20;

    public static GraphicsPath CreateRoundedRectangle(Rectangle rectangle, int radius)
    {
        // Clamp radius so tiny rects (menu items, badges) never produce inverted arcs.
        int maxRadius = Math.Min(rectangle.Width, rectangle.Height) / 2;
        int r = Math.Max(1, Math.Min(radius, Math.Max(1, maxRadius)));
        int diameter = r * 2;
        GraphicsPath path = new();
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
