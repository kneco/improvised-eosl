using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ImprovisedEosl.Spike.SyncModal;

public static class NativeWindowVisuals
{
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    private static readonly int CaptionColor = ToColorRef(245, 236, 227);
    private static readonly int BorderColor = ToColorRef(203, 183, 168);
    private static readonly int TextColor = ToColorRef(90, 51, 37);

    public static int ToColorRef(byte red, byte green, byte blue) =>
        red | (green << 8) | (blue << 16);

    public static void UseBrownFrame(Window window, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
        {
            TryApplyBrownFrame(window, log);
            return;
        }

        window.SourceInitialized += (_, _) => TryApplyBrownFrame(window, log);
    }

    public static bool TryApplyBrownFrame(Window window, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (SystemParameters.HighContrast)
        {
            log?.Invoke("native frame tint skipped: high contrast is enabled");
            return false;
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            log?.Invoke("native frame tint skipped: DWM caption colors require Windows 11 or later");
            return false;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            log?.Invoke("native frame tint skipped: window handle is unavailable");
            return false;
        }

        try
        {
            var borderSucceeded = SetWindowAttribute(handle, DwmwaBorderColor, BorderColor);
            var captionSucceeded = SetWindowAttribute(handle, DwmwaCaptionColor, CaptionColor);
            var textSucceeded = SetWindowAttribute(handle, DwmwaTextColor, TextColor);
            var succeeded = borderSucceeded && captionSucceeded && textSucceeded;
            log?.Invoke(
                "native frame tint applied: " +
                $"border={borderSucceeded}; caption={captionSucceeded}; text={textSucceeded}");
            return succeeded;
        }
        catch (DllNotFoundException ex)
        {
            log?.Invoke("native frame tint skipped: dwmapi unavailable: " + ex.GetType().Name);
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            log?.Invoke("native frame tint skipped: DwmSetWindowAttribute unavailable: " + ex.GetType().Name);
            return false;
        }
    }

    private static bool SetWindowAttribute(IntPtr handle, int attribute, int color)
    {
        var value = color;
        var result = DwmSetWindowAttribute(handle, attribute, ref value, Marshal.SizeOf<int>());
        return result >= 0;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);
}
