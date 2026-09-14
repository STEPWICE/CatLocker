using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CatLocker;

/// <summary>
/// Low-level keyboard/mouse lock based on WH_KEYBOARD_LL / WH_MOUSE_LL.
/// While locked, swallows EVERYTHING (Down and Up, including Win/Apps/Alt/Shift/F1-F24)
/// so no stray keystroke reaches other apps. The configurable unlock chord is detected
/// inside the hook itself and swallowed as well, then the UI is notified asynchronously.
/// RegisterHotKey stays as a fallback for locking and for hook-loss recovery.
/// </summary>
internal sealed class InputLocker : IDisposable
{
    private static readonly IntPtr Swallowed = new(1);

    private readonly NativeMethods.LowLevelProc keyboardProc;
    private readonly NativeMethods.LowLevelProc mouseProc;
    private IntPtr keyboardHook;
    private IntPtr mouseHook;
    private volatile bool isLocked;
    private volatile bool ctrlDown;
    private volatile bool shiftDown;
    private volatile bool altDown;
    private volatile bool mainDown;
    private UnlockHotkey unlockChord = UnlockHotkey.Default;
    private SynchronizationContext? uiContext;
    private bool disposed;

    public InputLocker()
    {
        keyboardProc = KeyboardHookCallback;
        mouseProc = MouseHookCallback;
    }

    public bool IsLocked => isLocked;

    public LockMode CurrentMode { get; private set; } = LockMode.KeyboardAndMouse;

    /// <summary>Raised on the captured UI context when the in-hook unlock chord fires.</summary>
    public event EventHandler? UnlockRequested;

    public void Lock(LockMode mode)
    {
        Lock(mode, UnlockHotkey.Default);
    }

    public void Lock(LockMode mode, UnlockHotkey chord)
    {
        ThrowIfDisposed();

        if (isLocked)
        {
            return;
        }

        CurrentMode = mode;
        unlockChord = chord ?? UnlockHotkey.Default;
        uiContext = SynchronizationContext.Current;
        SyncModifierState();

        InstallHooks();
        isLocked = true;
    }

    /// <summary>
    /// Re-installs both hooks without changing the locked state.
    /// Used by the watchdog after the UI thread was unresponsive
    /// (Windows silently drops LL hooks past LowLevelHooksTimeout).
    /// </summary>
    public void RefreshHooks()
    {
        ThrowIfDisposed();

        if (!isLocked)
        {
            return;
        }

        UnhookAll();
        SyncModifierState();

        try
        {
            InstallHooks();
        }
        catch
        {
            // Stay honest: no hooks means no protection.
            isLocked = false;
            throw;
        }
    }

    public void Unlock()
    {
        if (!isLocked && keyboardHook == IntPtr.Zero && mouseHook == IntPtr.Zero)
        {
            return;
        }

        // Mark unlocked first so any racing hook invocation passes input through
        // instead of swallowing keys while we unhook.
        isLocked = false;
        ctrlDown = false;
        shiftDown = false;
        altDown = false;
        mainDown = false;

        UnhookAll();
    }

    private void InstallHooks()
    {
        IntPtr moduleHandle = GetCurrentModuleHandle();
        keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WhKeyboardLl, keyboardProc, moduleHandle, 0);
        if (keyboardHook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not install the keyboard hook.");
        }

        if (CurrentMode == LockMode.KeyboardAndMouse)
        {
            mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WhMouseLl, mouseProc, moduleHandle, 0);
            if (mouseHook == IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(keyboardHook);
                keyboardHook = IntPtr.Zero;
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not install the mouse hook.");
            }
        }
    }

    private void UnhookAll()
    {
        if (keyboardHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(keyboardHook);
            keyboardHook = IntPtr.Zero;
        }

        if (mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(mouseHook);
            mouseHook = IntPtr.Zero;
        }
    }

    private void SyncModifierState()
    {
        // In case modifiers were already held before Lock()/RefreshHooks().
        ctrlDown = IsKeyPhysicallyDown(NativeMethods.VkControl)
            || IsKeyPhysicallyDown(NativeMethods.VkLcontrol)
            || IsKeyPhysicallyDown(NativeMethods.VkRcontrol);
        shiftDown = IsKeyPhysicallyDown(NativeMethods.VkShift)
            || IsKeyPhysicallyDown(NativeMethods.VkLshift)
            || IsKeyPhysicallyDown(NativeMethods.VkRshift);
        altDown = IsKeyPhysicallyDown(NativeMethods.VkMenu)
            || IsKeyPhysicallyDown(NativeMethods.VkLmenu)
            || IsKeyPhysicallyDown(NativeMethods.VkRmenu);
        mainDown = IsKeyPhysicallyDown((int)unlockChord.KeyCode);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0 || !isLocked)
        {
            return NativeMethods.CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }

        NativeMethods.Kbdllhookstruct info = Marshal.PtrToStructure<NativeMethods.Kbdllhookstruct>(lParam);
        int message = wParam.ToInt32();
        bool isKeyDown = message is NativeMethods.WmKeydown or NativeMethods.WmSyskeydown;
        bool isKeyUp = message is NativeMethods.WmKeyup or NativeMethods.WmSyskeyup;
        uint vk = info.VkCode;

        // Track modifiers (swallowed, never passed through while locked).
        if (vk is NativeMethods.VkControl or NativeMethods.VkLcontrol or NativeMethods.VkRcontrol)
        {
            if (isKeyDown)
            {
                ctrlDown = true;
            }
            else if (isKeyUp)
            {
                ctrlDown = false;
            }

            if (isKeyDown && mainDown && ModifiersSatisfied())
            {
                TriggerUnlockFromHook();
            }

            return Swallowed;
        }

        if (vk is NativeMethods.VkShift or NativeMethods.VkLshift or NativeMethods.VkRshift)
        {
            if (isKeyDown)
            {
                shiftDown = true;
            }
            else if (isKeyUp)
            {
                shiftDown = false;
            }

            if (isKeyDown && mainDown && ModifiersSatisfied())
            {
                TriggerUnlockFromHook();
            }

            return Swallowed;
        }

        if (vk is NativeMethods.VkMenu or NativeMethods.VkLmenu or NativeMethods.VkRmenu)
        {
            if (isKeyDown)
            {
                altDown = true;
            }
            else if (isKeyUp)
            {
                altDown = false;
            }

            if (isKeyDown && mainDown && ModifiersSatisfied())
            {
                TriggerUnlockFromHook();
            }

            return Swallowed;
        }

        if (vk == unlockChord.KeyCode)
        {
            if (isKeyDown)
            {
                mainDown = true;
                if (ModifiersSatisfied())
                {
                    TriggerUnlockFromHook();
                    return Swallowed;
                }
            }
            else if (isKeyUp)
            {
                mainDown = false;
            }

            // Swallow the main key in both directions so the chord never leaks to other apps.
            return Swallowed;
        }

        // Everything else — Win L/R, Apps/Menu, Esc, Tab, F1-F24,
        // letters, digits, media keys — is swallowed on BOTH Down and Up.
        // Blocking the Up events is what stops Start (opens on Win release),
        // Alt+Tab / Alt+F4 completion, and stuck menu behavior.
        return Swallowed;
    }

    private bool ModifiersSatisfied()
    {
        UnlockHotkey chord = unlockChord;
        return (!chord.Ctrl || ctrlDown)
            && (!chord.Shift || shiftDown)
            && (!chord.Alt || altDown);
    }

    private void TriggerUnlockFromHook()
    {
        // Flip the flag synchronously so key-ups arriving right after the chord
        // pass through instead of leaving stuck modifiers.
        // The current Down itself is still swallowed by the caller.
        isLocked = false;
        ctrlDown = false;
        shiftDown = false;
        altDown = false;
        mainDown = false;

        SynchronizationContext? context = uiContext;
        if (context is not null)
        {
            // Post (async): keep the hook callback under the LowLevelHooksTimeout budget.
            context.Post(_ => UnlockRequested?.Invoke(this, EventArgs.Empty), null);
        }
        else
        {
            UnlockRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0 || !isLocked)
        {
            return NativeMethods.CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        // In KeyboardAndMouse mode block everything: moves, downs, ups, wheel/h-wheel,
        // x-buttons. Passing ButtonUp through would let users finish drags started
        // before the lock or dismiss popups by release — so swallow those too.
        // (In KeyboardOnly mode no mouse hook is installed, so the mouse stays live.)
        return Swallowed;
    }

    private static bool IsKeyPhysicallyDown(int vk)
    {
        try
        {
            return (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;
        }
        catch
        {
            return false;
        }
    }

    private static IntPtr GetCurrentModuleHandle()
    {
        // Preferred for LL hooks: handle of the current module.
        IntPtr handle = NativeMethods.GetModuleHandle(null);
        if (handle != IntPtr.Zero)
        {
            return handle;
        }

        try
        {
            using Process process = Process.GetCurrentProcess();
            string? moduleName = null;
            try
            {
                moduleName = process.MainModule?.ModuleName;
            }
            catch
            {
                moduleName = null;
            }

            handle = NativeMethods.GetModuleHandle(moduleName);
            if (handle != IntPtr.Zero)
            {
                return handle;
            }
        }
        catch
        {
            // Fall through to the error below.
        }

        throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resolve the current module handle.");
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(InputLocker));
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Unlock();
        disposed = true;
    }
}
