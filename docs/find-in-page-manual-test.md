# Find-in-page manual test

Issue #11 is intentionally implemented by using WebView2's built-in Find UI rather than a custom
DOM search layer. This keeps search behavior aligned with the embedded browser and avoids adding a
new script injection or compatibility permission boundary.

## Preconditions

- Run from a normal user PowerShell.
- Use a build that includes the Issue #11 branch.
- Do not disable WebView2 browser accelerator keys.

```powershell
dotnet run --configuration Release --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj
```

## Checks

1. Open the built-in home or parent test page.
2. Click inside the web content and press `Ctrl+F`.
   - Expected: WebView2's built-in Find UI opens.
   - Expected: typing a term highlights matches and the browser UI can move between matches.
3. Click the address field and press `Ctrl+F`.
   - Expected: the same WebView2 Find UI opens instead of editing the address field.
4. Navigate to an ordinary HTTP(S) page and repeat the two `Ctrl+F` checks.
5. Confirm that compatibility permission prompts, the compatibility status indicator, and
   `showModalDialog()` behavior are unchanged.

## Security boundary

Find-in-page is a browser shell feature. It must not:

- grant or deny compatibility APIs;
- inject custom search scripts into page content;
- expose a new host object or native command to web content; or
- weaken WebView2 browser security or sandbox settings.
