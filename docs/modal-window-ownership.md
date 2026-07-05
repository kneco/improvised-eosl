# Modal window ownership

## MVP contract

While a synchronous `showModalDialog` child is open:

- the child has the calling WPF window's HWND as its native owner
- the calling window is disabled and cannot receive mouse or keyboard input
- other unrelated Windows applications remain usable
- closing, timing out, failing, or crashing the child restores the calling window's prior enabled state
- nested dialogs disable only their immediate owner and restore the chain from the inside out

This is application-modal behavior. The wrapper does not attempt to block Alt+Tab or make the dialog system-modal across unrelated applications.

## Implementation

The parent HWND is captured before the child STA starts. The child STA creates its HWND, assigns the native owner through `WindowInteropHelper`, and disables the owner with `EnableWindow` before showing the child.

The previous enabled state is retained. `Closed` restores the owner, and the STA runner repeats restoration in `finally` as protection against initialization or dispatcher failures. Restoration is idempotent. If a valid owner cannot be attached and disabled, dialog creation fails instead of silently continuing as a non-modal window.

On timeout, the host requests child close and waits up to five additional seconds for close and owner restoration before returning to the blocked JavaScript caller. The timeout result is therefore observed after normal cleanup in the successful cleanup path.

Nested dialogs pass the current child HWND as the next owner. This avoids cross-thread WPF `Window.Owner` access while retaining the native owner chain.

## Validation

Before this implementation, the parent window could be brought to the foreground while the child was open, even though the blocked parent UI thread could not process commands.

The 2026-06-28 validation recorded:

- top-level owner: `ownerWasEnabled=True; ownerEnabledNow=False`
- close/timeout restoration: `ownerEnabledNow=True`
- timeout ordering: owner restoration and child close completed before `ShowDialog` returned the timeout result
- four-level nesting: every immediate owner was disabled on entry and restored from the innermost dialog outward
- all automatic modes relevant to owner enforcement pass; the current suite exposes fifteen total WebView2 modes

Windows UI automation may report that an activation request was issued for a disabled parent. The authoritative state for this check is the native `IsWindowEnabled` result recorded by the host.

## Remaining checks

- Mixed-DPI and multi-monitor focus restoration remains unmeasured.
- Minimize/restore and taskbar grouping deserve a later shell-level usability pass.
- `SetForegroundWindow` is best effort under Windows foreground activation rules; owner enablement is the required correctness condition.
