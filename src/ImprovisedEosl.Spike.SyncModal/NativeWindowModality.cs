using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ImprovisedEosl.Spike.SyncModal;

internal static class NativeWindowModality
{
    public static bool AttachAndDisableOwner(Window child, nint ownerHandle, Action<string> log)
    {
        if (ownerHandle == 0)
        {
            throw new InvalidOperationException(
                "A native owner window is required for synchronous modal behavior.");
        }

        var childInterop = new WindowInteropHelper(child);
        var childHandle = childInterop.EnsureHandle();
        childInterop.Owner = ownerHandle;

        var ownerWasEnabled = IsWindowEnabled(ownerHandle);
        if (ownerWasEnabled)
        {
            EnableWindow(ownerHandle, false);
        }

        var ownerEnabledNow = IsWindowEnabled(ownerHandle);
        if (ownerEnabledNow)
        {
            throw new InvalidOperationException(
                $"Failed to disable native dialog owner 0x{ownerHandle:X}.");
        }

        TryLog(log,
            $"dialog native owner attached: childHwnd=0x{childHandle:X}; " +
            $"ownerHwnd=0x{ownerHandle:X}; ownerWasEnabled={ownerWasEnabled}; " +
            $"ownerEnabledNow={ownerEnabledNow}");
        return ownerWasEnabled;
    }

    public static void RestoreOwner(nint ownerHandle, bool ownerWasEnabled, Action<string> log)
    {
        if (ownerHandle == 0 || !ownerWasEnabled)
        {
            return;
        }

        EnableWindow(ownerHandle, true);
        SetForegroundWindow(ownerHandle);
        TryLog(log,
            $"dialog native owner restored: ownerHwnd=0x{ownerHandle:X}; " +
            $"ownerEnabledNow={IsWindowEnabled(ownerHandle)}");
    }

    private static void TryLog(Action<string> log, string message)
    {
        try
        {
            log(message);
        }
        catch
        {
            // Native owner restoration must not depend on diagnostic I/O.
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(nint windowHandle, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint windowHandle);
}
