# Architecture

## Current MVP solution structure

```text
ImprovisedEosl.sln

src/
  ImprovisedEosl.Core/
  ImprovisedEosl.ModalDialog/
  ImprovisedEosl.Spike.SyncModal/

tests/
  ImprovisedEosl.Spike.Tests/
```

The current dependency direction is:

- `ImprovisedEosl.Spike.SyncModal` -> `ImprovisedEosl.Core`
- `ImprovisedEosl.Spike.SyncModal` -> `ImprovisedEosl.ModalDialog`
- `ImprovisedEosl.Spike.Tests` -> both class libraries and
  `ImprovisedEosl.Spike.SyncModal` through project references; the Windows-targeted test executable
  directly validates the shell's pure compatibility-status presentation mapping without creating
  WPF controls
- `ImprovisedEosl.Core` and `ImprovisedEosl.ModalDialog` have no WPF or WebView2 dependency
- the two class libraries are currently independent of each other

`ImprovisedEosl.Core` owns the pure origin, approval persistence, payload validation,
navigation policy, legacy API detection, and rolling-log logic. `ImprovisedEosl.ModalDialog`
owns pure feature parsing and the policy that converts parsed legacy features into child
window options. The WPF spike retains WebView2 setup, host objects, STA lifecycle, actual
window mutation, UI, and test pages. Within the spike, host-object registration, shim
injection, synchronous modal execution, and test-only host registration are implemented by
separate bridge/host classes rather than nested in `MainWindow`.

Each child WebView2 also receives the compatibility bridge. A nested synchronous call opens
the next dialog on another STA thread and uses the same user data folder and origin policy.
The current MVP permits at most four simultaneously open dialog levels. A child document may
open another dialog only when its own current origin is already approved; approval of the
top-level origin is not inherited by a cross-origin child. Detection in an unapproved child is
logged, but consent is deferred to a top-level navigation and reload.

Fatal child renderer or browser process failures are converted into a structured modal failure
and close the affected child so the synchronous caller is not left waiting for the normal
dialog timeout. After a shared browser-process exit releases the synchronous call, the parent
waits for its environment exit notification and replaces the closed WebView2 control before
navigating back to the last URL. Auto-recoverable process failures are logged without closing
the dialog.

Renderer-unresponsive handling combines a notification threshold with a five-second
responsiveness-probe grace period. A responsive probe resets the observation count. A child
that remains unresponsive returns a structured failure and closes; a persistently unresponsive
parent causes a browser-process restart and parent WebView2 recreation. Separate STA threads do not imply separate renderer processes: the
same-origin parent and child were observed receiving the same unresponsive event.

Each dialog receives the calling window's HWND as its native owner. The caller is disabled
before the child is shown and restored on close and again from the STA runner's `finally`
path. Nested dialogs form an HWND owner chain without accessing WPF `Window.Owner` across
threads. See `docs/modal-window-ownership.md`.

This is an incremental extraction from the proven spike. The finer-grained App, WebView,
and Interop projects below remain a proposed target rather than current implementation.

## Proposed solution structure

```text
ImprovisedEosl.sln

src/
  ImprovisedEosl.App/
  ImprovisedEosl.Core/
  ImprovisedEosl.WebView/
  ImprovisedEosl.ModalDialog/
  ImprovisedEosl.Interop/

assets/
  show-modal-dialog-shim.js

tests/
  ImprovisedEosl.Tests/
  pages/
    parent.html
    dialog.html
```

## Component responsibilities

### ImprovisedEosl.App

- main window
- browser shell
- navigation UI
- startup profile selection
- compatibility status indicator for the current origin
- user consent prompt for discovered legacy API usage

### ImprovisedEosl.Core

- compatibility profile model
- URL matching
- origin allow-list matching
- user-approved origin persistence
- feature flags
- logging contracts
- serialization rules

### ImprovisedEosl.WebView

- WebView2 initialization
- early script injection
- origin-gated script injection
- low-privilege legacy API discovery injection
- navigation events
- session and user-data-folder management

### ImprovisedEosl.ModalDialog

- child dialog lifecycle
- separate STA thread
- feature parsing
- feature application policy that converts parsed dialog features to child window options
- return value collection
- cancellation behavior

### ImprovisedEosl.Interop

- synchronous host object
- JavaScript/native boundary
- validation and serialization

## Compatibility profiles

Example:

```json
{
  "id": "legacy-order-system",
  "displayName": "受発注システム",
  "startUrl": "https://order.example.local/",
  "allowedOrigins": [
    "https://order.example.local"
  ],
  "compatibility": {
    "showModalDialog": true
  }
}
```

Compatibility profile rules:

- Profiles must be independent of the general browser shell.
- Normal browsing must work without a compatibility profile.
- Compatibility behavior is opt-in per origin, either from a configured profile or from explicit user approval after detection.
- An origin match is scheme + host + explicit port.
- Wildcard matching is out of scope for the MVP unless it is documented and tested separately.
- A page that is not matched by an enabled profile must not receive the `showModalDialog` host object or shim.
- User-approved origins must be stored separately from built-in or admin-authored profile defaults so they do not conflict with future browser-compatibility settings.
- The current schema and loading limits are documented in `docs/compatibility-profiles.md`.

## Localization policy

Product copy should be authored from English source strings and localized through resource keys.

MVP language behavior:

- Default UI language: Japanese.
- Source copy language: English.
- Japanese strings are the default localization values.
- UI code should not hard-code user-facing strings once the browser shell moves beyond the spike.
- Future localization should add resource files rather than changing compatibility logic.

## Legacy API discovery and consent

The preferred user experience is discovery-driven:

1. Normal browsing starts with compatibility disabled for the current origin.
2. A low-privilege discovery shim detects calls to known removed APIs such as `window.showModalDialog`.
3. The browser prompts the user with the current origin and API name.
4. If the user allows it, the origin is added to the user-approved compatibility list for that API.
5. The page is reloaded, or the user is asked to reload, so the full compatibility shim and host object can be enabled for that origin.

Discovery rules:

- The discovery shim may be injected more broadly than the full compatibility shim.
- The discovery shim must not expose native dialog execution, file access, process access, or arbitrary native commands.
- Detection should be per API name and per origin.
- The prompt must show the exact origin, the detected API, and the risk that legacy compatibility changes page behavior.
- Deny must keep the site usable as a normal website where possible.
- Allow must be revocable from settings.
- A user approval should not silently grant future unrelated compatibility APIs.

Suggested prompt copy source:

- Detection prompt title: `Legacy API detected`
- Detection prompt body: `This site attempted to use window.showModalDialog, a removed browser API. Allow compatibility mode for this origin?`
- Allow button: `Allow and reload`
- Deny button: `Deny`
- Reload note in the prompt: `If allowed, the page will reload. Depending on the site, you may need to start the operation again from the top page.`

Default Japanese localization:

- Detection prompt title: `廃止されたAPIの呼び出しを検出しました`
- Detection prompt body: `このサイトは window.showModalDialog を使用しようとしました。このoriginで互換モードを有効にしますか？`
- Allow button: `許可してリロード`
- Deny button: `許可しない`
- Reload note in the prompt: `許可するとページをリロードします。サイトによってはトップページから操作し直してください。`
- Do not show a second OK-only confirmation after the user chooses Allow. Fewer modal steps are preferred.

Synchronous API caveat:

- `showModalDialog` is synchronous, so prompting during the first detected call cannot always preserve the original page control flow.
- MVP behavior should prefer: detect, ask, persist allow decision, then reload before enabling the full shim.
- A later experiment may test same-call enablement, but it must be documented separately because it requires broader native exposure during detection.

## Browser shell MVP

The browser shell MVP exists to make the project understandable to third-party reviewers and testers.

Minimum shell behavior:

- address bar
- navigate button or Enter-to-navigate
- back
- forward
- reload
- current URL display
- plain-text compatibility status indicator

The compatibility status indicator should show at least:

- `Compatibility: off`
- `Compatibility: legacy API detected; permission needed`
- `Compatibility: showModalDialog enabled for this origin`
- `Compatibility: blocked for this origin`

This status is not a security boundary by itself. It is a user-visible reflection of the compatibility profile decision.

## Security boundary

Compatibility behavior must be enabled only for explicitly allowed origins.

The host must reject:

- unexpected origins
- non-HTTP schemes unless explicitly allowed
- malformed size or position values
- excessive window dimensions
- untrusted native commands
- arbitrary host-object method exposure

The compatibility layer must not become a general-purpose native execution bridge.

Host-object exposure rules:

- Do not expose the synchronous modal host object to arbitrary websites.
- Add the host object only after the current document origin is known to be allowed, or expose only a minimal broker that refuses all unapproved origins before doing any work.
- The full JavaScript compatibility shim must be injected only for allowed origins.
- A broader discovery shim is allowed only if it is low-privilege and cannot execute native compatibility behavior by itself.
- Navigation to a non-allowed origin must remove or avoid compatibility behavior for the new document.
- Logs must distinguish "compatibility disabled" from "compatibility failed".

Current implementation rule:

- JavaScript-provided origin values are claims, not authority.
- The host compares each claimed origin with the actual current `ParentWebView.Source` origin before compatibility policy evaluation.
- Test-only broker methods are limited to the exact process-local HTTP test origin.
- Compatibility and test methods use separate host objects. The test object is added only for the local test origin and removed before other top-level navigations.
- Child dialog targets are limited to bounded HTTP(S) URLs without userinfo and are validated before child STA creation.
- Nested dialogs repeat the actual-document-origin check at every level and stop with a structured failure after four open dialog levels.
- See `docs/host-object-security.md`.
