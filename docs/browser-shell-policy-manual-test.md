# Browser shell policy manual test

Issue #3 should be validated from a normal user PowerShell after changes to the JSON-only browser
shell policy. This checklist records the manual gate; individual implementation PRs should also
record their automated validation separately.

## Latest result

Partial browser shell policy validation passed on 2026-07-11.

Passed from a normal user PowerShell:

- Standard mode showed Back, Forward, Reload, editable address entry, Go, Settings, Diagnostics,
  compatibility status, and current-origin display.
- Standard mode allowed ordinary HTTP(S) navigation.
- Standard mode preserved `Ctrl+F` WebView2 find-in-page.
- Restricted mode with `toolbar-primary-toolbar-hidden:true` hid the complete wrapper toolbar:
  Back, Forward, Reload, editable address entry, Go, Settings, Diagnostics, compatibility status,
  and current-origin display.
- Restricted mode kept the native Windows title bar and close button visible.
- Restricted-mode diagnostics recorded the explicit policy source, loaded policy, and toolbar
  restriction values including `toolbarPrimaryToolbarHidden=True`, `toolbarAddressEntryHidden=True`,
  `toolbarHistoryCommandHidden=True`, `toolbarReloadCommandHidden=True`,
  `toolbarGoCommandHidden=True`, `toolbarSettingsCommandHidden=True`, and
  `toolbarDiagnosticsCommandHidden=True`.
- Invalid policy with an unknown `browserShell` property failed safe to the standard visible shell.
  The toolbar was visible, diagnostics logged the unknown property warning, and presentation was
  applied with all shell controls visible.
- Toolbar-hidden-only mode hid Back, Forward, and Reload buttons while keeping address entry, Go,
  Settings, Diagnostics, and compatibility status visible.
- Toolbar-hidden-only mode preserved `Ctrl+F` WebView2 find-in-page.
- `--export-shell-policy <path>` exited with code 0 before WebView2 startup, did not open the app
  UI, and wrote a standard visible-shell policy template.

Passed from an agent-launched PowerShell:

- `--export-shell-policy <path>` exited with code 0 before WebView2 startup and wrote a standard
  visible-shell policy template.
- `--apply-shell-policy <source> --shell-policy <target>` exited with code 0 before WebView2
  startup and atomically replaced the target with a valid restricted policy.
- Repeating apply with an invalid source exited with code 1 and left the previous target file
  unchanged.

Not yet passed as a full manual gate:

- invalid-source apply target-preservation from a normal user PowerShell; the attempted check used
  a missing `$applied` target path, so the exit-code failure was observed but target preservation
  was not measured in that run;
- `--reset-user-settings` runtime behavior against a disposable or explicitly approved user
  profile;
- compatibility consent boundary checks; and
- `F3` find continuation and explicit Alt+Left/Alt+Right hidden-buttons-only navigation behavior.

The agent environment did not run the WebView2 UI portions because prior evidence shows
agent-launched WebView2 behavior is not authoritative when it differs from a normal user
PowerShell. The agent also did not run `--reset-user-settings` because the implementation targets
the real `%LOCALAPPDATA%\ImprovisedEosl\SyncModalSpike` folder and should not be invoked without an
explicit disposable-profile setup or user approval.

## Preconditions

- Use a build that includes the browser shell policy implementation.
- Run from a normal user PowerShell.
- Use a temporary copy of the application or an explicit `--shell-policy` path so the normal
  development profile is not changed.

## Standard mode

1. Start without `config/browser-shell-policy.json` and without `--shell-policy`.
2. Confirm Back, Forward, Reload, editable address entry, Go, Settings, Diagnostics,
   compatibility status, and current origin are visible.
3. Confirm ordinary HTTP(S) navigation works.
4. Confirm `Ctrl+F` still opens WebView2 find-in-page.

## Restricted mode

Use a policy that hides the complete primary toolbar:

```json
{
  "version": 1,
  "browserShell": {
    "toolbar-primary-toolbar-hidden": true,
    "toolbar-address-entry-hidden": true,
    "toolbar-history-command-hidden": true,
    "toolbar-reload-command-hidden": true,
    "toolbar-go-command-hidden": true,
    "toolbar-settings-command-hidden": true,
    "toolbar-diagnostics-command-hidden": true,
    "keyboard-history-command-disabled": false,
    "keyboard-reload-command-disabled": false
  }
}
```

1. Start with `--shell-policy <path-to-policy>`.
2. Confirm Back/Forward/Reload, editable address entry, Go, Settings, Diagnostics, compatibility
   status, and current-origin controls are all hidden.
3. Confirm the native Windows title bar and close button remain visible.
4. Confirm ordinary in-page application workflow still works.
5. Confirm the diagnostic log records the loaded policy path, `toolbar-primary-toolbar-hidden:true`,
   and ignored child command values.

## Navigation accelerator suppression

Issue #24 adds a separate gate for suppressing selected Back, Forward, and Reload accelerators.
The detailed key matrix and design options are tracked in
`docs/navigation-accelerator-research.md`. Baseline manual measurement before production policy
work uses `docs/navigation-accelerator-manual-test.md`.

Use a policy that leaves the toolbar visible but suppresses navigation accelerators:

```json
{
  "version": 1,
  "browserShell": {
    "toolbar-primary-toolbar-hidden": false,
    "toolbar-address-entry-hidden": false,
    "toolbar-history-command-hidden": false,
    "toolbar-reload-command-hidden": false,
    "toolbar-go-command-hidden": false,
    "toolbar-settings-command-hidden": false,
    "toolbar-diagnostics-command-hidden": false,
    "keyboard-history-command-disabled": true,
    "keyboard-reload-command-disabled": true
  }
}
```

1. Start with `--shell-policy <path-to-policy>`.
2. Confirm Back, Forward, and Reload toolbar buttons remain visible and operate according to their
   visible enabled/disabled state.
3. Confirm targeted browser accelerators for Back, Forward, and Reload are suppressed according to
   the documented key matrix.
4. Confirm `Ctrl+F` opens WebView2 find-in-page and `F3` continues the find session.
5. Confirm ordinary text editing shortcuts and page movement keys retain normal browser behavior.
6. Confirm clicked links, script navigation, redirects, form submission, and typed address
   navigation are not represented as blocked by this policy.
7. Confirm diagnostics identify the effective accelerator policy and log any unsupported key in a
   bounded way without recording typed characters or field contents.

Repeat with a policy that sets `toolbar-history-command-hidden:true` and
`toolbar-reload-command-hidden:true` but leaves `keyboard-history-command-disabled:false` and
`keyboard-reload-command-disabled:false`. Confirm hidden buttons alone do not suppress keyboard
behavior.

For fixture-based validation, start the navigation accelerator page directly with the same policy:

```powershell
dotnet run --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --navigation-accelerator-manual --shell-policy <path-to-policy> --show-diagnostics
```

The current production path uses WPF routed-event handling and logs
`navigation accelerator WPF suppression` when it sets `Handled=true` for a targeted policy command.
This does not claim the more precise WebView2 direct-controller
`IsBrowserAcceleratorKeyEnabled=false` behavior.

## Fail-safe policy handling

For each case, start with `--shell-policy <path>` and confirm the standard visible shell is used
with a warning in the diagnostic log:

- malformed JSON;
- unsupported `version`;
- unknown root property;
- unknown `browserShell` property;
- non-boolean restriction value; and
- file larger than the configured maximum.

## Command-line operations

1. Run `--export-shell-policy <path>` and confirm the process exits before WebView2 starts and
   writes a valid JSON policy.
2. Run `--apply-shell-policy <source> --shell-policy <target>` with a valid source and writable
   target. Confirm the target is atomically replaced and the process exits before WebView2 starts.
3. Run the same apply command with invalid source JSON. Confirm the target is unchanged.
4. Run the same apply command with a read-only or unwritable target. Confirm the command fails
   visibly and the target is unchanged.
5. Run `--reset-user-settings` and confirm only the normal-startup URL and user compatibility
   allow/deny decisions are reset.
6. Confirm `--reset-user-settings` does not change shell policy, compatibility profiles, WebView2
   user data, cookies, local storage, or package files.

## Compatibility boundary

1. Navigate to the built-in compatibility test page in standard mode.
2. Trigger `window.showModalDialog`, allow compatibility, and reload.
3. Restart in restricted mode with the same user decision.
4. Confirm the modal behavior still depends only on origin/API permission, not shell visibility.
5. Revoke the user decision after returning to standard mode and confirm shell policy is unchanged.

## Security boundary

The feature must not:

- disable WebView2 security or sandbox settings;
- grant compatibility APIs;
- include shell policy in user settings import/export;
- hide native close;
- expose policy mutation to web content; or
- claim kiosk or enterprise lockdown enforcement.

The future Issue #24 accelerator policy must additionally preserve `Ctrl+F`/`F3` find-in-page and
must not claim to block page script navigation, redirects, clicked links, form submission, typed
address navigation, or origin changes.
