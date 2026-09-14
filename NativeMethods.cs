using System.Runtime.InteropServices;

namespace CatLocker;

internal static class NativeMethods
{
    public const int WhKeyboardLl = 13;
    public const int WhMouseLl = 14;
    public const int WmHotkey = 0x0312;
    public const int WmKeydown = 0x0100;
    public const int WmKeyup = 0x0101;
    public const int WmSyskeydown = 0x0104;
    public const int WmSyskeyup = 0x0105;
    public const int WmLbuttondown = 0x0201;
    public const int WmLbuttonup = 0x0202;
    public const int WmRbuttondown = 0x0204;
    public const int WmRbuttonup = 0x0205;
    public const int WmMbuttondown = 0x0207;
    public const int WmMbuttonup = 0x0208;
    public const int WmXbuttondown = 0x020B;
    public const int WmXbuttonup = 0x020C;
    public const int WmMousewheel = 0x020A;
    public const int WmMousehwheel = 0x020E;
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint VkL = 0x4C;
    public const int VkControl = 0x11;
    public const int VkLcontrol = 0xA2;
    public const int VkRcontrol = 0xA3;
    public const int VkShift = 0x10;
    public const int VkLshift = 0xA0;
    public const int VkRshift = 0xA1;
    public const int VkMenu = 0x12;
    public const int VkLmenu = 0xA4;
    public const int VkRmenu = 0xA5;
    public const int VkLwin = 0x5B;
    public const int VkRwin = 0x5C;
    public const int VkApps = 0x5D;
    public const int VkSleep = 0x5F;
    public const int VkEscape = 0x1B;
    public const int VkTab = 0x09;
    public const int VkCapital = 0x14;
    public const int VkSnapshot = 0x2C;
    public const int VkF1 = 0x70;
    public const int VkF24 = 0x87;
    public const uint LlkfInjected = 0x10;
    public const uint LlkfLowerIlInjected = 0x02;

    public delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct Kbdllhookstruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Msllhookstruct
    {
        public Point Pt;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetLastInputInfo(ref Lastinputinfo plii);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential)]
    public struct Lastinputinfo
    {
        public uint CbSize;
        public uint DwTime;
    }
}
