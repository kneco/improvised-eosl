using System.Runtime.InteropServices;

namespace ImprovisedEosl.Spike.SyncModal;

internal static class NativeTestInput
{
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;

    public static void ClickWindowCenter(nint windowHandle, Action<string> log)
    {
        if (!GetWindowRect(windowHandle, out var bounds) || !GetCursorPos(out var originalPosition))
        {
            throw new InvalidOperationException("Could not read native window or cursor coordinates.");
        }

        var x = bounds.Left + ((bounds.Right - bounds.Left) / 2);
        var y = bounds.Top + ((bounds.Bottom - bounds.Top) / 2);
        SetForegroundWindow(windowHandle);
        if (!SetCursorPos(x, y))
        {
            throw new InvalidOperationException("Could not position the cursor for unresponsive-renderer validation.");
        }

        mouse_event(MouseEventLeftDown, 0, 0, 0, 0);
        mouse_event(MouseEventLeftUp, 0, 0, 0, 0);
        SetCursorPos(originalPosition.X, originalPosition.Y);
        log($"native test click sent to child window center: hwnd=0x{windowHandle:X}; point={x},{y}");
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint windowHandle, out Rect bounds);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint windowHandle);

    [DllImport("user32.dll")]
    private static extern void mouse_event(
        uint flags,
        uint dx,
        uint dy,
        uint data,
        nuint extraInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }
}
