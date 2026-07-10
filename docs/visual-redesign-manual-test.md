# Visual redesign manual test

Issue #5 changes the shell's visual identity only. It must not change navigation behavior,
compatibility permission, host-object exposure, WebView2 security settings, diagnostics, or modal
synchronization.

## Latest partial result

Passed in the current user session on 2026-07-10:

- title-bar icon displayed the brown Improvised EOSL mark instead of the former blue `IE` wordmark;
- command icons used the brown palette on the toolbar;
- the native main-window frame used the pale brown caption tint while preserving the standard
  Windows title bar and caption buttons;
- the diagnostic log recorded successful DWM frame tint application for border, caption, and text;
- Back and Forward rendered as disabled at startup while Reload, address navigation, Settings,
  Diagnostics, and the compatibility status remained enabled;
- Forward and address navigation were visually distinct: Forward used the browser-history arrow,
  while address navigation used the page/enter-style mark;
- the compatibility status retained its visible short text and complete UI Automation name,
  including origin plus enabled, denied, and detected API lists;
- the compatibility detail window opened and displayed the same complete origin/API detail;
- at a 760 pixel window width, the address field, Go, compatibility status, Settings, and
  Diagnostics controls did not overlap; and
- the published executable exposed the brown embedded application icon.
- Debug `--auto` smoke still exited with code `0` after native frame tinting was added;
- Release package publishing and distribution layout validation still passed.

Validation environment:

- OS version string: `Microsoft Windows NT 10.0.26200.0`
- WebView2 Runtime: `150.0.4078.48`
- primary display: `1920x1080`
- observed WPF window DPI: `96` (`100%`)
- Windows app theme: dark
- high contrast skip behavior, taskbar, Alt+Tab, and 150%/200% display-scale checks remain pending.

Run this checklist from a normal user PowerShell. Agent-launched WebView2 processes are not a
reliable UI validation environment on every machine.

## Start

```powershell
dotnet run --no-build --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj
```

Use the built-in home page and at least one ordinary HTTP(S) site.

## Application Identity

1. Confirm the title-bar icon uses the brown Improvised EOSL mark, not the former blue `IE`
   wordmark.
2. Confirm the native window frame uses the pale brown caption tint on supported Windows versions.
   Standard caption buttons, resize behavior, Snap, and the system menu must remain OS-owned.
3. Pin or focus the application and confirm the taskbar icon is recognizable at the current display
   scale.
4. Use Alt+Tab and confirm the icon remains recognizable and is not visually confused with
   Microsoft Edge, Internet Explorer, or WebView2.
5. Inspect the built executable and confirm Windows Explorer shows the same application icon.

## Browser Commands

1. Confirm Back, Forward, Reload, address navigation, Settings, Diagnostics, and compatibility
   status icons use the brown command palette.
2. Confirm Forward and address navigation are visually distinct. Forward should read as browser
   history movement; address navigation should read as opening the typed address.
3. Confirm disabled Back/Forward states remain visibly disabled without relying only on color.
4. Confirm command tooltips, keyboard focus, click targets, and UI Automation names are unchanged.

## Status And Accessibility

1. Confirm the compatibility status text remains visible next to its icon.
2. Confirm status meaning remains available through icon shape and short text, not color alone.
3. Open the compatibility detail window and confirm the complete origin/API detail is unchanged.
4. Check Windows high contrast and light/dark themes. The UI may keep its product palette, but text,
   icon strokes, focus indication, and hover states must remain readable.
5. In Windows high contrast, confirm native frame tinting is skipped or otherwise remains readable
   through the OS-provided high-contrast frame.

## Display Scale

Check 100%, 150%, and 200% display scale where available.

For each scale:

1. Confirm the toolbar does not overlap at a narrow but usable window width.
2. Confirm icon strokes remain crisp enough to distinguish commands.
3. Confirm the title-bar, taskbar, Alt+Tab, and executable icons remain recognizable.
4. Record OS version, WebView2 Runtime version, display scale, and observed result.
