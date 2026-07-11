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
- wrapper address entry visibility when the toolbar is visible;
- wrapper back, forward, reload, and typed-address navigation command visibility when the toolbar
  is visible;
- wrapper Settings and Diagnostics command visibility when the toolbar is visible; and
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
    "primaryToolbar": "visible",
    "addressEntry": "editable",
    "historyCommands": "visible",
    "reloadCommand": "visible",
    "goCommand": "visible",
    "settingsCommand": "visible",
    "diagnosticsCommand": "visible",
    "navigationAccelerators": {
      "historyCommands": "browser-default",
      "reloadCommand": "browser-default"
    }
  }
}
```

Allowed values:

- `primaryToolbar`: `visible` or `hidden`
- `addressEntry`: `editable` or `hidden`
- `historyCommands`: `visible` or `hidden`
- `reloadCommand`: `visible` or `hidden`
- `goCommand`: `visible` or `hidden`
- `settingsCommand`: `visible` or `hidden`
- `diagnosticsCommand`: `visible` or `hidden`
- `navigationAccelerators.historyCommands`: `browser-default` or `suppressed`
- `navigationAccelerators.reloadCommand`: `browser-default` or `suppressed`

Rules:

- Unknown root, section, or property names fail the file closed.
- Missing optional command properties default to `visible`.
- `primaryToolbar:hidden` hides the complete wrapper toolbar: Back, Forward, Reload, address entry,
  Go, Settings, Diagnostics, compatibility status, and current-origin display.
- The native window title bar and close affordance remain visible and OS-owned when
  `primaryToolbar` is hidden.
- If `primaryToolbar` is `hidden`, individual toolbar command values are ignored and this
  normalization should be logged.
- If `addressEntry` is `hidden`, `goCommand` must be treated as `hidden` even if the file says
  `visible`; this normalization should be logged as an approximation.
- Toolbar visibility and accelerator suppression are independent. Hiding Back, Forward, or Reload
  buttons must not silently suppress browser accelerators for those commands, and suppressing an
  accelerator must not change button visibility.
- Missing `navigationAccelerators` properties default to `browser-default`.
- `navigationAccelerators` does not control `Ctrl+F`, `F3`, text editing, page movement keys,
  DevTools, print, zoom, or arbitrary page-defined shortcuts.
- File size should use the existing 1 MiB configuration limit and JSON depth should remain bounded
  to 32.

Invalid JSON, unsupported versions, unknown properties, oversized files, and impossible command
combinations must fail safe to the built-in standard shell and log a warning. The standard shell
means the primary toolbar, address entry, browser commands, Settings, Diagnostics, compatibility
status, and current origin are all visible.

## Full-toolbar hidden mode

`primaryToolbar:hidden` is allowed because many line-of-business deployments intentionally suppress
Back, Forward, Reload, and direct address entry so operators stay inside the application workflow.
In this mode the in-window origin and compatibility status controls are also hidden with the toolbar.
That is an explicit operational tradeoff, not a security guarantee.

Recovery from a bad full-toolbar policy is command-line based:

- start once with a known-good `--shell-policy <path>`;
- use `--export-shell-policy <path>` to generate a visible-toolbar template;
- use `--apply-shell-policy <source> --shell-policy <target>` to replace the deployed policy; or
- use `--reset-user-settings` for user-managed startup URL and compatibility decisions.

`--reset-user-settings` does not reset the administrator shell policy. It is a recovery path for
ordinary user state only. Restoring a hidden toolbar requires replacing the policy file or launching
with another policy path.

## Command-line operations

The first implementation should keep command-line operations offline and explicit:

- `--shell-policy <path>`: load a validated shell policy from the given path for this run.
- `--export-shell-policy <path>`: write the effective policy, or the built-in standard template
  when no policy file exists, then exit before starting WebView2.
- `--apply-shell-policy <source> --shell-policy <target>`: validate `source`, atomically replace
  `target`, then exit before starting WebView2.
- `--reset-user-settings`: delete or replace only user-managed settings, then exit before starting
  WebView2.

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

A future implementation must use `CoreWebView2Controller.AcceleratorKeyPressed` as the design
point for command-specific behavior. The implementation gate must distinguish these outcomes:

- set `IsBrowserAcceleratorKeyEnabled = false` for a targeted browser accelerator only when web
  content should still receive the key event;
- set `Handled = true` only when the host must stop both browser handling and propagation to web
  content for the targeted command; and
- leave `Ctrl+F` and `F3` in the existing find-in-page path.

The exact key matrix remains a validation item. At minimum, the design must measure or explicitly
reject Ctrl+R, F5, Alt+Left, Alt+Right, browser Back/Forward keys, and any Backspace-driven history
behavior before claiming suppression coverage. The policy must log unsupported or unrecognized
accelerator requests instead of implying broader enforcement.

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
4. Implement `primaryToolbar:hidden` as a full toolbar hide, including address entry, navigation
   commands, Settings, Diagnostics, compatibility status, and current-origin controls.
5. Keep native close visible and verify command-line recovery before treating full-toolbar hidden
   mode as usable.
6. Add the Issue #24 accelerator design only after pure policy tests distinguish toolbar command
   visibility from targeted browser accelerator suppression and after the key matrix above is
   validated.
7. Add a manual test that verifies restricted mode, invalid-policy fail-safe, CLI export/apply,
   reset-user-settings, ordinary browsing, compatibility consent, `Ctrl+F`, diagnostics logging,
   targeted Back/Forward/Reload accelerator behavior, and native close behavior.
