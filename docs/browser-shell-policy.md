# Browser shell policy

## Purpose

Issue #3 is an administrator/operations feature for hiding selected wrapper browser controls such
as the editable address entry and navigation commands. Issue #24 extends the same administrator
policy surface with a separate design question: whether selected host/browser accelerators for
Back, Forward, and Reload can be suppressed without disabling unrelated browser features. Neither
surface is a compatibility API, and neither may grant, deny, or emulate Internet Explorer behavior.

The policy is JSON-only. General users should not receive a Settings UI for this surface. If an
organization wants the mode to be enforced, the deployed policy file must be owned and protected by
operating-system permissions.

## Boundaries

This policy may control only two administrator-owned host surfaces:

- primary wrapper toolbar visibility;
- wrapper address entry visibility, including the embedded compatibility status chip, when the
  toolbar is visible;
- wrapper back, forward, and reload command visibility when the toolbar is visible;
- wrapper Settings/help hub visibility when the toolbar is visible; and
- future host/browser accelerator handling for the same Back, Forward, and Reload command group.

This policy must not:

- change WebView2 sandboxing, process isolation, storage, certificates, or network security;
- grant compatibility APIs such as `window.showModalDialog`, `window.open` feature handling, or
  `window.close` handoff;
- modify configured compatibility profiles or user-approved compatibility decisions;
- rewrite page script or inject a new host object;
- intercept arbitrary native commands or page keyboard input;
- claim kiosk, lockdown, or data-loss-prevention behavior;
- prevent all WebView2/browser accelerator behavior, including find-in-page; or
- hide the native window close affordance.

The existing `window.open` and top-level close handoff feature handling may approximate legacy
popup chrome. That path is compatibility behavior. The administrator shell policy must be modeled
separately, even if the final restricted presentation also hides the full primary toolbar.

## Trust model

The policy file is trusted configuration in the same sense as `config/compatibility-profiles.json`:
the application validates the schema and bounds, but file authenticity and write protection are
outside the application. Anyone who can modify the effective policy file can change the shell mode.

Recommended source order:

1. `--shell-policy <path>` if specified.
2. `config/browser-shell-policy.json` relative to the executable directory.
3. Built-in standard shell when no policy file exists.

The application must log the selected source and loaded mode. It must not silently create or repair
the policy file during normal browsing.

## Schema version 1 proposal

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
    "keyboard-history-command-disabled": false,
    "keyboard-reload-command-disabled": false
  },
  "functionKeyPolicy": {
    "f5Reload": true,
    "f6LocationFocus": true,
    "f11Fullscreen": true,
    "f12DevTools": true
  }
}
```

All values are JSON booleans. The `browserShell` keys use existing version 1 "turn this
restriction on/off" switches. The `functionKeyPolicy` keys default to normal browser-like behavior
and use explicit `false` to suppress one wrapper/browser function-key action:

| Key | `true` means | Scope |
| --- | --- | --- |
| `toolbar-primary-toolbar-hidden` | Hide the complete primary wrapper toolbar. | Toolbar presentation |
| `toolbar-address-entry-hidden` | Hide the editable address entry and its embedded compatibility status chip. | Toolbar presentation |
| `toolbar-history-command-hidden` | Hide Back and Forward wrapper commands. | Toolbar presentation |
| `toolbar-reload-command-hidden` | Hide the Reload wrapper command. | Toolbar presentation |
| `toolbar-go-command-hidden` | Accepted for schema version 1 compatibility. The current shell has no standalone Go button; typed-address navigation uses Enter in the address entry. | Deprecated toolbar presentation |
| `toolbar-settings-command-hidden` | Hide the Settings wrapper command. | Toolbar presentation |
| `toolbar-diagnostics-command-hidden` | Accepted for schema version 1 compatibility. Diagnostics are now opened from the shell settings/help hub. | Deprecated toolbar presentation |
| `keyboard-history-command-disabled` | Suppress targeted Back and Forward keyboard/browser accelerators. | Host/browser accelerator handling |
| `keyboard-reload-command-disabled` | Suppress targeted Reload keyboard/browser accelerators. | Host/browser accelerator handling |
| `functionKeyPolicy.f5Reload` | Allow F5 reload. Set `false` to suppress F5 reload only. | Function-key browser action |
| `functionKeyPolicy.f6LocationFocus` | Allow F6 to focus/select the wrapper address entry when that entry is visible and focusable. Set `false` to suppress F6. | Function-key browser action |
| `functionKeyPolicy.f11Fullscreen` | Allow F11 to toggle wrapper fullscreen. Set `false` to suppress F11 fullscreen. | Function-key browser action |
| `functionKeyPolicy.f12DevTools` | Allow WebView2 DevTools access through F12 and other DevTools entry points. Set `false` to disable WebView2 DevTools for the run. | Function-key browser action |

Rules:

- Unknown root, section, or property names fail the file closed.
- Missing optional `browserShell` command properties default to `false`.
- Missing optional `functionKeyPolicy` properties default to `true`.
- `toolbar-primary-toolbar-hidden:true` hides the complete wrapper toolbar: Back, Forward, Reload,
  address entry, the embedded compatibility status chip, the Settings/help hub gear, and
  current-origin display.
- The native window title bar and close affordance remain visible and OS-owned when
  `toolbar-primary-toolbar-hidden` is true.
- If `toolbar-primary-toolbar-hidden` is true, individual toolbar command values are ignored and
  this normalization should be logged.
- `toolbar-go-command-hidden` is retained so existing version 1 policy files continue to load and
  export. Setting it does not hide any additional current UI because the standalone Go button was
  removed after Issue #43.
- The visible Settings command is the shell settings/help hub entry. Clicking the gear and pressing
  F1 open the same wrapper-owned hub for application settings, compatibility status, and
  diagnostics. This is shell UI behavior, not page-level IE `onhelp` emulation.
- Compatibility status is a compact chip embedded inside the wrapper address entry, not a separate
  toolbar command. It remains wrapper-owned chrome with tooltip and UI Automation detail; it does
  not rewrite or expose page DOM.
- `toolbar-diagnostics-command-hidden` is retained so existing version 1 policy files continue to
  load and export. Setting it does not hide any additional current UI because the separate
  Diagnostics toolbar button moved behind the shell hub after Issue #59.
- Toolbar visibility and accelerator suppression are independent. Hiding Back, Forward, or Reload
  buttons must not silently suppress browser accelerators for those commands, and suppressing an
  accelerator must not change button visibility.
- Missing keyboard restriction properties default to normal browser behavior.
- `keyboard-history-command-disabled` applies only to `Alt+Left`, `Alt+Right`, and dedicated
  browser Back / Forward keys when the host can observe them.
- `keyboard-reload-command-disabled` applies only to `Ctrl+R` and `F5`.
- `keyboard-reload-command-disabled:true` remains the broad reload accelerator restriction for
  both `Ctrl+R` and `F5`. `functionKeyPolicy.f5Reload:false` suppresses only F5 and does not affect
  `Ctrl+R`.
- `functionKeyPolicy.f6LocationFocus:true` can focus only the wrapper address entry. If that entry
  is hidden, disabled, or otherwise unavailable, F6 is handled by the host and logged as ignored
  rather than escaping to hidden browser chrome.
- `functionKeyPolicy.f11Fullscreen:true` is a wrapper fullscreen toggle. It is not kiosk mode, DLP,
  or a security boundary.
- `functionKeyPolicy.f12DevTools:false` uses WebView2's DevTools availability setting. It does not
  disable browser accelerator keys globally and must not be implemented by setting
  `AreBrowserAcceleratorKeysEnabled=false`.
- Keyboard restriction keys do not control the F1 shell hub, `Ctrl+F`, `F3`, Backspace, text
  editing, page movement keys, print, zoom, or arbitrary page-defined shortcuts.
- File size should use the existing 1 MiB configuration limit and JSON depth should remain bounded
  to 32.

Invalid JSON, unsupported versions, unknown properties, oversized files, and impossible command
combinations must fail safe to the built-in standard shell and log a warning. The standard shell
means the primary toolbar, address entry, browser commands, Settings/help hub, compatibility
status chip, and current origin are visible. Diagnostics remain reachable from the hub.
Typed-address navigation is available by pressing Enter in the address entry.

## Full-toolbar hidden mode

`toolbar-primary-toolbar-hidden:true` is allowed because many line-of-business deployments
intentionally suppress Back, Forward, Reload, and direct address entry so operators stay inside the
application workflow. In this mode the in-window origin and embedded compatibility status chip are
also hidden with the toolbar. That is an explicit operational tradeoff, not a security guarantee.

Recovery from a bad full-toolbar policy is command-line based:

- start once with a known-good `--shell-policy <path>`;
- use `--export-shell-policy <path>` to generate a visible-toolbar template;
- use `--apply-shell-policy <source> --shell-policy <target>` to replace the deployed policy; or
- use `--reset-user-settings` for user-managed startup URL and compatibility decisions.

`--reset-user-settings` does not reset the administrator shell policy. It is a recovery path for
ordinary user state only. Restoring a hidden toolbar requires replacing the policy file or launching
with another policy path.
F1 does not override `toolbar-primary-toolbar-hidden:true`; that mode intentionally hides the
in-window shell entry points, so recovery remains command-line or administrator-policy based.

## Command-line operations

The first implementation keeps command-line operations explicit:

- `--shell-policy <path>`: load a validated shell policy from the given path for this run. This
  source selection is implemented for the current toolbar presentation and keyboard accelerator
  policy.
- `--export-shell-policy <path>`: write the effective policy, or the built-in standard template
  when no policy file exists, then exit before starting WebView2. This is implemented.
- `--apply-shell-policy <source> --shell-policy <target>`: validate `source`, atomically replace
  `target`, then exit before starting WebView2. This is implemented.
- `--reset-user-settings`: delete or replace only user-managed settings, then exit before starting
  WebView2. This is implemented for the user-managed initial URL and user compatibility
  allow/deny decisions.

`--reset-user-settings` must not modify:

- `config/browser-shell-policy.json`;
- any path supplied by `--shell-policy`;
- `config/compatibility-profiles.json`;
- WebView2 user data;
- cookies, local storage, certificates, or cache; or
- release/package files.

`--apply-shell-policy` is a convenience for administrators and test automation, not a privilege
boundary. It must not elevate permissions or bypass the operating system. If the current Windows
user can write the target path, the command can update it; otherwise it must fail visibly and leave
the previous file intact.

## WebView2 constraints

Hiding wrapper buttons does not automatically prevent browser or page navigation. Web content can
still navigate itself, links can be clicked, redirects can occur, and WebView2/browser accelerator
behavior may remain active.

The current find-in-page design deliberately keeps WebView2 browser accelerator keys enabled for
`Ctrl+F` and `F3`. `CoreWebView2Settings.AreBrowserAcceleratorKeysEnabled = false` is therefore too
broad for Issue #24 because Microsoft documents it as disabling browser accelerators including
Find on Page, Reload, print, zoom, DevTools, and special browser-function keys.

Issue #57 keeps function-key browser actions as wrapper shell policy instead of IE keyboard-event
mutation. The measured `window.event.keyCode = 0` pattern did not explain modern WebView2 / Edge IE
mode function-key behavior. The supported implementation therefore handles only wrapper-owned or
WebView2-exposed browser actions: F5 reload, F6 address focus, F11 wrapper fullscreen, and F12
DevTools availability. It does not install a page shim, rewrite scripts, emulate writable
`KeyboardEvent` objects, or log typed keys.

The first production accelerator implementation uses the WPF `PreviewKeyDown` route and sets
`KeyEventArgs.Handled = true` for targeted Back, Forward, and Reload commands when the matching
`keyboard-...-disabled` policy key is true. This matches the stronger "host handled the key"
behavior and intentionally does not promise that page JavaScript receives the suppressed key.

`CoreWebView2Controller.AcceleratorKeyPressed` remains the design point for any future, more
precise command-specific behavior. A future direct-controller implementation must distinguish
these outcomes:

- set `IsBrowserAcceleratorKeyEnabled = false` for a targeted browser accelerator only when web
  content should still receive the key event;
- set `Handled = true` only when the host must stop both browser handling and propagation to web
  content for the targeted command; and
- leave `Ctrl+F` and `F3` in the existing find-in-page path.

The exact key matrix remains a validation item for each target deployment. The current
implementation targets `Ctrl+R`, `F5`, `Alt+Left`, `Alt+Right`, and dedicated browser Back/Forward
keys when the WPF route observes them. Backspace is excluded because the baseline did not show
history-back navigation in the tested flow. The policy must log unsupported or unrecognized
accelerator requests instead of implying broader enforcement. The pre-implementation evidence and
candidate matrix are recorded in `docs/navigation-accelerator-research.md`; the baseline manual
fixture and run checklist are recorded in `docs/navigation-accelerator-manual-test.md`.

For function keys, F5 and F6 use the same targeted WPF routed-event handling path when the host
observes the key. F11 is implemented as a wrapper window-state action. F12 is controlled through
`CoreWebView2Settings.AreDevToolsEnabled` because DevTools is a WebView2 host capability rather
than a page DOM compatibility behavior.

Even with targeted accelerator suppression, the feature remains workflow guidance rather than a
kiosk security boundary. It does not block page script navigation, redirects, clicked links,
location assignment, form submission, or origin changes.

Navigation policy remains separate. If an organization needs to restrict reachable origins, that is
a future allow-list/navigation-control feature with its own security review.

## Permission model

Shell policy is process-level host configuration, not origin-scoped page permission.

- It applies before any page is trusted.
- It is not affected by compatibility consent.
- A site cannot request, observe, or change it through the compatibility broker.
- User Settings import/export must not include it.
- Revoking user compatibility decisions must not change it.

## Implementation gates

1. Add a pure `BrowserShellPolicyStore` in `ImprovisedEosl.Core` with bounded JSON loading,
   normalization diagnostics, and template export.
2. Add tests for missing file, valid standard/restricted modes, unknown properties, hidden trust
   fields, oversized input, unsupported version, command-line option parsing, export, apply, and
   reset-user-settings target selection.
3. Apply the presentation in WPF without changing compatibility policy, startup profile selection,
   WebView2 settings, local-content loading, or modal synchronization.
4. Implement `toolbar-primary-toolbar-hidden:true` as a full toolbar hide, including address entry,
   navigation commands, Settings/help hub gear, the embedded compatibility status chip, and
   current-origin controls.
5. Keep native close visible and verify command-line recovery before treating full-toolbar hidden
   mode as usable.
6. Wire the Issue #24 accelerator keys to WPF routed-event suppression after pure policy tests
   distinguish toolbar command visibility from targeted browser accelerator suppression and after
   the key matrix above is validated.
7. Add a manual test that verifies restricted mode, invalid-policy fail-safe, CLI export/apply,
   reset-user-settings, ordinary browsing, compatibility consent, `Ctrl+F`, diagnostics logging,
   targeted Back/Forward/Reload accelerator behavior, and native close behavior.
