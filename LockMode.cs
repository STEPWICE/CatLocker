namespace CatLocker;

internal enum LockMode
{
    KeyboardOnly = 0,
    KeyboardAndMouse = 1
}

internal static class LockModeExtensions
{
    public static string ToDisplayText(this LockMode mode)
    {
        return mode switch
        {
            LockMode.KeyboardOnly => "Keyboard only",
            _ => "Keyboard + mouse"
        };
    }

    public static string ToOverlayText(this LockMode mode, string hotkeyText)
    {
        return mode switch
        {
            LockMode.KeyboardOnly => $"Keyboard is locked. Press {hotkeyText} to unlock.",
            _ => $"Keyboard and mouse are locked. Press {hotkeyText} to unlock."
        };
    }

    public static string ToTrayText(this LockMode mode)
    {
        return mode switch
        {
            LockMode.KeyboardOnly => "keyboard locked",
            _ => "keyboard and mouse locked"
        };
    }
}
