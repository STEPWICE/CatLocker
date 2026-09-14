namespace CatLocker;

internal sealed class HotKeyWindow : NativeWindow, IDisposable
{
    private bool disposed;

    public event EventHandler? HotKeyPressed;

    public HotKeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (Handle != IntPtr.Zero)
        {
            DestroyHandle();
        }
    }
}
