using System.Drawing.Drawing2D;

namespace CatLocker;

/// <summary>
/// Single source of truth for CatLocker's soft modern look: indigo-violet accent,
/// airy light surfaces, calm dark surfaces, pill shapes, hairline borders.
/// All colors are live getters so flipping <see cref="DarkMode"/> re-themes open UI.
/// Text-on-fill pairs are picked for ≥ 4.5:1 contrast (body) / ≥ 3:1 (large).
/// </summary>
internal static class ModernTheme
{
    public static bool DarkMode { get; set; }

    private static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);
    private static Color C(int a, int r, int g, int b) => Color.FromArgb(a, r, g, b);

    // Surfaces
    public static Color WindowBackground => DarkMode ? C(27, 34, 46) : C(241, 244, 249);
    public static Color CardBackground => DarkMode ? C(39, 47, 62) : C(255, 255, 255);
    public static Color CardElevated => DarkMode ? C(47, 56, 73) : C(248, 250, 254);
    public static Color OverlayBackground => DarkMode ? C(52, 56, 92) : C(36, 38, 66);
    public static Color OverlayBorder => C(120, 255, 255, 255);

    // Text
    public static Color TextPrimary => DarkMode ? C(232, 238, 246) : C(27, 39, 64);
    public static Color TextSecondary => DarkMode ? C(166, 178, 198) : C(91, 107, 133);
    public static Color TextMuted => DarkMode ? C(109, 123, 146) : C(138, 151, 173);
    public static Color TextOnAccent => C(255, 255, 255);

    // Lines
    public static Color Border => DarkMode ? C(58, 68, 87) : C(223, 229, 240);
    public static Color SoftBorder => DarkMode ? C(44, 53, 70) : C(231, 236, 244);

    // Accent: indigo-violet. Fills keep white text readable in both themes.
    public static Color Accent => DarkMode ? C(142, 148, 249) : C(91, 80, 230);
    public static Color AccentHover => DarkMode ? C(124, 130, 245) : C(76, 66, 216);
    public static Color AccentPressed => DarkMode ? C(108, 114, 238) : C(63, 55, 184);
    public static Color AccentSoft => DarkMode ? C(45, 47, 87) : C(230, 230, 250);
    public static Color AccentMuted => C(165, 166, 245);
    public static Color AccentDeep => C(139, 92, 246);

    /// <summary>Accent as text on light surfaces (badges, value labels). High contrast.</summary>
    public static Color AccentStrong => DarkMode ? C(168, 176, 255) : C(67, 56, 202);

    // Hero is always the indigo gradient, both themes.
    public static Color HeroStart => C(91, 80, 230);
    public static Color HeroEnd => C(139, 92, 246);
    public static Color BadgeBackground => C(235, 242, 241, 254);
    public static Color BadgeText => C(67, 56, 202);

    // Form fields (combos, numerics)
    public static Color FieldBackground => DarkMode ? C(34, 42, 57) : C(255, 255, 255);
    public static Color FieldBorder => DarkMode ? C(72, 85, 108) : C(211, 219, 232);

    // Slider
    public static Color TrackBack => DarkMode ? C(58, 68, 87) : C(226, 230, 240);
    public static Color TrackFill => Accent;

    // Secondary button (Cancel)
    public static Color ButtonSecondary => DarkMode ? C(48, 58, 77) : C(255, 255, 255);
    public static Color ButtonSecondaryHover => DarkMode ? C(58, 69, 91) : C(238, 241, 248);
    public static Color ButtonSecondaryPressed => DarkMode ? C(66, 78, 102) : C(226, 231, 242);

    // Preview zone backdrop (contrasts with the dark overlay pill in both themes)
    public static Color PreviewBackdrop => DarkMode ? C(21, 27, 38) : C(231, 236, 244);

    // Status
    public static Color Success => C(34, 197, 94);
    public static Color SuccessSoft => DarkMode ? C(28, 70, 54) : C(220, 252, 231);
    public static Color Warning => C(245, 158, 11);
    public static Color WarningSoft => DarkMode ? C(74, 58, 28) : C(254, 243, 199);
    public static Color Danger => DarkMode ? C(248, 130, 130) : C(220, 80, 80);
    public static Color DangerSoft => DarkMode ? C(78, 42, 46) : C(254, 226, 226);

    // Tray menu
    public static Color MenuBackground => DarkMode ? C(39, 47, 62) : C(255, 255, 255);
    public static Color MenuSelected => DarkMode ? C(52, 52, 94) : C(236, 235, 253);
    public static Color MenuCheckBackground => DarkMode ? C(52, 52, 94) : C(226, 224, 250);

    // Tray glyph palette — soft but distinguishable at 16px.
    public static Color GlyphLock => C(91, 80, 230);
    public static Color GlyphMode => C(245, 158, 11);
    public static Color GlyphSettings => DarkMode ? C(166, 178, 198) : C(100, 116, 139);
    public static Color GlyphStartup => C(34, 197, 94);
    public static Color GlyphExit => DarkMode ? C(248, 130, 130) : C(220, 80, 80);

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
