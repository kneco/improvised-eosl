# Navigation accelerator manual test

Issue #24 requires baseline measurement before production policy parsing or accelerator
suppression is implemented. This checklist uses the local fixture only; it is not evidence that
Back, Forward, or Reload suppression is available.

## Preconditions

- Build or run the WPF spike from a normal user PowerShell.
- Start the fixture directly:

```powershell
dotnet run --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --navigation-accelerator-manual
```

- Alternatively, start normally and open `navigation-accelerator-reference.html` from the built-in
  home page.
- Keep WebView2 security and browser accelerator settings at their defaults.

## Baseline fixture checks

1. Confirm the page title is `navigation accelerator reference fixture`.
2. Confirm the page shows URL, `history.state`, reload count, event count, and an event log.
3. Select `push step 1`, then `push step 2`, and confirm the URL fragment and `history.state`
   change.
4. Select `history.back()` and `history.forward()` and confirm the fixture records `popstate`
   events.
5. Select `location.reload()` and confirm the reload count increments.

## Keyboard matrix

Record each observed outcome separately as browser navigation, page event receipt, text editing,
find UI, or no visible change.

| Key input | Focus target | Expected baseline to record |
| --- | --- | --- |
| `Alt+Left` | document body | Whether WebView2 moves back in history and whether the page records key events. |
| `Alt+Right` | document body | Whether WebView2 moves forward in history and whether the page records key events. |
| Browser Back key | document body | Whether hardware/browser key input is observable on the test machine. |
| Browser Forward key | document body | Whether hardware/browser key input is observable on the test machine. |
| `Ctrl+R` | document body | Whether the document reloads and whether the page records key events before reload. |
| `F5` | document body | Whether the document reloads and whether the page records key events before reload. |
| Backspace | document body | Whether modern WebView2 treats Backspace as history navigation. |
| Backspace | input and textarea | Text should edit; no navigation-suppression claim is allowed from this case. |
| `Ctrl+F` | document body | WebView2 find-in-page should remain available. |
| `F3` | active find session | WebView2 find-next behavior should remain available. |
| `Ctrl+C` / `Ctrl+V` | input and textarea | Editing behavior should remain available. |
| Page Up / Page Down | document body | Page movement should remain available. |

## Recording rules

- Do not record typed field contents.
- Record only key category, focus target category, visible navigation/reload result, page event
  receipt, and find/editing behavior.
- The fixture intentionally ignores ordinary text keys and displays only bounded key categories
  from the Issue #24 matrix.
- If agent-launched behavior differs from a normal user PowerShell run, treat the normal user run
  as authoritative.
- Do not change production shell policy, WebView2 sandboxing, or
  `AreBrowserAcceleratorKeysEnabled` while collecting this baseline.

## Exit criteria

- The baseline matrix has enough evidence to choose a temporary measurement hook:
  direct `CoreWebView2Controller.AcceleratorKeyPressed`, WPF routed events, or unsupported.
- `Ctrl+F` and `F3` behavior remains documented as preserved.
- Any Backspace result is explicitly labeled measured or unsupported rather than assumed.
