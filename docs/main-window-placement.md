# Main window placement persistence

## Contract

Normal startup restores the main shell window's last normal bounds and maximized state from:

```text
%LOCALAPPDATA%\ImprovisedEosl\SyncModalSpike\main-window-placement.json
```

The setting is not used or changed by automatic WebView2 validation modes.

The persisted rectangle is WPF's `RestoreBounds`, not the current minimized or maximized
rectangle. Closing while minimized reopens in the state that preceded minimization: normal
after normal, maximized after maximized. The application never deliberately starts minimized.

## Boundary behavior

- Missing, corrupt, oversized, unsupported-version, non-finite, undersized, or implausibly
  large values are ignored and logged.
- A rectangle with less than `64x64` device-independent pixels on the current virtual desktop
  is treated as belonging to a removed display and is ignored.
- Ignored placement retains the XAML `1100x760` size and lets Windows choose the position.
- Maximized placement first applies the saved normal rectangle, then sets the maximized state,
  preserving a useful Restore target.
- Snap layout is not a WPF `WindowState`; a snapped window reopens normally using its restore
  rectangle.
- Values are device-independent pixels. Exact mixed-DPI placement after changing display scale
  is not yet claimed.
- WebView2 is hidden while the shell is minimized, following Microsoft's controller visibility
  guidance. A taskbar close saves the intended next-launch state, keeps the shell visually
  hidden, restores the native window to normal, and then disposes WebView2. This avoids leaving
  the host process blocked in minimized-window teardown.

## Manual matrix

1. Close a moved/resized normal window and confirm the next launch restores it.
2. Close maximized and confirm the next launch is maximized and Restore returns to prior bounds.
3. Minimize from normal, close from the taskbar, and confirm the next launch is normal.
4. Minimize from maximized, close from the taskbar, and confirm the next launch is maximized.
5. Save on a secondary monitor, disconnect it, and confirm the window uses default placement.
6. Repeat across display-scale changes and record any WPF device-independent-unit drift.

## Validation result

Manual validation on 2026-07-01 passed cases 1 through 5. The initial minimized taskbar-close
run exposed a host-process hang after placement was saved. Hiding WebView2 while minimized and
restoring the hidden native window before disposal fixed that path; both minimized-from-normal
and minimized-from-maximized shutdowns then returned control to PowerShell and restored the
intended next-launch state. Display-scale changes in case 6 remain unmeasured.

This follows the documented Windows/WPF restored-window model. Microsoft Edge does not expose
a stable public application-level placement-file contract for this wrapper to consume.
