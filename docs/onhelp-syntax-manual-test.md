# IE `onhelp` syntax manual test

Issue #16 validates whether the current WebView2 wrapper tolerates IE-era
`<body onhelp="return false">` syntax. This is a validation fixture, not a new F1 suppression
implementation.

## Start

Run from a normal user PowerShell. Do not use `--no-build` unless the executable was already
rebuilt from the current branch.

```powershell
dotnet run --configuration Release --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj
```

## Checks

1. From the home page, open `onhelp return false fixture`.
2. Confirm `Status: loaded`.
3. Confirm `Error: none`.
4. Press `click test` and confirm the status changes to `clicked`.
5. Press `F1`.
   - Expected: the app remains responsive.
   - Expected: no browser help UI opens, matching the current Improvised EOSL baseline.
6. Confirm ordinary navigation, compatibility status, and `Ctrl+F` find still work.

If all checks pass, no implementation is needed for Issue #16 unless a real target page shows a
separate script-visible error.
