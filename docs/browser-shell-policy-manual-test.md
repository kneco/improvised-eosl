# Browser shell policy manual test

Issue #3 should be validated from a normal user PowerShell after the JSON-only browser shell policy
is implemented. This checklist records the intended gate; it is not evidence that the feature is
already implemented.

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
    "primaryToolbar": "hidden",
    "addressEntry": "hidden",
    "historyCommands": "hidden",
    "reloadCommand": "hidden",
    "goCommand": "hidden",
    "settingsCommand": "hidden",
    "diagnosticsCommand": "hidden"
  }
}
```

1. Start with `--shell-policy <path-to-policy>`.
2. Confirm Back/Forward/Reload, editable address entry, Go, Settings, Diagnostics, compatibility
   status, and current-origin controls are all hidden.
3. Confirm the native Windows title bar and close button remain visible.
4. Confirm ordinary in-page application workflow still works.
5. Confirm the diagnostic log records the loaded policy path, `primaryToolbar:hidden`, and ignored
   child command values.

## Future navigation accelerator suppression

Issue #24 adds a separate future gate for suppressing selected Back, Forward, and Reload
accelerators. This checklist is not evidence that the behavior is implemented. The detailed key
matrix and design options are tracked in `docs/navigation-accelerator-research.md`. Baseline
manual measurement before production policy work uses `docs/navigation-accelerator-manual-test.md`.

Use a policy that leaves the toolbar visible but suppresses navigation accelerators:

```json
{
  "version": 1,
  "browserShell": {
    "primaryToolbar": "visible",
    "historyCommands": "visible",
    "reloadCommand": "visible",
    "navigationAccelerators": {
      "historyCommands": "suppressed",
      "reloadCommand": "suppressed"
    }
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

Repeat with a policy that hides Back/Forward/Reload toolbar commands but leaves
`navigationAccelerators` at `browser-default`. Confirm hidden buttons alone do not suppress
keyboard behavior.

## Fail-safe policy handling

For each case, start with `--shell-policy <path>` and confirm the standard visible shell is used
with a warning in the diagnostic log:

- malformed JSON;
- unsupported `version`;
- unknown root property;
- unknown `browserShell` property;
- invalid `primaryToolbar` value; and
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
