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

## WPF routed suppression measurement

Use this mode to test the smallest WPF routed-event suppression path. It is a temporary manual
measurement hook, not production policy parsing.

```powershell
dotnet run --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --navigation-accelerator-wpf-suppress-manual --show-diagnostics
```

Expected measurement behavior:

- The app opens `navigation-accelerator-reference.html` directly.
- `Alt+Left`, `Alt+Right`, `Ctrl+R`, `F5`, Browser Back, and Browser Forward are handled by the
  host through WPF `PreviewKeyDown` if the WPF WebView2 wrapper forwards those accelerators.
- The diagnostic log records only bounded command categories: `history-back`, `history-forward`,
  or `reload`.
- `Ctrl+F` and `F3` must remain find-in-page behavior.
- Backspace remains outside the suppression target because the baseline did not show
  history-back navigation in the tested flow.

Record whether each targeted key still changes the fixture URL or reload count. Also record
whether the page receives JavaScript key events. WPF suppression is expected to behave like host
`Handled=true`, not like `IsBrowserAcceleratorKeyEnabled=false`, so page receipt may differ from a
future direct controller-event implementation.

## Production shell-policy suppression check

After creating a policy with `keyboard-history-command-disabled:true` and
`keyboard-reload-command-disabled:true`, run the same fixture through production policy loading:

```powershell
dotnet run --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --navigation-accelerator-manual --shell-policy <path-to-policy> --show-diagnostics
```

Confirm `Alt+Left`, `Alt+Right`, `Ctrl+R`, and `F5` no longer change history or reload the page,
while `Ctrl+F`, `F3`, editable-field Backspace, and editable-field copy/paste keep their baseline
behavior. The diagnostics should log `navigation accelerator WPF suppression` with bounded command
categories only.

## Production shell-policy suppression result: 2026-07-11

Environment:

- Fixture started from the WPF shell with `--navigation-accelerator-manual`.
- Policy supplied through `--shell-policy` from a temporary JSON file.
- Diagnostics were visible with `--show-diagnostics`.
- WebView2 runtime reported `150.0.4078.65` on Windows `10.0.26200.0`.

Policy:

- `keyboard-history-command-disabled:true`
- `keyboard-reload-command-disabled:true`
- toolbar keys remained false, so this run measured keyboard suppression only.

Recorded observations:

| Case | Observation |
| --- | --- |
| Policy load | Diagnostics recorded `source=explicit`, the temporary policy path, `keyboardHistoryCommandDisabled=True`, and `keyboardReloadCommandDisabled=True`. |
| `Alt+Left` | Diagnostics recorded `navigation accelerator WPF suppression: source=policy; command=history-back; key=Alt+Left; handled=true`. Tester reported the case passed. |
| `Alt+Right` | Diagnostics recorded `navigation accelerator WPF suppression: source=policy; command=history-forward; key=Alt+Right; handled=true`. Tester reported the case passed. |
| `Ctrl+R` | Diagnostics recorded `navigation accelerator WPF suppression: source=policy; command=reload; key=Control+R; handled=true`. Tester reported the case passed. |
| `F5` | Diagnostics recorded `navigation accelerator WPF suppression: source=policy; command=reload; key=F5; handled=true`. Tester reported the case passed. |
| `Ctrl+F` | Diagnostics recorded `opened WebView2 find-in-page UI`; tester reported the overall matrix passed. |

Result:

- The production `--shell-policy` path successfully reached WPF routed-event suppression for the
  tested Back, Forward, and Reload accelerators.
- `Ctrl+F` find-in-page remained available after policy suppression.
- This evidence covers the tested keyboard path. Dedicated Browser Back / Forward hardware keys
  remain untested on this machine because the tester's keyboard does not provide them.
