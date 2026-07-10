# Browser shell policy

## Purpose

Issue #3 is an administrator/operations feature for hiding selected wrapper browser controls such
as the editable address entry and navigation commands. It is not a compatibility API and must not
grant, deny, or emulate Internet Explorer behavior.

The policy is JSON-only. General users should not receive a Settings UI for this surface. If an
organization wants the mode to be enforced, the deployed policy file must be owned and protected by
operating-system permissions.

## Boundaries

This policy may control only host shell presentation:

- primary wrapper toolbar visibility;
- wrapper address entry visibility when the toolbar is visible;
- wrapper back, forward, reload, and typed-address navigation command visibility when the toolbar
  is visible; and
- wrapper Settings and Diagnostics command visibility when the toolbar is visible.

This policy must not:

- change WebView2 sandboxing, process isolation, storage, certificates, or network security;
- grant compatibility APIs such as `window.showModalDialog`, `window.open` feature handling, or
  `window.close` handoff;
- modify configured compatibility profiles or user-approved compatibility decisions;
- rewrite page script or inject a new host object;
- intercept arbitrary native commands;
- claim kiosk, lockdown, or data-loss-prevention behavior;
- prevent all WebView2/browser accelerator behavior; or
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
    "diagnosticsCommand": "visible"
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
`Ctrl+F` and `F3`. Disabling browser accelerators globally to suppress Back, Forward, Reload, or F1
would conflict with that behavior and requires a separate design gate. A later enforcement phase may
evaluate a narrow accelerator policy, but version 1 of the shell policy should be described as
wrapper-shell presentation and workflow guidance, not a kiosk security boundary.

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
6. Add a manual test that verifies restricted mode, invalid-policy fail-safe, CLI export/apply,
   reset-user-settings, ordinary browsing, compatibility consent, `Ctrl+F`, diagnostics logging,
   and native close behavior.
