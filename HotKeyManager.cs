using System.ComponentModel;

namespace CatLocker;

/// <summary>
/// Global hotkey for lock/unlock (configurable chord, default Ctrl+Shift+L).
/// While locked, the LL hook detects and swallows the chord itself, so WM_HOTKEY
/// normally does not fire — this manager then acts as a fallback (hook loss, race).
/// </summary>
internal sealed class HotKeyManager : IDisposable
{
    private const int HotKeyId = 0x4C01;
    private readonly HotKeyWindow window = new();
    private bool registered;
    private bool disposed;

    public event EventHandler? ToggleRequested;

    public bool IsRegistered => registered;

    public void Register(UnlockHotkey chord)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        chord ??= UnlockHotkey.Default;

        Unregister();

        bool ok = NativeMethods.RegisterHotKey(window.Handle, HotKeyId, chord.WinModifiers, chord.KeyCode);
        if (!ok)
        {
            throw new Win32Exception($"Could not register {chord.DisplayText} as a global hotkey.");
        }

        registered = true;
    }

    public void Unregister()
    {
        if (registered)
        {
            NativeMethods.UnregisterHotKey(window.Handle, HotKeyId);
            registered = false;
        }
    }

    private void HandleHotKeyPressed(object? sender, EventArgs e)
    {
        ToggleRequested?.Invoke(this, EventArgs.Empty);
    }

    public HotKeyManager()
    {
        window.HotKeyPressed += HandleHotKeyPressed;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Unregister();
        window.HotKeyPressed -= HandleHotKeyPressed;
        window.Dispose();
    }
}
