using System.ComponentModel;
using System.Text.Json;

namespace CatLocker;

internal sealed class CatLockerApplicationContext : ApplicationContext
{
    private readonly NotifyIcon notifyIcon;
    private readonly ContextMenuStrip menu;
    private readonly ToolStripMenuItem lockMenuItem;
    private readonly ToolStripMenuItem startupMenuItem;
    private readonly ToolStripMenuItem keyboardOnlyMenuItem;
    private readonly ToolStripMenuItem keyboardMouseMenuItem;
    private readonly ToolStripMenuItem soundsMenuItem;
    private readonly InputLocker inputLocker = new();
    private readonly HotKeyManager hotKeyManager = new();
    private readonly List<Bitmap> glyphs = new();
    private readonly List<TouchBarrierForm> barriers = new();
    private readonly System.Windows.Forms.Timer uiTimer = new();
    private readonly System.Threading.Timer watchdogTimer;
    private AppSettings settings;
    private UnlockHotkey currentChord;
    private OverlayForm? overlayForm;
    private DateTime lockTimeUtc;
    private long lastUiTickTicks = DateTime.UtcNow.Ticks;
    private volatile bool hookRefreshPending;
    private bool settingsDialogOpen;
    private bool exiting;
    private bool disposed;

    public CatLockerApplicationContext()
    {
        settings = AppSettings.Load();
        settings.StartWithWindows = StartupManager.IsEnabled();
        TrySaveSettings(showError: false);

        currentChord = settings.GetChord();
        ModernTheme.DarkMode = settings.DarkMode;

        lockMenuItem = new ToolStripMenuItem("Lock now", CreateMenuGlyph(ModernTheme.GlyphLock), (_, _) => ToggleLock())
        {
            ShortcutKeyDisplayString = settings.GetHotkeyMenuHint()
        };
        ToolStripMenuItem settingsMenuItem = new("Settings...", CreateMenuGlyph(ModernTheme.GlyphSettings), (_, _) => ShowSettings());
        startupMenuItem = new ToolStripMenuItem("Start with Windows", CreateMenuGlyph(ModernTheme.GlyphStartup), (_, _) => ToggleStartup())
        {
            CheckOnClick = false,
            Checked = settings.StartWithWindows
        };
        soundsMenuItem = new ToolStripMenuItem("Lock sounds", null, (_, _) => ToggleSounds())
        {
            CheckOnClick = false,
            Checked = settings.EnableSounds
        };

        keyboardOnlyMenuItem = new ToolStripMenuItem(LockMode.KeyboardOnly.ToDisplayText(), null, (_, _) => SetLockMode(LockMode.KeyboardOnly));
        keyboardMouseMenuItem = new ToolStripMenuItem(LockMode.KeyboardAndMouse.ToDisplayText(), null, (_, _) => SetLockMode(LockMode.KeyboardAndMouse));
        ToolStripMenuItem modeMenuItem = new("Lock mode", CreateMenuGlyph(ModernTheme.GlyphMode));
        modeMenuItem.DropDownItems.Add(keyboardOnlyMenuItem);
        modeMenuItem.DropDownItems.Add(keyboardMouseMenuItem);

        ToolStripMenuItem exitMenuItem = new("Exit", CreateMenuGlyph(ModernTheme.GlyphExit), (_, _) => ExitApplication());

        menu = new ContextMenuStrip
        {
            BackColor = ModernTheme.MenuBackground,
            ForeColor = ModernTheme.TextPrimary,
            Padding = new Padding(6, 6, 6, 6),
            Renderer = new ModernMenuRenderer(),
            ShowImageMargin = true
        };
        menu.Items.Add(lockMenuItem);
        menu.Items.Add(modeMenuItem);
        menu.Items.Add(settingsMenuItem);
        menu.Items.Add(startupMenuItem);
        menu.Items.Add(soundsMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitMenuItem);

        notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = TrayIconFactory.Get(locked: false),
            Text = "CatLocker - unlocked",
            Visible = true
        };
        notifyIcon.DoubleClick += (_, _) => ToggleLock();

        UpdateModeMenuState();
        UpdateTrayState();

        inputLocker.UnlockRequested += HandleUnlockRequested;

        // Soft-fail: tray + in-hook unlock keep working even if the global hotkey is taken.
        try
        {
            hotKeyManager.ToggleRequested += (_, _) => ToggleLock();
            hotKeyManager.Register(currentChord);
        }
        catch (Win32Exception ex)
        {
            MessageBox.Show(
                $"{currentChord.DisplayText} is already used by another application.\nLock/unlock from the tray icon will still work.\n\n{ex.Message}",
                "CatLocker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        // 1s UI heartbeat: drives auto-lock/unlock timers and hook recovery.
        uiTimer.Interval = 1000;
        uiTimer.Tick += (_, _) => OnUiTick();
        uiTimer.Start();

        // Watchdog on a pool thread: flags the UI as stuck; recovery runs on the UI tick.
        watchdogTimer = new System.Threading.Timer(_ => WatchdogTick(), null, 2000, 2000);
    }

    private Bitmap CreateMenuGlyph(Color color)
    {
        // Soft dot: solid pastel core + subtle darker ring, readable on white at 16px.
        Bitmap bitmap = new(16, 16);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using SolidBrush brush = new(color);
            graphics.FillEllipse(brush, 3, 3, 10, 10);
            using Pen ring = new(Color.FromArgb(40, ModernTheme.TextPrimary), 1F);
            graphics.DrawEllipse(ring, 3.5F, 3.5F, 9F, 9F);
            using SolidBrush highlight = new(Color.FromArgb(90, Color.White));
            graphics.FillEllipse(highlight, 5, 5, 4, 4);
        }

        glyphs.Add(bitmap);
        return bitmap;
    }

    private void ToggleLock()
    {
        if (exiting)
        {
            return;
        }

        if (inputLocker.IsLocked)
        {
            Unlock();
            return;
        }

        Lock();
    }

    private void Lock()
    {
        try
        {
            UnlockHotkey chord = settings.GetChord();
            currentChord = chord;
            inputLocker.Lock(settings.LockMode, chord);
            lockTimeUtc = DateTime.UtcNow;

            if (settings.LockMode == LockMode.KeyboardAndMouse && settings.BlockTouch)
            {
                ShowTouchBarriers();
            }

            overlayForm ??= new OverlayForm(settings);
            overlayForm.ApplySettings(settings);
            // Barriers are TopMost too — keep the banner above them.
            foreach (TouchBarrierForm barrier in barriers)
            {
                barrier.Show();
            }

            overlayForm.Show();
            overlayForm.BringToFront();
            UpdateTrayState();
            if (settings.EnableSounds)
            {
                LockSoundPlayer.PlayLock();
            }
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            HideTouchBarriers();
            inputLocker.Unlock();
            MessageBox.Show(
                $"Could not lock input.\n\n{ex.Message}",
                "CatLocker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            UpdateTrayState();
        }
    }

    private void HandleUnlockRequested(object? sender, EventArgs e)
    {
        // Posted via the UI SynchronizationContext captured at Lock(), so this already
        // runs outside the hook callback and on the UI thread — safe for Unhook + Hide.
        Unlock();
    }

    private void Unlock()
    {
        bool wasLocked = inputLocker.IsLocked;
        inputLocker.Unlock();
        HideTouchBarriers();

        if (overlayForm is not null)
        {
            overlayForm.Hide();
        }

        UpdateTrayState();
        if (wasLocked && settings.EnableSounds)
        {
            LockSoundPlayer.PlayUnlock();
        }
    }

    private void ShowTouchBarriers()
    {
        HideTouchBarriers();
        try
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                TouchBarrierForm barrier = new(screen.Bounds);
                barriers.Add(barrier);
            }
        }
        catch
        {
            HideTouchBarriers();
        }
    }

    private void HideTouchBarriers()
    {
        foreach (TouchBarrierForm barrier in barriers)
        {
            try
            {
                barrier.Hide();
                barrier.Dispose();
            }
            catch
            {
            }
        }

        barriers.Clear();
    }

    private void OnUiTick()
    {
        Interlocked.Exchange(ref lastUiTickTicks, DateTime.UtcNow.Ticks);

        if (exiting)
        {
            return;
        }

        // Hook recovery after the UI thread was stuck (Windows drops LL hooks silently).
        if (hookRefreshPending && inputLocker.IsLocked)
        {
            hookRefreshPending = false;
            try
            {
                inputLocker.RefreshHooks();
                notifyIcon.ShowBalloonTip(
                    3000,
                    "CatLocker",
                    "Input protection was restored after the UI was unresponsive.",
                    ToolTipIcon.Warning);
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                Unlock();
                MessageBox.Show(
                    $"Input hooks were lost and could not be restored.\nCatLocker has unlocked to stay honest.\n\n{ex.Message}",
                    "CatLocker",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
        }

        // Auto-lock after idle.
        if (!inputLocker.IsLocked && !settingsDialogOpen && settings.AutoLockMinutes > 0)
        {
            TimeSpan idle = GetIdleTime();
            if (idle >= TimeSpan.FromMinutes(settings.AutoLockMinutes))
            {
                Lock();
                return;
            }
        }

        // Auto-unlock after a fixed locked period.
        if (inputLocker.IsLocked && settings.AutoUnlockMinutes > 0)
        {
            if (DateTime.UtcNow - lockTimeUtc >= TimeSpan.FromMinutes(settings.AutoUnlockMinutes))
            {
                Unlock();
            }
        }
    }

    private void WatchdogTick()
    {
        try
        {
            if (!exiting && inputLocker.IsLocked &&
                (DateTime.UtcNow - new DateTime(Interlocked.Read(ref lastUiTickTicks), DateTimeKind.Utc)).TotalSeconds > 6)
            {
                hookRefreshPending = true;
            }
        }
        catch
        {
        }
    }

    private static TimeSpan GetIdleTime()
    {
        try
        {
            NativeMethods.Lastinputinfo info = new() { CbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.Lastinputinfo>() };
            if (!NativeMethods.GetLastInputInfo(ref info))
            {
                return TimeSpan.Zero;
            }

            uint idleMs = unchecked((uint)Environment.TickCount - info.DwTime);
            return TimeSpan.FromMilliseconds(idleMs);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private void ShowSettings()
    {
        bool wasLocked = inputLocker.IsLocked;

        if (wasLocked)
        {
            Unlock();
        }

        settingsDialogOpen = true;
        try
        {
            using SettingsForm form = new(settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                AppSettings updatedSettings = form.Settings.Clone().Normalize();
                if (ApplyStartupSetting(updatedSettings.StartWithWindows))
                {
                    settings = updatedSettings;
                    settings.StartWithWindows = StartupManager.IsEnabled();
                    TrySaveSettings(showError: true);
                    ModernTheme.DarkMode = settings.DarkMode;
                    currentChord = settings.GetChord();
                    ReregisterHotKey();
                    startupMenuItem.Checked = settings.StartWithWindows;
                    soundsMenuItem.Checked = settings.EnableSounds;
                    lockMenuItem.ShortcutKeyDisplayString = settings.GetHotkeyMenuHint();
                    overlayForm?.ApplySettings(settings);
                    UpdateModeMenuState();
                    UpdateTrayState();
                }
            }
        }
        finally
        {
            settingsDialogOpen = false;
        }

        if (wasLocked && !exiting)
        {
            Lock();
        }
    }

    private void ReregisterHotKey()
    {
        try
        {
            hotKeyManager.Register(currentChord);
        }
        catch (Win32Exception ex)
        {
            MessageBox.Show(
                $"{currentChord.DisplayText} is already used by another application.\nLock/unlock from the tray icon will still work.\n\n{ex.Message}",
                "CatLocker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ToggleStartup()
    {
        bool requestedState = !StartupManager.IsEnabled();
        if (!ApplyStartupSetting(requestedState))
        {
            startupMenuItem.Checked = StartupManager.IsEnabled();
            return;
        }

        settings.StartWithWindows = StartupManager.IsEnabled();
        startupMenuItem.Checked = settings.StartWithWindows;
        TrySaveSettings(showError: true);
    }

    private void ToggleSounds()
    {
        settings.EnableSounds = !settings.EnableSounds;
        soundsMenuItem.Checked = settings.EnableSounds;
        TrySaveSettings(showError: true);
    }

    private void SetLockMode(LockMode mode)
    {
        if (settings.LockMode == mode)
        {
            return;
        }

        bool wasLocked = inputLocker.IsLocked;
        if (wasLocked)
        {
            Unlock();
        }

        settings.LockMode = mode;
        TrySaveSettings(showError: true);
        overlayForm?.ApplySettings(settings);
        UpdateModeMenuState();
        UpdateTrayState();

        if (wasLocked && !exiting)
        {
            Lock();
        }
    }

    private bool ApplyStartupSetting(bool enabled)
    {
        try
        {
            StartupManager.SetEnabled(enabled);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            MessageBox.Show(
                $"Could not update Windows startup setting.\n\n{ex.Message}",
                "CatLocker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }

    private void TrySaveSettings(bool showError)
    {
        try
        {
            settings.Save();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or JsonException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            if (!showError)
            {
                return;
            }

            MessageBox.Show(
                $"Could not save CatLocker settings.\n\n{ex.Message}",
                "CatLocker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void UpdateModeMenuState()
    {
        keyboardOnlyMenuItem.Checked = settings.LockMode == LockMode.KeyboardOnly;
        keyboardMouseMenuItem.Checked = settings.LockMode == LockMode.KeyboardAndMouse;
    }

    private void UpdateTrayState()
    {
        bool locked = inputLocker.IsLocked;
        lockMenuItem.Text = locked ? "Unlock" : "Lock now";
        menu.BackColor = ModernTheme.MenuBackground;
        menu.ForeColor = ModernTheme.TextPrimary;
        notifyIcon.Icon = TrayIconFactory.Get(locked);
        notifyIcon.Text = locked
            ? $"CatLocker - {settings.LockMode.ToTrayText()}"
            : $"CatLocker - unlocked ({settings.LockMode.ToDisplayText()})";
    }

    private void ExitApplication()
    {
        if (exiting)
        {
            return;
        }

        exiting = true;
        try
        {
            uiTimer.Stop();
        }
        catch
        {
        }

        try
        {
            watchdogTimer.Dispose();
        }
        catch
        {
        }

        Unlock();
        HideTouchBarriers();
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        menu.Dispose();
        foreach (Bitmap glyph in glyphs)
        {
            glyph.Dispose();
        }

        glyphs.Clear();
        TrayIconFactory.Dispose();
        overlayForm?.Dispose();
        hotKeyManager.Dispose();
        inputLocker.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            disposed = true;
            inputLocker.UnlockRequested -= HandleUnlockRequested;
        }

        base.Dispose(disposing);
    }
}
