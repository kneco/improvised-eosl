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

## Manual baseline result: 2026-07-11

Environment:

- Fixture started from a normal user PowerShell with `--navigation-accelerator-manual`.
- Fixture URL reached `http://127.0.0.1:18080/navigation-accelerator-reference.html#step-2`.
- Final visible fixture state after the pasted observation showed `history.state` as `{"step":2}`,
  reload count `3`, and event count `26`.

Recorded observations:

| Case | Observation |
| --- | --- |
| Page load and fixture state | Fixture loaded and displayed URL, history state, reload count, event count, controls, editable targets, and event log. |
| History stack setup | `push step 1` and `push step 2` reached `#step-2` with `history.state` set to `{"step":2}`. |
| `Alt+Left` | Tester confirmed this moved backward in the prepared history stack. |
| `Alt+Right` | Tester confirmed this moved forward in the prepared history stack. |
| `Ctrl+F` outside editable controls | Tester reported this as the only outside-editable key case with visible expected behavior. Find-in-page preservation remains a required constraint. |
| `F3` | Tester confirmed F3 worked after understanding the active find-session requirement. |
| Backspace outside editable controls | Tester reported Backspace did not trigger history-back navigation in the tested flow. |
| Backspace in input | Multiple capture and bubble `keydown` / `keyup` entries were recorded with target `input`; text editing occurred. |
| `Ctrl+C` and `Ctrl+V` in input | Capture and bubble entries were recorded with target `input`; tester reported editable-field shortcuts worked. |
| `Ctrl+C` outside editable controls | Capture and bubble `keydown` entries were recorded with target `document`; no typed content was logged. |
| `location.reload()` fixture control | Tester corrected the initial summary and confirmed the reload control worked. |
| `F5` | Tester corrected the initial summary and confirmed F5 reload worked. A post-reload visible state included reload count `3`, and the log included `F5` `keyup` entries after `pageshow`. |
| `Ctrl+R` | Tester confirmed Ctrl+R triggered reload. |
| Browser Back / Forward hardware keys | Tester's keyboard has no dedicated Back / Forward hardware keys. Current scope does not require hardware-key coverage unless target deployment hardware introduces it. |
| Editable controls overall | Tester reported that all tested editable-field cases worked. |

Remaining design gate:

- The normal-user baseline is sufficient to describe current Back, Forward, Reload, Find, and
  editable-field behavior for the tested keyboard.
- Production policy still must not be implemented until a temporary hook compares direct
  `AcceleratorKeyPressed` behavior against the WPF routed-event path and selects
  `IsBrowserAcceleratorKeyEnabled`, `Handled`, or unsupported behavior.
