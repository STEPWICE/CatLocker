namespace CatLocker;

/// <summary>
/// Tray icons for locked/unlocked states. Locked = app icon + soft blue badge dot.
/// Cached for the app lifetime; disposed on exit.
/// </summary>
internal static class TrayIconFactory
{
    private static Icon? unlockedIcon;
    private static Icon? lockedIcon;

    public static Icon Get(bool locked)
    {
        return locked
            ? lockedIcon ??= Build(locked: true)
            : unlockedIcon ??= Build(locked: false);
    }

    private static Icon Build(bool locked)
    {
        Icon baseIcon;
        try
        {
            baseIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
        }
        catch
        {
            baseIcon = (Icon)SystemIcons.Application.Clone();
        }

        using (baseIcon)
        {
            using Bitmap bitmap = new(16, 16);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.DrawIcon(baseIcon, new Rectangle(0, 0, 16, 16));

                if (locked)
                {
                    using SolidBrush brush = new(ModernTheme.Accent);
                    graphics.FillEllipse(brush, 8, 8, 8, 8);
                    using Pen ring = new(Color.White, 1.5F);
                    graphics.DrawEllipse(ring, 8.5F, 8.5F, 7F, 7F);
                }
            }

            IntPtr handle = bitmap.GetHicon();
            try
            {
                using Icon temp = Icon.FromHandle(handle);
                return (Icon)temp.Clone();
            }
            finally
            {
                NativeMethods.DestroyIcon(handle);
            }
        }
    }

    public static void Dispose()
    {
        unlockedIcon?.Dispose();
        unlockedIcon = null;
        lockedIcon?.Dispose();
        lockedIcon = null;
    }
}
