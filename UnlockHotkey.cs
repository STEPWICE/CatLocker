namespace CatLocker;

/// <summary>
/// Configurable unlock chord: 1+ modifiers (Ctrl/Shift/Alt) + a main key (A-Z, 0-9, F1-F24).
/// Persisted as "Ctrl+Shift+L". Win/Apps keys are intentionally not allowed: they are
/// system-owned and unreliable as chord members.
/// </summary>
internal sealed record UnlockHotkey(bool Ctrl, bool Shift, bool Alt, uint KeyCode)
{
    public static readonly UnlockHotkey Default = new(true, true, false, NativeMethods.VkL);

    public uint WinModifiers =>
        (Ctrl ? NativeMethods.ModControl : 0) |
        (Shift ? NativeMethods.ModShift : 0) |
        (Alt ? NativeMethods.ModAlt : 0);

    public string DisplayText => string.Join(" + ", Parts());

    public string PersistText => string.Join("+", Parts());

    private IEnumerable<string> Parts()
    {
        if (Ctrl)
        {
            yield return "Ctrl";
        }

        if (Shift)
        {
            yield return "Shift";
        }

        if (Alt)
        {
            yield return "Alt";
        }

        yield return KeyName(KeyCode);
    }

    public static string KeyName(uint vk)
    {
        if (vk is >= 0x41 and <= 0x5A or >= 0x30 and <= 0x39)
        {
            return ((char)vk).ToString();
        }

        if (vk is >= 0x70 and <= 0x87)
        {
            return $"F{vk - 0x70 + 1}";
        }

        return $"VK 0x{vk:X2}";
    }

    public static bool TryParse(string? text, out UnlockHotkey? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        bool ctrl = false;
        bool shift = false;
        bool alt = false;
        string? keyToken = null;

        foreach (string raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries))
        {
            string token = raw.Trim().ToUpperInvariant();
            switch (token)
            {
                case "CTRL":
                case "CONTROL":
                    ctrl = true;
                    break;
                case "SHIFT":
                    shift = true;
                    break;
                case "ALT":
                    alt = true;
                    break;
                default:
                    if (keyToken is not null)
                    {
                        return false;
                    }

                    keyToken = token;
                    break;
            }
        }

        if (keyToken is null || (!ctrl && !shift && !alt))
        {
            return false;
        }

        if (!TryParseKey(keyToken, out uint vk))
        {
            return false;
        }

        result = new UnlockHotkey(ctrl, shift, alt, vk);
        return true;
    }

    private static bool TryParseKey(string token, out uint vk)
    {
        vk = 0;
        if (token.Length == 1 && char.IsLetterOrDigit(token[0]))
        {
            vk = (uint)char.ToUpperInvariant(token[0]);
            return true;
        }

        if (token.StartsWith("F", StringComparison.Ordinal) &&
            int.TryParse(token[1..], out int f) && f >= 1 && f <= 24)
        {
            vk = 0x70u + (uint)(f - 1);
            return true;
        }

        return false;
    }
}
