using System.Text.Json;

namespace CatLocker;

internal sealed class AppSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CatLocker");

    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

    public int OverlayOpacity { get; set; } = 82;

    public int OverlayVerticalPosition { get; set; } = 50;

    public LockMode LockMode { get; set; } = LockMode.KeyboardAndMouse;

    public bool StartWithWindows { get; set; }

    public bool EnableSounds { get; set; } = true;

    /// <summary>Unlock chord persisted as "Ctrl+Shift+L".</summary>
    public string UnlockHotkey { get; set; } = CatLocker.UnlockHotkey.Default.PersistText;

    /// <summary>Auto-lock after N minutes idle. 0 = off.</summary>
    public int AutoLockMinutes { get; set; }

    /// <summary>Auto-unlock N minutes after locking. 0 = off.</summary>
    public int AutoUnlockMinutes { get; set; }

    public bool DarkMode { get; set; }

    /// <summary>Invisible fullscreen click-eater on all screens (touch/pen barrier) in K+M mode.</summary>
    public bool BlockTouch { get; set; } = true;

    public CatLocker.UnlockHotkey GetChord()
    {
        if (CatLocker.UnlockHotkey.TryParse(UnlockHotkey, out CatLocker.UnlockHotkey? chord) && chord is not null)
        {
            return chord;
        }

        return CatLocker.UnlockHotkey.Default;
    }

    public string GetHotkeyDisplay() => GetChord().DisplayText;

    public string GetHotkeyMenuHint() => GetHotkeyDisplay().Replace(" + ", "+", StringComparison.Ordinal);

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(SettingsPath);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings?.Normalize() ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        // Clone before normalizing so Save() never mutates the caller's instance.
        AppSettings snapshot = Clone().Normalize();
        Directory.CreateDirectory(SettingsDirectory);
        string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public AppSettings Normalize()
    {
        OverlayOpacity = Math.Clamp(OverlayOpacity, 20, 100);
        OverlayVerticalPosition = Math.Clamp(OverlayVerticalPosition, 5, 95);
        if (!Enum.IsDefined(LockMode))
        {
            LockMode = LockMode.KeyboardAndMouse;
        }

        if (!CatLocker.UnlockHotkey.TryParse(UnlockHotkey, out _))
        {
            UnlockHotkey = CatLocker.UnlockHotkey.Default.PersistText;
        }

        AutoLockMinutes = Math.Clamp(AutoLockMinutes, 0, 240);
        AutoUnlockMinutes = Math.Clamp(AutoUnlockMinutes, 0, 240);

        return this;
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            OverlayOpacity = OverlayOpacity,
            OverlayVerticalPosition = OverlayVerticalPosition,
            LockMode = LockMode,
            StartWithWindows = StartWithWindows,
            EnableSounds = EnableSounds,
            UnlockHotkey = UnlockHotkey,
            AutoLockMinutes = AutoLockMinutes,
            AutoUnlockMinutes = AutoUnlockMinutes,
            DarkMode = DarkMode,
            BlockTouch = BlockTouch
        };
    }
}
