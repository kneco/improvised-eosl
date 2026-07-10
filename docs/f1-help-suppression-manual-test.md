# F1/help suppression manual test

Issue #16 is intentionally implemented as a bounded browser-shell key handling behavior. It
suppresses F1 only when the current document contains an IE-era `onhelp` suppression handler such
as `<body onhelp="return false">`. It does not emulate IE's `onhelp` DOM event and does not
implement writable IE keyboard event objects such as `event.keyCode = 0`.

## Preconditions

- Run from a normal user PowerShell.
- Use a build that includes the Issue #16 branch.
- Do not use `--no-build` unless the executable was already rebuilt from that branch.

```powershell
dotnet run --configuration Release --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj
```

## Checks

1. Open an ordinary page that does not contain `onhelp="return false"` and press `F1`.
   - Expected: the wrapper does not suppress F1 solely by default.
2. Open the built-in `f1-help-suppression.html` page, which contains
   `onhelp="return false"`.
3. With focus in web content, press `F1`.
   - Expected: no browser/help UI opens.
   - Expected: the app remains responsive.
4. With focus in the address field or another wrapper chrome control while that page is current,
   press `F1`.
   - Expected: no browser/help UI opens.
   - Expected: focus and normal operation are preserved.
5. Navigate away to a page without `onhelp="return false"` and press `F1`.
   - Expected: the previous page's suppression decision is not retained.
6. From `f1-help-suppression.html`, open the same fixture as a child `showModalDialog()` window
   and press `F1` with focus in the child content.
   - Expected: no browser/help UI opens.
   - Expected: modal return/cancel behavior is unchanged.
7. Confirm `Ctrl+F` still opens the WebView2 Find UI.
8. Confirm ordinary navigation, compatibility status, and compatibility permission prompts are
   unchanged.

## Security boundary

F1/help suppression must not:

- disable WebView2 browser accelerator keys globally;
- disable Chromium/WebView2 security or sandbox features;
- expose a new host object or native command to web content;
- inject arbitrary page scripts; or
- attempt general IE event-object mutation such as `event.keyCode = 0`.
