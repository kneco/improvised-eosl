# Application settings manual test

## Latest result

Passed on 2026-07-04 from a normal user PowerShell:

- the former `アプリ設定` toolbar control rendered without layout problems before settings UI consolidation
- saving a valid URL did not navigate the current page
- restarting opened the saved URL
- `ホームを使用` restored the built-in home page on restart
- invalid URLs such as `file:` were rejected without closing the settings window
- the unified toolbar exposed one `設定` entry for both the initial URL and compatibility decisions
- the unified window listed and revoked allow/deny decisions
- import/export continued to work after consolidation
- the visible JSON drop target clearly communicated D&D and reacted during drag

Run this checklist from a normal user PowerShell. Agent-launched WebView2 processes are not a
reliable UI validation environment on the current machine.

## Start

```powershell
dotnet run --no-build --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj
```

## Initial URL save and restart

1. Open `設定`.
2. Enter a reachable absolute HTTP(S) URL and choose `保存`.
3. Confirm that the currently displayed page does not navigate.
4. Close and restart the application with the command above.
5. Confirm that the saved URL opens and the diagnostic log identifies `source=user-settings`.
6. Confirm that legacy compatibility is not enabled merely because the URL was saved.

## Validation and cancellation

1. Enter `file:///C:/Windows/win.ini`, a relative URL, and a URL containing user information in
   separate attempts.
2. Confirm that each is rejected while the settings window remains open.
3. Enter a different valid URL, choose `キャンセル`, restart, and confirm that the previous saved
   URL remains active.

## Home fallback

1. Open `設定`, choose `ホームを使用`, and save.
2. Restart and confirm that the built-in home page opens.
3. Close the app and replace
   `%LOCALAPPDATA%\ImprovisedEosl\SyncModalSpike\browser-settings.json` with invalid JSON.
4. Restart and confirm that the built-in home page opens and a bounded settings warning is logged.

## Profile precedence

With a valid configured profile, start the application with `--profile=<id>` and confirm that the
profile `startUrl` opens instead of the saved user initial URL. Do not modify the trusted profile
file through the application-settings UI.

## Automatic isolated startup check

Run from a normal user PowerShell:

```powershell
dotnet run --no-build --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --browser-settings-auto
```

The process must navigate to the isolated persisted URL, report
`browser settings initial URL selected from isolated persisted state`, exit with code 0, and leave
normal `%LOCALAPPDATA%` settings unchanged.

Passed on 2026-07-04 from a normal user PowerShell. The process exited with code 0. The diagnostic
log recorded `source=user-settings`, the exact isolated local target passed the native comparison,
and the temporary settings fixture was removed on exit.

## Portable settings checks

The portable settings implementation requires these additional checks:

1. Export staged settings and confirm that the JSON contains the initial URL and user allow/deny
   decisions, but no configured profiles, cookies, local paths, or WebView2 data.
2. Change the staged values, import the exported file, confirm the summary, and then choose Save.
   Restart and confirm the initial URL and user decisions were restored.
3. Import again but choose Cancel in the application-settings window. Confirm that persisted and
   runtime values did not change.
4. Drop one valid `.json` file onto the visible drop target in the unified settings window and
   confirm that the target changes color and follows the same preview/confirmation path as Import.
5. Confirm that the toolbar has one `設定` entry rather than separate application and compatibility
   settings, and that the same window lists user allow/deny decisions with a revoke action.
6. Drop JSON onto the main browser window and confirm it is not accepted as settings or local HTML.
7. Try malformed JSON, an unknown property such as `profiles`, a duplicate decision, and a decision
   present in both approvals and denials. Confirm each is rejected without staging changes.
8. Confirm that importing user settings never changes `config/compatibility-profiles.json` or a
   configured grant visible at runtime.
