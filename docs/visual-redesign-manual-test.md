# Visual redesign manual test

Issue #5 changes the shell's visual identity only. It must not change navigation behavior,
compatibility permission, host-object exposure, WebView2 security settings, diagnostics, or modal
synchronization.

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
2. Pin or focus the application and confirm the taskbar icon is recognizable at the current display
   scale.
3. Use Alt+Tab and confirm the icon remains recognizable and is not visually confused with
   Microsoft Edge, Internet Explorer, or WebView2.
4. Inspect the built executable and confirm Windows Explorer shows the same application icon.

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

## Display Scale

Check 100%, 150%, and 200% display scale where available.

For each scale:

1. Confirm the toolbar does not overlap at a narrow but usable window width.
2. Confirm icon strokes remain crisp enough to distinguish commands.
3. Confirm the title-bar, taskbar, Alt+Tab, and executable icons remain recognizable.
4. Record OS version, WebView2 Runtime version, display scale, and observed result.
