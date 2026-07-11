# Implementation plan

## Current decision

The MVP must first validate the synchronization model documented in `docs/technical-feasibility.md`.

Do not begin broad implementation until the proof of concept confirms that a synchronous WebView2 host-object call can remain blocked while a child WebView2 on a separate STA thread remains interactive and returns a value.

Use WPF for the first proof of concept. WinUI 3 may be reconsidered only after the synchronization model is proven.

The synchronization gate and measured technical-MVP acceptance priorities have passed. Release
`v0.1.6-mvp`, including the bounded `window.open()` extension, is complete. The synchronization
MVP is therefore closed as a technical feasibility milestone. The broader product-MVP exit is now
defined in `docs/mvp-readiness.md`: release-branch review, merge only when a separate feature branch
exists, complete validation evidence, a successful new tag workflow, verified GitHub Release ZIP
asset, and repository cleanup are all required. Remaining work items are classified as
post-MVP and are not implicit release blockers. All broader product-MVP gates passed for
`v0.1.7-mvp`; that selected milestone is complete without making a production-readiness claim.

## Phase 0: feasibility gate

- confirm supported .NET version
- confirm target Windows versions
- confirm target WebView2 Runtime version or minimum supported version
- use WPF for the MVP proof of concept
- define packaged vs unpackaged app assumption for the PoC
- define custom user data folder location for shared-session testing
- document origin and scheme policy for local manual test pages
- document cancellation, timeout, initialization failure, and child crash outcomes
- document that parent JavaScript callbacks are forbidden while the sync host call is blocked
- document that nested parent-thread modal loops are forbidden

Exit criteria:

- `docs/technical-feasibility.md` exists and records WebView2 constraints.
- The central synchronization model has a concrete PoC task.
- No browser-shell work is started.

## Phase 1: synchronization proof of concept

- create minimal WPF desktop application
- initialize parent WebView2
- load local parent test page
- inject only the synchronous host object required for the test
- log the thread IDs involved in parent WebView2 creation, host object invocation, child STA creation, and child close
- prove a primitive synchronous return value without creating a child WebView2

Exit criteria:

- Parent JavaScript blocks during the synchronous host call.
- Native host code returns a primitive value to the original JavaScript call site.
- The host does not call back into parent JavaScript during the blocked call.

Status:

- Automatic PoC passed for synchronous host-object blocking and return propagation.
- Build verified with .NET SDK 8.0.422.

## Phase 2: separate STA child WebView2 proof

- start a dedicated STA thread
- create child window and WebView2
- run a message pump on the child STA
- pass a local child URL and serialized arguments
- keep child interactive
- wait for close from the parent host method without pumping a nested modal loop on the parent WebView2 UI thread
- return serialized value
- add timeout diagnostics for child initialization and child close

Exit criteria:

- Parent JavaScript after the call does not run while the child is open.
- Child WebView2 remains mouse- and keyboard-interactive.
- Child DOM events run while the parent call is blocked.
- Closing the child returns a serialized value to the original JavaScript call.
- Repeated open/close cycles do not deadlock.

Status:

- Automatic PoC passed for child WebView2 initialization on separate STA threads.
- Automatic PoC passed for child DOM timer execution while parent JavaScript was blocked.
- Automatic PoC passed for three repeated open/close cycles.
- Manual keyboard and mouse interaction passed.

## Phase 3: session and lifecycle validation

- create parent and child WebView2 controls with a shared custom user data folder or shared environment
- verify cookie or storage visibility between parent and child
- verify child navigation failure behavior
- verify child window close and cancellation behavior
- verify process-failure logging where practical
- decide nested-dialog behavior

Exit criteria:

- Session sharing is demonstrated or explicitly marked unsupported for MVP.
- Cancellation returns `undefined`.
- Failures are logged distinctly from successful compatibility behavior.

Status:

- HTTP-based cookie sharing passed.
- HTTP-based parent-to-child `localStorage` sharing passed.
- Child-to-parent `localStorage` visibility is delayed until after the parent event loop resumes.
- `sessionStorage` is not considered shared for separate top-level windows.
- Cancellation returned `undefined` in manual validation.
- Navigation failure returned a structured non-success result in automatic validation.
- Timeout returned a structured non-success result and closed the child window in automatic validation.
- Parent and child WebView2 controls subscribe to `ProcessFailed` and log failure kind, reason, exit code, and description.
- `--process-failure-auto` crashed the child renderer in isolation and observed `RenderProcessExited / Crashed`. The blocked parent resumed with a structured `child-process-failure` after about 1.2 seconds, before the 10-second test timeout, and the child WebView was disposed.
- `--browser-process-failure-auto` terminates the shared browser process while a child dialog is open. The child returns a structured failure, the blocked parent call unwinds, and the parent waits for the environment exit event before replacing its WebView2 control and restoring the last URL.
- `--unresponsive-auto` induces a child renderer hang and native test input. The first notification starts a responsiveness probe; a failed five-second grace period returns a structured child failure and restores the owner before timeout. The parent independently probes the shared renderer after its blocked call unwinds.
- `--parent-unresponsive-auto` proved that normal reload does not reliably interrupt a persistently hung renderer. The final policy restarts the browser process after the grace period, then recreates the parent WebView2 and restores its URL through the browser-exit recovery path.
- WebView2 unresponsive-event latency varied from about 16 seconds to no event within 120 seconds under the same injected hang and click. The parent automatic mode uses a test-only 45-second watchdog to keep the recovery-path test finite; normal browsing remains event-driven.
- Multi-process UDF recovery remains unvalidated; see `docs/webview2-process-failure.md`.
- Nested `showModalDialog` passed automatically: the child host call opened a grandchild WebView2 on a third STA and returned the grandchild value through the child to the original parent JavaScript call.
- Nested calls are capped at four open dialog levels. The fifth call returns `nested-dialog-depth-exceeded` without creating another STA.
- Every nested level rechecks its own actual origin. An approved `127.0.0.1` parent did not grant nested execution to a child navigated to unapproved `localhost`; child-origin consent is deferred to top-level navigation and reload.
- Native owner modality is enforced across STA threads: the calling HWND is disabled before showing a child and restored on close, timeout, process failure, or STA failure. Four-level nesting restored each immediate owner from the inside out.
- The policy is application-modal, not system-modal: unrelated applications and Alt+Tab remain available. See `docs/modal-window-ownership.md`.

## Phase 4: browser shim

- inject `window.showModalDialog`
- expose `window.dialogArguments`
- capture `window.returnValue`
- override close behavior safely
- add cancellation semantics
- reject unsupported origins and schemes before invoking native dialog behavior
- log unsupported values instead of pretending compatibility

Status:

- PoC-level `window.showModalDialog` shim path passed.
- Origin gating is implemented for HTTP(S) origins; unsupported schemes fail closed.

## Phase 5: compatibility profiles and origin gating

- define a small profile JSON schema for the PoC
- include allowed origins as scheme + host + explicit port
- keep profile loading independent from the browser shell
- store user-approved origins separately from configured profile defaults
- expose `showModalDialog` only for enabled origins
- inject the shim only for enabled origins
- add a plain-text compatibility status indicator next to the address bar
- log blocked compatibility requests for non-allowed origins

Status:

- Runtime origin gating uses normalized `scheme://host:effective-port` identity.
- User-approved origins are loaded from a versioned JSON store independently from runtime-only test allowances.
- Invalid or corrupt approval files fail closed and are logged.
- Versioned configured compatibility profiles are loaded independently from the browser shell and user approval store.
- Configured and user/runtime grants are held in separate policy collections; user revocation cannot remove a configured grant.
- Profile loading is bounded and fail-closed for invalid JSON, unknown properties, unsafe origins, unsupported versions, and excessive file/profile/origin counts.
- The checked-in profile file is empty, so ordinary browsing and discovery consent behavior remain unchanged until an administrator adds a profile.
- `--profile-auto` proves that a configured grant alone enables the synchronous JavaScript-to-child-WebView2 path and return-value propagation without user or runtime approval.
- `--profile=<id>` and `--profile <id>` select a validated profile and navigate to its `startUrl`; invalid, unknown, or multiple selections fail visibly instead of falling back.
- `--startup-profile-auto --profile=automatic-configured-origin` validates profile resolution and initial WebView2 navigation without a runtime allowance.
- The toolbar compatibility-settings window lists and revokes user-approved origin/API pairs.
- Revoking the active origin reloads the page after a successful atomic save.
- Host calls validate the JavaScript-claimed origin against the actual current Parent WebView2 source before applying the allow policy.
- Test-only host methods are restricted to the exact process-local HTTP test origin; automatic completion also requires an auto-run mode.
- The globally visible `compatibilityBroker` exposes only permission, discovery, and dialog execution. Test methods live on a separate `testProbe` that is registered only for local test navigation and removed before other origins.
- `--origin-guard-auto` verifies spoofed claimed origins are blocked before child STA creation.
- Dialog target URLs are limited to absolute HTTP(S), 8192 characters, and no userinfo; invalid targets are rejected before child STA creation.
- Child redirects and script navigations are checked again on every `NavigationStarting`; `--navigation-auto` verifies a blocked runtime navigation returns synchronously.
- URL logs omit query strings and fragments.

Exit criteria:

- General websites can be browsed without receiving compatibility host objects.
- Allowed test origins show `Compatibility: showModalDialog enabled for this origin`.
- Non-allowed origins show `Compatibility: off` or `Compatibility: blocked for this origin`.

## Phase 6: legacy API discovery and user consent

- add a low-privilege discovery shim for `window.showModalDialog`
- detect attempted legacy API usage on non-enabled origins
- prompt the user with origin, API name, and behavior-change warning
- support Allow and Deny
- persist Allow as a user-approved origin for that specific API
- require reload before enabling the full compatibility shim
- use a single consent dialog with `許可してリロード` and `許可しない` actions
- include the reload/top-page warning inside the consent dialog instead of showing a second OK-only message
- show `Compatibility: legacy API detected; permission needed` while waiting for user action
- log user decisions

Status:

- Discovery, Allow/Deny, reload, status display, and decision logging are implemented.
- Allow is persisted per normalized origin and API across application restarts.
- Persistence behavior is documented in `docs/compatibility-origin-persistence.md`.
- Consent and compatibility-settings copy now use resource-style keys with English source copy and Japanese default values.

Localization requirements:

- author source copy in English
- default UI language is Japanese
- keep localized strings in resource-style keys once UI implementation begins

Exit criteria:

- A normal website can be browsed without receiving the full compatibility host object.
- A test page that calls `window.showModalDialog` on a non-enabled origin triggers a permission prompt.
- Allow enables `showModalDialog` for that origin after reload.
- Deny leaves the origin without the compatibility shim.
- User approval for `showModalDialog` does not enable unrelated future compatibility APIs.

### Revocation on an already loaded document

Issue #7 identified a stale-document edge case: a document created while `showModalDialog` was
allowed retained the execution version of the injected JavaScript function after the user revoked
that decision in Settings. The native broker still rejected the dialog, so the security boundary
remained closed, but the call appeared unresponsive instead of returning to discovery and consent.

Implementation constraints and assumptions:

- WebView2 document-created scripts cannot be replaced retroactively on an already loaded document.
- The injected `showModalDialog` function must therefore query the native origin policy on every
  call, rather than selecting an execution-only or discovery-only implementation once at document
  creation time.
- An unapproved call uses only the existing low-privilege detection path. Revocation must not
  expose dialog execution, broaden origin matching, or grant another compatibility API.
- Explicit denials continue to suppress repeated prompts in the native broker.
- Configured grants remain administrator-authored and cannot be revoked through user Settings.
- The synchronous execution path is unchanged after the current-origin and current-policy checks
  succeed; this fix does not alter the validated STA synchronization model.

Validation:

- retain the existing policy tests for allow, revoke, configured grants, and denial;
- add a browser regression mode that loads an allowed document, revokes its runtime decision
  without navigation, invokes the already-installed function, and proves that detection occurs
  without opening a child dialog; and
- manually verify allow, revoke in Settings, save, invoke again on the same page, and confirm that
  the consent prompt reappears.
- The user sees a clear warning that the current interrupted operation may need to be restarted from the site's top page.

## Phase 7: minimal browsing shell

- add address bar
- add navigate action
- add back, forward, and reload controls
- show the current URL
- keep shell UI separate from modal-dialog compatibility logic
- keep the browser shell intentionally minimal until MVP behavior is stable

Exit criteria:

- A tester can navigate to ordinary websites.
- Ordinary websites do not receive the `showModalDialog` shim unless their origin is explicitly allowed.
- The current compatibility state is visible as text.

Status:

- Complete for the MVP. Ordinary browsing, navigation controls, current URL, compatibility state,
  and origin-gated injection have been manually and automatically exercised.

## Phase 8: dialog features

- use `docs/dialog-feature-reference-checklist.md` as the raw Edge IE mode evidence log
- use `docs/dialog-feature-compatibility.md` as the MVP compatibility contract for measured feature behavior
- keep `docs/dialog-feature-application-design.md` as the boundary between parser, compatibility policy, and WPF mutation
- preserve parser behavior already aligned with measurement: required `px` size units, decimal truncation, negative size invalidation, duplicate size last-wins, `:` / `=` separators, case-insensitive names, unknown-field logging
- implement WPF child-window application for `dialogWidth`, `dialogHeight`, `dialogLeft`, `dialogTop`, `center`, and `resizable`
- map omitted `center` to centered placement unless explicit left/top are present
- let explicit `dialogLeft` / `dialogTop` override centering
- map only `resizable:yes` to a resizable WPF window; omitted `resizable` and `resizable:no` map to no-resize
- implement screen-aware clamping for zero, huge, negative-position, and offscreen-position cases in the WPF application layer
- keep `status` and `scroll` parsed and logged as unsupported for MVP despite measured IE effects
- log unknown fields and intentional behavior mismatches
- keep spike-only diagnostics such as `timeoutMs` separate from legacy feature compatibility

Status:

- Edge IE mode reference measurements are recorded for the MVP matrix.
- Parser tests are aligned with measured size, separator, duplicate, negative-size, and resize-default behavior.
- `status` and `scroll` have measured effects but remain out of MVP application scope.
- WPF child-window application is implemented for measured MVP fields: size, explicit position, default centering, offscreen clamping, and resize mode.
- Automated WebView2 feature smoke is available through `--feature-auto` and verifies return propagation plus applied size/position observations for a fixed subset.
- Automated WebView2 feature smoke also covers unsupported `status`, `scroll`, and unknown-field diagnostics to ensure unsupported behavior is logged without breaking synchronous return propagation.
- Manual WebView2 resize verification passed for `resizable:yes`, `resizable:no`, and omitted `resizable` on 2026-06-27.
- Native WPF bounds and DPI are logged separately from Chromium `window.outerWidth` / `window.outerHeight` so chrome comparisons do not conflate the embedded WebView viewport with the desktop window frame.
- Manual WebView2 chrome review passed at 100% DPI on 2026-06-27: standard title bar and close button, no extra application header, and no clipped content top.
- The child page's `Cancel` button returned JavaScript `undefined`, confirming the explicit cancellation path during the chrome review.
- `--native-close-auto` exercises the WPF `Window.Close()` path used by the title-bar X and verifies that the synchronous parent call resumes with JavaScript `undefined` without a child return message.
- Literal title-bar X interaction passed through the `--native-x-ui` harness on 2026-06-28: the child closed without a JavaScript close message, the host returned `undefined`, and the parent recorded `returnedUndefined:true` before exiting normally.
- Remaining implementation work is broader repeatability checks across monitor and DPI configurations.
- Cowork's broader IE compatibility research has been reviewed in `docs/ie-compat-research-review.md`; useful inventory items were adopted, while claims that conflict with corrected measurements or current Microsoft lifecycle wording remain non-normative.

Exit criteria:

- The project has a repeatable reference-test checklist for dialog feature behavior.
- The project has documented Microsoft Edge IE mode setup steps for the reference test.
- The project has a documented WebView2 smoke test for the reference page harness.
- The project has a documented feature application layer that separates parsing from WPF window mutation.
- The spike calculates, logs, and applies reference-validated MVP dialog window options to the WPF child window.
- Runtime and tests label the measured subset `ReferenceValidated`; safety clamps and unsupported fields remain explicit approximations.
- Parser behavior is no longer based only on project-local assumptions.
- Child window size, position, and resize behavior are applied in a way that is either IE-compatible or explicitly documented as an approximation.

## Phase 9: repeatability and tests

- repeated open/close test
- deadlock and timeout diagnostics
- authentication test
- automated tests for feature parsing and serialization
- automated or manual checks for applied dialog size, position, and resize behavior
- manual test page for child WebView2 interactivity and blocking behavior

Status:

- Standard legacy child code using `window.returnValue = value; window.close()` is covered by automatic synchronous smoke tests; project-specific close helpers are no longer used.
- JSON arguments and return values are bounded to 1 MiB UTF-8 and depth 64 with independent native validation.
- `--payload-auto` covers oversized arguments and cyclic return-value rejection without deadlock.
- Payload logs are length-bounded and include byte counts.
- Feature strings are limited to 16 KiB UTF-8 and 128 entries before parsing; `--payload-auto` verifies oversized feature rejection without extra child STA creation.
- Pure compatibility policy has been extracted from the WPF spike into `ImprovisedEosl.Core` and `ImprovisedEosl.ModalDialog` class libraries.
- The test project now uses project references instead of compiling linked copies of production source files.
- The four-project solution builds, and the current policy suite contains 54 passing checks.
- Fifteen WebView2 automatic modes are available after adding parent-only renderer recovery: `--auto`, `--session-auto`, `--failure-auto`, `--feature-auto`, `--payload-auto`, `--origin-guard-auto`, `--navigation-auto`, `--native-close-auto`, `--nested-auto`, `--process-failure-auto`, `--browser-process-failure-auto`, `--unresponsive-auto`, `--parent-unresponsive-auto`, `--profile-auto`, and `--startup-profile-auto`.
- Parent host-object registration, legacy shim injection, modal execution, and test-probe registration are separated from `MainWindow` into focused bridge/host classes within the WPF spike.
- Automatic modes use process-specific temporary WebView2 user data folders so they do not contend with or mutate the normal browsing profile; session sharing is still tested within each process.
- Automatic startup exceptions and the 30-second parent WebView2 initialization timeout exit nonzero instead of leaving a failed test window open.
- Parent and child WPF WebView2 controls are explicitly disposed when their windows close to release Runtime processes and profile resources promptly.
- All fifteen automatic modes passed serially from a clean process state on 2026-06-28. Rapid creation of multiple fresh WebView2 processes can still produce Runtime-level `GpuProcessExited` / `BrowserProcessExited` failures on the current test machine even with distinct user data folders; the smoke modes should not be treated as a parallel or high-frequency process-churn test.
- Cowork's detailed `showModalDialog` edge-case inventory has been reviewed. Parser edge cases supported by evidence are covered by tests.
- The IE-mode-safe argument boundary harness showed that direct strings at 4,000/4,096/4,097/5,000 characters and 5,000-character strings nested in objects and arrays all arrived intact. The MVP keeps its 1 MiB JSON safety boundary and does not emulate the older documented 4,096-character truncation for the current Edge IE mode target.

## Phase 10: polished shell

- Edge-like minimal window chrome
- navigation controls
- compatibility-mode indicator
- configuration profile loading

Status:

- Normal startup opens a neutral local home page with web search and explicit shortcuts; automatic validation continues to open `parent.html` directly, and a selected compatibility profile still takes precedence.
- A local legacy-EC comparison sample uses separate same-origin HTTP parent and child pages so `showModalDialog`, `dialogArguments`, and `returnValue` can be evaluated without conflating the core modal MVP with rejected `file:` / `data:` navigation, the separately bounded `window.open()` extension, or unsupported ActiveX behavior.
- The legacy-EC sample passed an end-to-end UI check on 2026-06-30: first-call discovery and reload completed, the child received `productName` and `price`, quantity `3` returned synchronously, and the parent cart rendered `3` items totaling `294,000` yen.
- Child WPF chrome follows the child document title, with `Dialog` as a bounded fallback, instead of exposing the internal spike name.
- The diagnostic panel is hidden by default and can be toggled from the toolbar.
- `--show-diagnostics` starts with the panel visible for troubleshooting.
- File logging remains active and rotates at 5 MiB with one backup.
- Diagnostic behavior and data-handling caveats are documented in `docs/diagnostics.md`.
- Versioned JSON compatibility profile loading and command-line startup selection are implemented; a graphical profile chooser and profile editing UI remain unimplemented.

This phase remains lower priority than synchronous blocking behavior, child dialog usability, return value propagation, dialog argument propagation, session sharing, and feature string parsing.

## Phase 11: experimental distribution

Status:

- `scripts/publish-dist.ps1` creates a self-contained `win-x64` Release package under `dist/`.
- The package includes the .NET 8 runtime, WebView2 native loader, HTML pages, configuration files, and a concise end-user README.
- The package remains folder-based because the native loader and editable page/configuration assets must stay explicit; single-file publishing is not an MVP requirement.
- The generated ZIP was validated by running its published executable with `--auto`; all three synchronous child-dialog cycles completed and the process exited with code 0 on 2026-06-28.
- Microsoft Edge WebView2 Runtime remains an external prerequisite.
- A tag-triggered GitHub Actions workflow runs policy tests, builds the self-contained package, and attaches it to a GitHub Release.
- Code signing, an installer, automatic updates, and enterprise deployment remain out of scope.

### Post-MVP ZIP root simplification

Objective:

- Make the launch executable visible immediately after extraction by removing the redundant
  versioned wrapper directory inside the ZIP.

Decisions and constraints:

- Keep the archive filename versioned, but place the existing publish output directly at the ZIP
  root.
- Do not move managed assemblies, the WebView2 native loader, `config`, or `pages` relative to the
  executable. Their tested adjacent layout remains authoritative.
- Keep `README.txt`, `LICENSE.txt`, and `THIRD-PARTY-NOTICES.txt` at the ZIP root.
- Do not introduce a launcher, installer, single-file publishing, or any compatibility/security
  behavior change.
- Treat extraction into a new directory as the supported workflow because a flat archive can mix
  files into an existing destination.

Validation gate:

- Add an automated archive-layout check that rejects a versioned wrapper directory, requires the
  executable and required documentation at the ZIP root, and verifies the native loader,
  `config`, and `pages` are retained.
- Generate and inspect a self-contained `win-x64` ZIP locally.
- Run policy tests and a Release build.
- Run the extracted executable with `--auto` from a normal user PowerShell if the agent-launched
  WebView2 process is constrained. Do not weaken Chromium or WebView2 security flags.

Status (2026-07-06):

- Implemented direct-at-root ZIP entries while retaining the unchanged folder-based publish
  output on disk.
- The automated layout check passed for all 499 archive entries and confirmed the required root
  documents, executable, native loader, configuration, and HTML pages.
- All 60 policy checks passed, the self-contained Release publish completed, and the extracted
  executable completed `--auto` with exit code 0 from the agent environment. No normal-user
  fallback was required for this run.
- Pull requests targeting `main` build the self-contained package, run the archive-layout check,
  and retain the validated ZIP briefly as a workflow artifact. Tag pushes continue to use the
  separate release workflow and publish only after the same layout check passes.

## Phase 12: safe local HTML loading

- keep direct `file:` compatibility origins rejected
- accept a single local `.html` or `.htm` file from drag-and-drop or the address bar
- expose only the selected file's directory through an application-owned loopback HTTP server
- navigate the existing parent WebView2 instead of opening another native window
- keep local compatibility approval session-only and revoke it when the selected root changes
- preserve relative resources and relative `showModalDialog` child URLs
- add a separate small-window manual test parent and child page
- document that server-side code and absolute `file:` references are unsupported

Exit criteria:

- A dropped local HTML file opens in the current main WebView2.
- The local page reaches the normal legacy API detection and consent flow.
- A relative local child dialog remains interactive and returns synchronously.
- Path traversal, non-HTML entry files, multiple drops, and direct `file:` child navigation remain rejected.
- Local loopback approval is not written to the persistent user approval store.

Status:

- Absolute Windows paths and `file:` URLs entered in the address bar are mapped to a loopback HTTP origin.
- The selected directory is served by a dedicated loopback-only server with traversal and reparse-point rejection.
- Local compatibility approvals are session-only and are revoked when the selected directory changes.
- A separate small-screen parent and relative child page exist under `manual-tests/local-content/`.
- Address-bar validation passed on 2026-06-30: the local parent loaded in the existing WebView2, consent and reload completed, the relative child received its arguments, and `local-child-ok` returned synchronously.
- Existing `--auto` synchronous modal validation still passes after the local-loading changes.
- Literal Explorer-to-WebView2 drag-and-drop remains a manual verification item because Windows UI automation could not complete the cross-window drag in this environment.

## Phase 13: main-window placement persistence

Scope:

- persist only the main shell window's normal restore bounds and whether it should reopen maximized
- keep placement persistence separate from WebView2, compatibility permission, and child-dialog sizing
- use `%LOCALAPPDATA%\ImprovisedEosl\SyncModalSpike\main-window-placement.json`
- retain the XAML `1100x760` and Windows-selected position when no valid saved placement exists

Windows/WPF contract and assumptions:

- WPF keeps the pre-minimize/pre-maximize rectangle in `Window.RestoreBounds`; this is the rectangle to persist rather than the minimized or maximized frame bounds
- a minimized process exit must never cause the next launch to start minimized
- the last observed non-minimized state determines minimized-exit behavior: minimized from maximized reopens maximized, while minimized from normal reopens normal
- maximized state is restored only after applying valid normal bounds, so a later Restore command has a useful rectangle
- Edge does not publish a stable application-level JSON window-placement contract; the MVP intentionally follows the documented Windows/WPF window-state model rather than depending on Chromium profile internals
- snap/arranged state is not represented by WPF `WindowState`; it is persisted as its normal restore rectangle and reopens as a normal window
- placement is UI preference data, not a security boundary; malformed, non-finite, undersized, oversized, or unsupported-version data is ignored and logged
- a saved rectangle must intersect the current virtual desktop by a usable minimum area; otherwise Windows chooses the default startup position and the XAML default size is retained
- coordinates and sizes are stored as WPF device-independent units. Mixed-DPI fidelity after moving between monitors remains a manual validation item rather than an Edge-parity claim

Test plan:

- unit-test versioned JSON load/save, malformed data, finite/range validation, and minimized-state normalization
- manually verify normal, maximized, minimized-from-normal, minimized-from-maximized, moved/removed-monitor, and display-scale changes
- keep all automatic WebView2 modes independent of persisted placement so test runs do not mutate the normal user's preference

Implementation gate:

- this phase is shell-only and does not alter the already validated synchronous `showModalDialog` synchronization model

Status:

- Versioned, bounded, atomic placement persistence is implemented in `ImprovisedEosl.Core`.
- The WPF shell restores only sufficiently visible bounds and tracks the last non-minimized state.
- Normal, maximized, minimized-from-normal, and minimized-from-maximized policy paths have automated tests.
- The solution builds with zero warnings and the 47 policy tests pass.
- Manual validation on 2026-07-01 passed normal bounds restoration, maximized restoration,
  Restore-to-normal bounds, minimized-from-normal taskbar close, minimized-from-maximized
  taskbar close, and removed-external-monitor fallback.
- Mixed-DPI display-scale changes remain pending and are not a completion blocker for this phase.

## Phase 14: application and browser-command icons

Scope:

- add a multi-resolution Windows application icon to the executable and main window
- add icons to every browser-shell command button: back, forward, reload, navigate,
  compatibility settings, and diagnostics
- preserve visible text for compatibility settings and diagnostics because those actions are
  application-specific and less universally recognizable than browser navigation
- use icon-only navigation buttons only with bounded hit targets, tooltips, and automation names
- keep the compatibility status text visible; status icons must not replace the textual state
- keep this shell-only work separate from WebView2 and compatibility behavior

Asset and WPF constraints:

- Cowork's selected Style C artwork is design input, not a directly consumable WPF resource
- WPF does not natively render SVG through `Image`, so toolbar artwork is translated into
  individual XAML `Geometry` resources rather than adding an SVG rendering dependency
- the executable icon is generated as a multi-image `.ico`; the source wordmark must remain
  recognizable at 16, 20, 24, 32, 48, 64, 128, and 256 pixels
- icon colors use the shell resource palette and disabled buttons rely on WPF opacity/state
  treatment rather than separate baked bitmap variants
- no icon changes the command, origin policy, host-object exposure, or modal synchronization model

Acceptance checks:

- build the complete solution with zero warnings and errors
- run policy tests and the existing synchronous WebView2 automatic smoke
- manually verify the title bar, taskbar, executable, enabled/disabled navigation states,
  tooltips, keyboard focus, and 100%/high-DPI rendering

Status:

- Style C has been translated into dependency-free XAML geometry resources for all six shell
  commands while preserving text on compatibility settings and diagnostics.
- A reproducible generator creates an application ICO containing 16, 20, 24, 32, 40, 48, 64,
  128, and 256 pixel images; the built executable exposes the embedded icon through Win32.
- The solution builds with zero warnings and errors, `git diff --check` passes, and all 47 policy
  tests pass.
- After the initially observed and previously documented Runtime process churn subsided, the
  existing WebView2 `--auto` smoke completed all three synchronous dialog cycles successfully.
- Shell inspection on 2026-07-01 passed for the title-bar icon, all six command icons, disabled
  navigation appearance, labeled application-specific actions, and crisp rendering at the
  current display scale. UI Automation exposes names for all six buttons; the localized
  compatibility-settings and diagnostics names stay synchronized with their tooltips.

## Phase 15: `window.open()` legacy window features

Scope:

- treat this as an explicit post-MVP extension; it does not broaden the completed synchronous
  `window.showModalDialog()` MVP claim
- preserve asynchronous, modeless `window.open()` and its returned `WindowProxy`
- measure only the requested features: `resizable`, `scrollbars`, `location`, `menubar`,
  `toolbar`, `status`, `fullscreen`, and `channelmode`
- keep popup lifecycle and feature policy separate from `ImprovisedEosl.ModalDialog`
- log unsupported or indistinguishable behavior instead of claiming compatibility

Contradictions and missing assumptions:

- The earlier README claim that every IE-specific `window.open()` feature was unsupported became
  stale after the measured subset was implemented; the README now describes the supported,
  approximated, and unsupported groups explicitly.
- the existing `DialogFeatureParser` is specific to semicolon-separated `showModalDialog`
  features and must not be reused for the comma-separated `window.open()` contract.
- This phase does not define omitted defaults, Boolean aliases, duplicate precedence, named-window
  reuse, `_blank`, popup-blocking policy, or interactions between `location` and `toolbar`.
- `width`, `height`, `left`, and `top` are needed to make measurements repeatable, but are not
  silently added to the requested compatibility scope.
- `fullscreen` and `channelmode` must not remove a reliable close path or bypass screen bounds.

WebView2 constraints:

- use `CoreWebView2.NewWindowRequested`; do not use the synchronous modal host object
- a supplied `NewWindow` must use the opener's `CoreWebView2Environment` and profile and must not
  be navigated before assignment
- take a deferral while initializing the target WebView2 and complete scripts/settings that must
  affect initial content before assigning `NewWindow`
- WebView2 exposes size, position, menu bar, scroll bars, status, and toolbar hints through
  `CoreWebView2WindowFeatures`; honoring any hint remains a host decision
- WebView2 does not separately expose `resizable`, `location`, `fullscreen`, or `channelmode`, so
  those values require either documented approximation or carefully bounded raw-feature capture
- WebView2's embedded popup blocker is disabled; use `IsUserInitiated` and an explicit host policy
- use a modeless WPF window without `ShowDialog()`, a nested dispatcher frame, or parent blocking
- retain normal navigation and browser security behavior and apply finite, screen-aware bounds

Measurement gate:

1. Run `pages/window-open-reference-ie.html` in Edge IE mode using
   `docs/window-open-feature-reference-checklist.md`.
2. Record omitted, `yes`, and `no` behavior for every requested feature plus the documented
   combination cases. Record modeless parent usability, `WindowProxy`, named-window reuse,
   resize, chrome, scrolling, bounds, focus, and close behavior.
3. Add a WebView2 observation harness that logs `NewWindowRequested` URI, name,
   `IsUserInitiated`, all `WindowFeatures` values, and native WPF bounds without applying them.
4. Produce a mapping table marking every requested feature `supported`, `approximated`,
   `unsupported`, or `indistinguishable`.
5. Do not implement feature application until the Edge IE mode evidence and mapping table are
   committed and reviewed.

Status:

- the dedicated Edge IE mode parent/child measurement pages and checklist are added
- user-assisted Edge IE mode measurement completed on 2026-07-04: every isolated popup returned
  a usable `WindowProxy` and left the parent modeless; named-window reuse succeeded
- `scrollbars` and `status` produced distinguishable `yes`/`no` behavior; `resizable`, `location`,
  `menubar`, `toolbar`, `fullscreen`, and `channelmode` did not honor their isolated Boolean values
  as literal chrome/window-state controls
- the combined `all-yes` case opened as a tab in the existing Edge window rather than as a native
  popup, so combination behavior must not be inferred by independently composing feature hints
- the IE-mode harness exposed return/close limitations for several `no` cases and for the
  `all-yes` tab; missing bounds are explicitly marked as manual observations in the checklist
- a dedicated, local-test-only `NewWindowRequested` observation handler now logs the sanitized
  request identity, user-initiation state, every exposed `CoreWebView2WindowFeatures` value, and
  default WPF bounds while deliberately applying none of the hints
- observation children use the opener's environment/profile, initialize under a deferral, remain
  modeless, and accept only the same-origin dedicated child path
- `--window-open-observation` and `--window-open-observation-auto` provide isolated-UDF manual and
  repeatable smoke entry points without changing normal application behavior
- solution build and all 47 policy tests pass; runtime capture remains pending because the local
  WebView2 GPU/browser processes repeatedly crashed before parent navigation on 2026-07-04, also
  after a Windows restart with an isolated UDF and `--disable-gpu`; Microsoft Edge Update then
  repaired/upgraded the Runtime from `149.0.4022.98` to `150.0.4078.48`, but the failure persisted;
  DISM completed successfully and SFC found no integrity violations, yet another restart did not
  change the result
- launching the same observation mode from a normal user PowerShell outside the Codex process
  environment succeeded on Runtime `150.0.4078.48`; the omitted request logged position `120,80`,
  size `640x480`, all four exposed display hints `false`, and default unmodified WPF bounds
  `182,182 720x520`, isolating the prior crashes to the Codex-launched process environment
- the serial observation run completed all 21 cases: every request exposed the fixed position and
  size and navigated successfully; all four display hints were false for every isolated case and
  `all-no`, while `all-yes` alone exposed all four as true
- the mapping table now marks `resizable`, `menubar`, and `toolbar` as approximated; `location`,
  `fullscreen`, and `channelmode` as unsupported; and isolated `scrollbars` and `status` as
  indistinguishable without bounded raw-feature capture
- broad feature application remains gated on review of this mapping and a decision whether to add
  bounded raw-feature capture for the two indistinguishable Boolean pairs
- Decision: include `scrollbars` and `status`. Capture their raw comma-separated values
  synchronously immediately before native `window.open`, limit input to 4 KiB and 64 entries,
  accept bare/`yes`/`true`/`1`/`on` and `no`/`false`/`0`/`off`, and let the last recognized
  duplicate win. Do not retain or log the raw string; correlate only the two parsed nullable
  Booleans with the following `NewWindowRequested` event.
- The follow-up 21-case runtime capture passed: every raw capture matched its request, isolated
  `yes`/`no`, combined `all-yes`/`all-no`, and omitted values were all distinguished correctly.
  Display application now requires an explicit origin-permission model; it must not silently
  reuse the existing `window.showModalDialog` grant.
- Permission decision: on first known legacy-feature detection, offer allow all currently known
  features, allow a checkbox-selected subset, or deny all. Persist both grants and denials per
  origin so repeated calls do not reprompt; local-file loopback decisions remain session-only.
  "Allow all" is version-bounded to the listed known feature set and never pre-authorizes future
  compatibility APIs. The settings UI lists both allowed and denied decisions and can clear either.
- `scrollbars` and `status` application is bounded to explicitly permitted origins and same-origin
  HTTP(S) children. Valid raw values override the indistinguishable WebView2 hints; omitted or
  rejected capture falls back to the hint. `scrollbars=no` injects an initial-document overflow
  policy, while `status=yes` adds a host-owned status area that keeps the child origin visible.
- Omitted or invalid `scrollbars` defaults to visible/scrollable regardless of the unreliable
  WebView2 hint so content is not made unreachable. Only a valid explicit raw `scrollbars=no`
  suppresses scrolling and hides the rendered scrollbar.
- The first suppression attempt left a visible scrollbar and snapped attempted movement back to
  the origin; that behavior was rejected. The revised path removes position-reset logic, uses
  `Emulation.setScrollbarsHidden` before initial navigation for native scrollbar visibility, and
  combines it with initial-document overflow and input suppression. Failure to apply the DevTools
  command is logged as initialization failure rather than pretending `no` was honored.
- Before an origin decision exists, a feature-bearing `window.open` call is detected but does not
  invoke native `window.open`; it returns `null`, presents consent, and requires the page operation
  to be retried after any required reload. This prevents an unconfigured child from opening behind
  the consent window and matches the existing fail-closed `showModalDialog` detection flow.

Completion:

- This phase was implemented and manually validated on 2026-07-04.
- The final solution build passed, all 50 policy tests passed, and the self-contained package
  completed the existing synchronous modal `--auto` smoke with exit code 0.
- `status=yes/no`, `scrollbars=yes/no`, omitted scrollbar safety, Japanese three-way consent, and
  persisted allow/deny decisions passed manual validation.
- Release `v0.1.6-mvp` was produced by the tag workflow and its Windows ZIP asset was verified on
  GitHub. The local release and verification artifacts were removed after publication.
- The next unrelated issue should begin in a fresh context. This phase involved long-running IE and
  WebView2 measurements, Runtime repair, an agent-launch-only WebView2 failure, repeated manual
  gates, merge, release, and cleanup; carrying that history into a new issue would increase stale
  assumption risk.

## Phase 16: initial URL and portable user settings

Objective:

- let a user choose the normal-startup URL from the application UI
- retain the built-in local home page when the setting is absent or invalid
- export and import the user-managed initial URL together with user compatibility decisions
- accept a settings JSON file by drag-and-drop inside the settings window

Scope and authority decision:

- Treat the settings UI and portable file as **user-managed application settings**, not as an
  enforceable administrator policy. A setting editable and importable by the current user cannot
  also be claimed as an OS-enforced administrator restriction.
- Keep `config/compatibility-profiles.json` as trusted, deployment-authored configuration. Import
  must never add, replace, or remove configured profile grants, profile IDs, or profile start URLs.
- Keep user allow/deny decisions logically separate from configured grants. The portable export
  may contain user decisions, but importing it updates only user-owned state.
- If centrally enforced policy is required later, design a separate read-only policy source with
  documented file ownership/ACLs, precedence, and signature or deployment assumptions. Do not
  silently promote this user settings file into that security role.

Contradictions and missing assumptions resolved for the first implementation:

- Existing `CompatibilityProfile.StartUrl` is already used when `--profile` selects a trusted
  profile; it is not the normal-startup default covered by this phase.
- Startup precedence is: automatic validation target, explicitly selected `--profile`, valid
  user initial URL, then the built-in local `home.html`. A profile's `startUrl` does not overwrite
  the user's saved normal-startup URL.
- A user initial URL is an absolute HTTP(S) URL without user information. It does not grant any
  compatibility API and may point to an origin with no compatibility decision.
- Missing, empty, malformed, unsupported-scheme, or unsafe initial URLs fall back to `home.html`
  and produce a bounded diagnostic. Normal startup must not fail closed merely because this
  preference is invalid.
- Import is replacement of the user-managed initial URL and user allow/deny collections after a
  preview/confirmation, not an implicit merge. This makes stale decisions visible and avoids
  ambiguous conflict rules.
- The main browser window keeps its existing `.html`/`.htm` drop behavior. Settings JSON is
  accepted only by the settings window, preventing a `.json` drop from being confused with local
  content navigation.

Storage and schema boundary:

- Store the normal-startup preference under
  `%LOCALAPPDATA%/ImprovisedEosl/SyncModalSpike/browser-settings.json` using versioned JSON and
  atomic replacement.
- Keep `user-approved-compatibility.json` as the runtime source of user compatibility decisions
  for the first implementation. Export/import uses a versioned portable envelope containing the
  initial URL, approvals, and denials; successful import writes both stores through validated
  application services.
- Limit an imported file to 1 MiB UTF-8 and JSON depth 32. Reject unknown properties, unsupported
  versions, invalid origins, unknown APIs, duplicate/conflicting allow/deny entries, and files
  containing configured-profile fields.
- Never execute, navigate to, or persist partially validated imported data. Parse and validate the
  complete document first, show a bounded summary, then replace user state only after explicit
  confirmation.
- Export through an explicit Save dialog. Do not include cookies, WebView2 profile data, local
  paths, diagnostics, credentials, or configured profiles. An exported URL may contain a query or
  fragment and must therefore be treated as potentially sensitive user data.

UI boundary:

- Use one unified user-settings window for the initial URL and compatibility decisions so the UI
  matches the portable JSON boundary. Trusted configured profiles remain outside this window.
- Provide an initial-URL field, a clear/use-home action, user compatibility decision summary,
  Export, Import, Save, and Cancel.
- Accept one `.json` file through an Import action or drag-and-drop over this window only. Reject
  multiple files and other extensions before reading them.
- Stage edits until Save. A validation or persistence failure leaves the active runtime settings
  unchanged and displays a specific error.
- Applying a new initial URL affects the next normal launch; it does not unexpectedly navigate the
  current page. Imported compatibility decisions may update current permission status, but do not
  auto-reload or navigate without a separate explicit action.

Implementation slices:

1. Add pure Core models and stores for the normal-startup preference and portable settings
   envelope, including bounded parse, validation, atomic save, and startup precedence policy.
2. Add policy tests for valid/missing/corrupt settings, URL safety, precedence, import replacement,
   grant/denial conflicts, configured-profile isolation, file/depth limits, and atomic-failure
   behavior.
3. Add the WPF application-settings window and keep UI orchestration separate from Core parsing
   and persistence.
4. Add file-picker export/import and settings-window-only JSON drag-and-drop with preview and
   confirmation.
5. Add an automatic startup mode that proves saved initial-URL precedence without depending on
   normal user state. Add a manual UI checklist for Save/Cancel, import/export round trip, invalid
   fallback, D&D rejection, and current-page non-navigation.

Exit criteria:

- A valid saved user initial URL opens on the next normal launch.
- Missing or invalid user settings visibly/logically fall back to the built-in home page.
- Explicit `--profile` selection continues to take precedence and all automatic modes remain
  isolated from normal user settings.
- Export/import round-trips the user initial URL and user allow/deny decisions without changing
  trusted configured profiles.
- Invalid or conflicting imports change neither in-memory nor persisted settings.
- JSON drag-and-drop works only in the settings window; existing local-HTML drag-and-drop remains
  unchanged.
- Pure policy tests, the solution build, and existing automatic compatibility tests pass. Manual
  WebView2/UI validation is run from a normal user PowerShell on request, not from the agent
  process environment.

Status:

- The scope is confirmed as user-managed settings without administrator permission enforcement.
- Core now has a versioned, bounded, atomically replaced browser-settings store for an optional
  HTTP(S) initial URL. Invalid persisted values produce a diagnostic and an empty preference so
  the application layer can fall back to the built-in home page.
- Startup navigation precedence is implemented as pure policy: automatic validation, explicitly
  selected profile, user initial URL, then built-in home.
- Four policy checks cover persistence/clearing, invalid-file fallback, unsafe URL rejection,
  and complete startup precedence.
- Normal WPF startup now loads `browser-settings.json` and applies the documented precedence.
  Automatic modes use an empty in-memory setting and never read normal user state; invalid files
  log a bounded warning and fall back to home.
- A unified settings window now edits, validates, saves, and clears the initial URL and lists or
  revokes user compatibility decisions.
  Save affects the next normal launch and does not navigate the current page. Invalid values keep
  the window open; Cancel and persistence failures do not change active settings.
- A versioned portable user-settings envelope contains only the initial URL and user allow/deny
  decisions. It rejects configured-profile fields, unknown properties, invalid origins/APIs,
  duplicates, allow/deny conflicts, oversized files, invalid URLs, and excessive JSON depth.
- The unified settings window can export staged values, import by picker or its visibly labeled
  JSON drop target, show a bounded replacement summary, and defer persistence until Save. The drop
  target changes color for accepted/rejected drags. The main browser window retains its HTML-only
  drop behavior.
- Three additional policy checks cover portable round-trip, allow/deny conflict rejection, and
  configured-field isolation. The Core/test build has zero warnings and all 57 policy checks pass;
  the full Release solution build passes with only the existing NU1900 advisory-source warning.
- A manual checklist is recorded in `docs/application-settings-manual-test.md`. Portable
  import/export, unified decision management, and the visible D&D target passed normal-user
  PowerShell validation on 2026-07-04.
- `--browser-settings-auto` creates an isolated temporary settings file, proves that its persisted
  initial URL wins over home without reading or changing normal user state, and removes the fixture
  on exit. Normal-user PowerShell validation passed on 2026-07-04 with exit code 0; the log recorded
  `source=user-settings`, exact-target success, and fixture cleanup.

Completion:

- Acceptance is complete: initial URL persistence and home fallback, unified settings UI,
  user allow/deny management, portable import/export, visible settings-only D&D, configured-profile
  isolation, invalid-input rejection, startup precedence, and current-page non-navigation are
  implemented and validated.
- Normal-user PowerShell validation passed on 2026-07-04 for toolbar layout, save without current
  navigation, saved-URL restart, home restoration, and invalid-URL rejection while keeping the
  settings window open.

## Phase 17: top-level `window.close()` handoff

Target legacy workflow:

1. A dummy launch page opens the real business page with `window.open()` so the business window
   uses popup-style chrome.
2. After the child has opened successfully, the dummy page calls `window.close()` on itself.
3. The opened business window remains alive as an independent top-level browser window.
4. The promoted business window must retain origin-gated `showModalDialog()`, compatible
   `window.open()` behavior, shared session state, diagnostics, process-failure handling, and an
   explicit user close path.

Corrected browser facts:

- Modern HTML does not reject every `window.close()` call. Script-created auxiliary windows are
  normally script-closable; other top-level windows are restricted by the current HTML rules and
  browser policy.
- Internet Explorer did not silently close every top-level window either. Closing a window that
  was not opened by script could show a confirmation prompt.
- WebView2 reports content close requests through `CoreWebView2.WindowCloseRequested`; the host
  decides whether closing the related native window is appropriate.

Current implementation gap:

- `showModalDialog()` children already override `window.close()` and return `window.returnValue`;
  that child-dialog behavior remains complete and is not the missing top-level-close scope.
- The main WebView does not currently subscribe to `WindowCloseRequested` or expose an approved
  top-level close bridge.
- Modeless `window.open()` windows are currently created as WPF owned windows (`Owner = this`).
  Windows destroys an owned popup when its owner is destroyed, so directly closing `MainWindow`
  would also close every intended business window.
- `NewWindowObservationWindow` currently hosts only a WebView plus optional status area. It does
  not install the full compatibility broker, nested modal host, test-safe lifecycle, or parent
  recovery behavior needed by a promoted business window.
- Therefore this behavior cannot be implemented safely as a one-line `MainWindow.Close()` handler.

Security and compatibility boundary:

- Treat top-level close as a separate compatibility API decision. Permission for
  `showModalDialog()` or window features must not silently grant permission to close a native
  application window.
- Validate the actual current document origin at the native boundary; JavaScript-provided origin
  values remain claims only.
- An unapproved close attempt is detected and offered through the existing per-origin consent UI.
  Denial is persisted and suppresses repeated prompts.
- Do not allow an iframe or cross-origin child document to close the wrapper through a top-level
  host object call.
- Preserve a reliable native close affordance and a visible origin indicator on the promoted
  business window. Legacy requests to hide browser chrome do not remove these trust boundaries.

Pending-first-child handoff model:

- The MVP targets dummy launchers that call `window.open()` and then immediately request their own
  `window.close()`. It does not implement general browser popup promotion.
- The first eligible direct-child request becomes a pending handoff. `window.open()` receives a
  real `about:blank` staging window so its synchronous return shape remains usable, but the business
  document is not executed there. If no parent close follows in the same task, the candidate is
  released on the next timer turn and the staging window navigates normally.
- When the current top-level document requests close, suppress native window closure, set the
  retained parent browsing context's `window.name`, apply the captured popup chrome/size, and
  navigate that same WebView2 to the pending child URL exactly once.
- Discard later direct-child requests during the same pending handoff and log them. The MVP may
  close staging children, but must not select or preserve multiple successors.
- Retaining the parent WebView2 preserves its already validated synchronous compatibility broker,
  process recovery, session/profile, diagnostics, and native close affordance.
- The handoff supports same-origin HTTP(S) GET navigation, target name, bounded feature capture,
  and shared session state. It does not preserve a child JavaScript heap, real `WindowProxy`,
  `window.opener`, POST bodies, `document.write()` into `about:blank`, or opener-to-child DOM access.
- An application that uses the return value from `window.open()` before closing the dummy is
  outside this MVP and must be logged as unsupported rather than approximated silently.

Lifecycle feasibility gate:

1. Capture exactly one eligible same-origin direct child without navigating a second WebView2.
2. Observe the parent `WindowCloseRequested`, require the pending handoff, and suppress actual WPF
   closure.
3. Apply the pending name, feature-derived shell, and size before navigating the retained parent
   WebView2 to the child URL.
4. Prove the business page executes once, advances its DOM timer, and completes a synchronous
   `showModalDialog()` round trip through the original parent broker.
5. Prove missing, cross-origin, malformed, and additional child requests fail closed and are logged.
6. Keep normal `window.open()` observation behavior unchanged outside the dedicated gate.

Rejected peer-window experiment:

- A WPF popup with the same environment/profile survived opener closure and its DOM continued, so
  native peer lifetime itself was viable.
- In a WebView2 adopted through `CoreWebView2NewWindowRequestedEventArgs.NewWindow`, both host-object
  paths failed before and after opener closure. Synchronous lookup failed at the object name with
  `remoteObjectId: 0`; asynchronous lookup remained unresolved until timeout. The host methods were
  never entered.
- Registering before assignment, immediately after assignment, after initial navigation, and after
  navigation under a never-before-used name did not change the result. Strongly retaining the COM
  object also did not help. This rules out ordinary .NET collection, name reuse, and registration
  timing as the MVP cause.
- The adopted-popup approach is therefore rejected for this milestone. The measurements remain in
  `docs/top-level-close-poc.md`; diagnostic-only popup bridge code must not ship as the solution.

Status:

- `--top-level-close-auto` and dedicated local dummy/business pages now implement the
  pending-first-child handoff gate.
- Normal-user PowerShell validation passed on 2026-07-05 with exit code `0`: the first child was
  captured without navigation, the retained parent adopted the requested name/720x520 shell,
  navigated once, advanced its DOM timer, and returned `selectedId=616` through the original
  synchronous modal broker.
- Normal popup ownership and normal top-level close behavior remain unchanged.
- Build and all 60 policy checks pass, including same-origin first-child selection, additional-child
  rejection, and close-time origin continuity.
- Production wiring treats `window.close handoff` as an independent user compatibility decision.
  An undecided origin stages only `about:blank`, suppresses parent close, and opens the existing
  consent UI; permission for modal or window features does not silently grant handoff.
- Normal-user `--top-level-close-manual` validation passed on 2026-07-05: no business document was
  shown before consent, `AllowAll` reloaded the launcher, the retained parent completed the handoff,
  and `Open modal` synchronously returned `selectedId=617`.
- `--top-level-close-popup-auto` is the regression gate for an ordinary `window.open()` with no
  parent close. It requires staging release, navigation in the same child, DOM progress, and clean
  last-window shutdown. Normal-user validation passed on 2026-07-05 with exit code `0` and five
  observed business DOM ticks.

Exit criteria:

- The approved dummy close request is converted into one navigation of the retained parent
  WebView2 to the first eligible direct-child URL.
- The business page remains interactive, receives captured name/chrome/size, shares the session,
  and retains synchronous `showModalDialog()` compatibility through the original broker.
- The business URL executes once; no adopted business popup is loaded and replayed.
- Close requests without permission, from the wrong origin, or without an eligible popup fail
  closed and are logged without terminating the application.
- Closing the final promoted root exits cleanly without orphaned WebView2 processes.

## Phase 18: public repository baseline

Goal: publish the completed experimental MVP from a reviewable, privacy-safe `main` baseline
without carrying the private development history into the public repository.

Decisions:

- License project-authored source and documentation under MIT with
  `Copyright (c) 2026 kneco`.
- Keep the source repository private and separate from the new public repository.
- Before reusing the public repository name, update every source-worktree remote to its retained
  private location so a later push cannot follow an old-name redirect into the public repository.
- Create the public repository from the sanitized current tree as one clean root commit on
  `main`; do not copy private branches, tags, releases, issues, or unreachable Git objects.
- Keep third-party components under their own licenses. Include the Microsoft WebView2 package
  license and notices in both the source tree and generated distribution ZIP.
- Replace machine-specific user paths in public documentation with portable commands.
- Add concise contribution and security-reporting guidance before changing visibility.
- Re-run policy tests, a Release build, secret/path scans, and distribution-content inspection on
  the clean public candidate. WebView2 UI modes remain a normal-user PowerShell gate.

Public release gate:

1. The public candidate contains `LICENSE`, `THIRD-PARTY-NOTICES.md`, `SECURITY.md`, and
   `CONTRIBUTING.md`.
2. README commands target `main` and current release `v0.1.7-mvp`.
3. No personal absolute paths, credentials, private keys, generated artifacts, or private-history
   references are present in the tracked tree.
4. The distribution ZIP contains the project license and applicable third-party notices.
5. Immediately before external changes, re-verify both clean worktrees and the public candidate's
   one-commit history.
6. Preserve the private source repository, including its releases, issues, tags, branches, and
   history, and update its worktree remote before creating the public repository. Do not copy or
   expose that repository as part of publication.
7. Create `kneco/improvised-eosl` as private, push only the sanitized
   public candidate's `main`, and verify its default branch and one-root-commit history before
   changing the replacement repository to public.
8. After changing visibility, verify the public tree and README links independently. Do not copy
   private releases, issues, tags, branches, or other history into the public repository.

## Phase 19: compact compatibility status display

Goal: recover browser content height while preserving a persistent textual compatibility state,
the exact origin/API detail, and accessible operation.

Design decision:

- Use an icon plus a short localized state label adjacent to the address field.
- Interpret the existing requirement that icons must not replace textual state as requiring a
  persistent human-readable state label, not the current full diagnostic sentence on its own row.
- Keep exact normalized origin and enabled API names in complete accessible text and an on-demand
  detail surface that does not depend on pointer hover.
- Do not place the indicator inside the editable address field in this phase; custom focus,
  hit-testing, and screen-reader behavior would add risk without changing compatibility behavior.
- Keep the indicator as a view of native policy. It must not grant, deny, revoke, or otherwise
  become a security boundary.
- Add structured status/API data to the Core presentation result before changing layout. The WPF
  shell must not parse English diagnostic labels to determine state or iconography.
- Preserve explicit per-API denials in that presentation result. The current English status label
  collapses an ordinary denied origin and an untouched origin to `off`, which does not meet Issue
  #2's requirement to keep denied, undecided, and detected states distinct.
- Preserve existing consent and settings flows and keep visual work separate from compatibility
  execution.

Comparison and detailed acceptance criteria are recorded in
`docs/compatibility-status-display-comparison.md`.

Implementation gate:

1. Add automated coverage for structured untouched, detected/pending, enabled-per-API,
   enabled-multiple, denied-per-API, mixed allow/deny, and opaque states and for Allow/Deny/Revoke
   transitions.
2. Add WPF mapping checks for localized short text, state-specific icon, and complete accessible
   text without diagnostic-label parsing.
3. Replace the full-width row only after manual navigation, redirect, permission, revocation,
   keyboard, screen-reader, high-contrast, theme, narrow-window, and 100%/150%/200% DPI checks pass.
4. Treat initialization and browser recovery as operational states distinct from compatibility
   allow/deny policy.

Status:

- The option comparison is complete and option C, icon plus persistent short text next to the
  address field, is selected for implementation.
- Core now reports structured `Undecided`, `DetectionPending`, `Enabled`, `Denied`, and `Blocked`
  presentation states with separate enabled, denied, and detected API lists. Existing English
  diagnostic labels and `DisplayText` remain unchanged.
- The structured-state checks cover untouched, pending detection, explicit denial, mixed
  allow/deny, multiple enabled APIs, decision clearing, and opaque-origin blocking.
- The shell's pure presentation mapper now selects a short Japanese label and semantic icon kind
  for every structured state and builds complete accessible/detail text containing the normalized
  origin plus enabled, denied, and detected API lists. It does not parse the diagnostic label.
- The Windows-targeted test executable directly references the spike to validate this mapper
  without creating WPF controls.
- The former full-width status row is replaced by a focusable compact status button next to the
  address field. The button retains short text, uses state-specific geometry, exposes complete
  Automation and tooltip text, and opens the same detail through keyboard or pointer activation.
- The navigation controls keep the address field and Go adjacent; the compatibility status follows
  Go so origin-related trust information remains nearby without splitting the navigation action.
  The corrected order passed normal-user visual confirmation on 2026-07-07.
- The first UI inspection rejected a native MessageBox because it clipped the complete detail.
  The replacement owned WPF detail window wraps the origin/API text, bounds its height with a
  vertical scrollbar, and provides default and cancel Close behavior.
- Initialization, recovery, and recovery failure use separate operational presentations and do
  not claim an Allow or Deny policy result.
- All 63 automated checks pass, the complete solution builds with zero warnings and errors, the
  existing synchronous WebView2 `--auto` smoke exits with code `0`, and `git diff --check` passes.
- Normal-user checks pass for basic layout, keyboard/detail operation, allow, deny, revoke, and
  origin-change transitions. Screen-reader, theme, high-contrast, multiple-resolution, and
  100%/150%/200% DPI checks are deferred under
  `docs/compatibility-status-display-manual-test.md`.

## Phase 20: main title bar page title

Goal: reflect the current document title in the native WPF title bar without changing navigation,
permission, or compatibility execution behavior.

Design decision:

- Treat public Issue #12 as a browser-shell UI polish item, separate from compatibility policy.
- Use WebView2's `DocumentTitleChanged` event for the parent WebView.
- Keep the application identity in the title bar by formatting non-empty document titles as
  `{document title} - Improvised EOSL`.
- Fall back to `Improvised EOSL` when WebView2 has no document title or reports only whitespace.
- Reset to the fallback title when navigation starts so the previous page title is not shown as a
  stale title while the next document is loading.
- Re-read WebView2's `DocumentTitle` after successful navigation completion as a fallback in case
  the title change event is not enough to update the native title bar on every runtime.
- Do not duplicate the application title when the document itself is titled `Improvised EOSL`.
- Do not infer trust, origin, or compatibility state from the document title; the compatibility
  status indicator remains the trust/compatibility display.

Implementation gate:

1. Add a pure formatting test for empty, whitespace, and non-empty document titles.
2. Manually confirm that the Release application title changes after navigating to the built-in
   test page and at least one ordinary HTTP(S) page.

## Phase 21: find in page

Goal: support Issue #11's `Ctrl+F` page search without building a custom search engine or changing
the compatibility boundary.

Design decision:

- Use WebView2's built-in Find UI and browser accelerator behavior.
- Keep `CoreWebView2Settings.AreBrowserAcceleratorKeysEnabled` enabled; WebView2 documents
  `Ctrl+F` and `F3` as browser accelerator keys for Find on Page, and the default is enabled.
- Add a WPF `Ctrl+F` shortcut only to cover focus that is currently in the wrapper chrome, such as
  the address field. The shortcut focuses the parent WebView2 and starts WebView2's Find session
  with the default Find dialog visible.
- Do not inject JavaScript search code, expose a new host object, or treat search as a
  compatibility permission.
- Manual validation is recorded in `docs/find-in-page-manual-test.md`.

Implementation gate:

1. Add pure shortcut-recognition coverage for `Ctrl+F`.
2. Confirm `Ctrl+F` opens WebView2 Find UI when focus is in web content and when focus is in the
   address field.
3. Confirm ordinary navigation and compatibility permission/status behavior are unchanged.

## Phase 22: IE onhelp syntax validation fixture

Goal: make Issue #16 validation possible from the built-in home page without asking testers to
hand-create an HTML file.

Design decision:

- Add `pages/onhelp-return-false.html` as a source manual-test fixture containing
  `<body onhelp="return false">`.
- Link the fixture from `home.html` so normal testers can open it without typing a file URL or
  copying HTML by hand.
- Treat the fixture as validation of current WebView2/wrapper tolerance. It does not implement
  IE `onhelp` event emulation, F1 interception, or writable keyboard-event compatibility.
- Keep `event.keyCode = 0` and related IE event-object mutation in Issue #17.

Implementation gate:

1. The fixture page loads and shows `Status: loaded` and `Error: none`.
2. The fixture button updates the page to prove inline event handlers still run.
3. Pressing F1 does not break the app and matches the current Improvised EOSL baseline.
4. Ordinary navigation, compatibility status, and `Ctrl+F` find remain unchanged.

## Phase 23: IE keyboard event mutation research gate

Goal: determine whether a narrowly scoped compatibility feature for IE-era keyboard event
mutation is justified and safe without expanding Improvised EOSL into general IE DOM emulation.

Research decision:

- Treat writable `IHTMLEventObj.keyCode` as genuine IE-era behavior, but do not assume that every
  `event.keyCode = 0` assignment cancelled the same browser action. The assignment's effect on
  later handlers, default actions, and host accelerators requires reference measurement.
- Treat this as a potentially high-value post-MVP compatibility feature. Deferral is a sequencing
  and evidence decision, not a conclusion that the feature lacks product value.
- Do not implement a shim until a real target-page pattern or a controlled reference fixture
  demonstrates a WebView2-visible incompatibility and the intended IE behavior.
- Keep Issue #17 separate from Issue #16. F1/`onhelp` host behavior does not establish general
  keyboard-event mutation semantics.
- Record the detailed feasibility, security boundary, rejected scope, and measurement matrix in
  `docs/ie-keyboard-event-mutation-research.md`.

Security and permission boundary:

- Any future behavior-changing shim is a new compatibility API permission, scoped to an exact
  normalized HTTP(S) origin. Existing approval for `showModalDialog`, window features, top-level
  close handoff, or `onhelp` validation must not enable it.
- The first candidate scope is the approved top-level document only. All child frames and opaque
  origins fail closed; frame support requires a separate origin-validation design.
- Enabling or revoking the feature requires reload so document-created instrumentation has an
  unambiguous lifetime.
- Do not record typed characters, key values, or target-field contents in diagnostics. Log only
  activation, blocked use, and bounded compatibility outcome categories.
- Do not add ActiveX/COM, a per-keystroke host-object/native bridge, arbitrary script rewriting,
  or browser security exceptions.

Measurement gate before any implementation proposal:

1. Capture the smallest real handler pattern that fails, including event type, phase, target,
   assignment, subsequent reads, propagation controls, and expected default action.
2. Compare that pattern in the agreed IE reference environment and current WebView2, recording
   script-visible values separately from final browser behavior.
3. Measure `window.event`, `returnValue`, `cancelBubble`, `keypress`, `charCode`, `which`, and
   `keyIdentifier` independently; do not bundle them into one compatibility claim.
4. Test whether per-event JavaScript instrumentation can observe the assignment and map only the
   proven cancellation case to `preventDefault()` or `stopPropagation()` without synthetic event
   redispatch or native key interception.
5. Verify exact-origin consent, reload/revocation, top-level-only behavior, navigation, input-field
   handling, browser accelerators, and diagnostics that contain no key data.
6. Decide from the evidence whether to reject the feature, retain a measurement fixture only, or
   propose a separately approved bounded shim.

Status:

- Research and design are documented; no implementation is authorized or present.
- A bounded JavaScript shim remains a feasibility hypothesis, not an accepted compatibility
  contract. Generic writable `KeyboardEvent` parity is rejected.

## Phase 24: brown visual identity redesign

Goal: make the wrapper visually recognizable as Improvised EOSL in normal desktop use without
implying that it is Microsoft Edge, Internet Explorer, WebView2, or a Microsoft-supported
compatibility product.

Design decision:

- Treat Issue #5 as shell visual identity work, not compatibility behavior work.
- Replace the blue command palette with a brown palette: dark brown as the primary command/icon
  color and pale warm brown as the hover/base surface.
- Replace the blue `IE`-style application icon with a distinct Improvised EOSL mark that remains
  recognizable in the title bar, taskbar, Alt+Tab, executable, and high-DPI views.
- Tint the native Windows frame through DWM attributes where supported so the standard title bar,
  resize, Snap, system menu, and accessibility behavior remain OS-owned. Do not replace the frame
  with custom chrome in this phase.
- Skip frame tinting when Windows high contrast is active. On unsupported Windows versions or DWM
  failure, leave the native frame unchanged and continue normally.
- Keep command icons as dependency-free XAML geometry resources. Do not introduce an SVG renderer
  or a UI icon library for this narrow WPF shell.
- Make the address navigation icon visually distinct from the Forward browser-history icon. Forward
  remains a simple right arrow; address navigation uses a page/enter-style mark.
- Preserve visible text on application-specific commands and the compatibility status control.
  Compatibility status meaning must remain available through icon geometry, short text, tooltip,
  and UI Automation text, not color alone.
- Keep the visual palette separate from origin policy, host-object exposure, WebView2 settings,
  consent, diagnostics, and modal synchronization.

Implementation gate:

1. Update the reproducible icon source and regenerate the tracked multi-resolution `.ico`.
2. Confirm the `.ico` still contains 16, 20, 24, 32, 40, 48, 64, 128, and 256 pixel images.
3. Build the solution and run the policy test executable.
4. Publish a Release package to verify the executable embeds the updated icon.
5. Manually inspect title-bar, taskbar, Alt+Tab, executable, enabled/disabled navigation states,
   command icon distinction, tooltips, keyboard focus, light/dark theme, high contrast, and
   100%/150%/200% display scale where available.

Status:

- Implemented in the WPF shell as a visual-only change. The command palette now uses the brown
  shell resource colors, and the address navigation icon is visually distinct from the Forward
  history icon.
- The application icon generator is dependency-free Python standard library code and regenerates a
  tracked multi-resolution `.ico` containing 16, 20, 24, 32, 40, 48, 64, 128, and 256 pixel images.
- The tracked icon no longer uses the former blue `IE` wordmark. It uses a brown Improvised EOSL
  mark that was verified from the generated ICO and from the published executable's embedded icon.
- Build, policy tests, `git diff --check`, Release package publishing, distribution layout
  validation, and packaged `--auto` smoke passed locally.
- Native frame tinting is applied through `DwmSetWindowAttribute` for supported Windows 11
  environments and is skipped for high contrast or unsupported DWM attributes. This keeps standard
  OS window behavior instead of implementing custom chrome.
- Manual title-bar, taskbar, Alt+Tab, Windows theme, high-contrast, and 100%/150%/200% display-scale
  checks remain to be run from a normal user session using `docs/visual-redesign-manual-test.md`.

## Phase 25: JSON-only administrator browser shell policy

Goal: design Issue #3 as a post-MVP administrator/operations feature that can hide selected wrapper
browser controls without expanding the user Settings UI or weakening WebView2/browser security.

Design decision:

- Treat shell policy as trusted JSON configuration, not a user-managed preference. The existing
  application Settings window remains limited to normal-startup URL and user compatibility
  decisions.
- Use a default executable-relative source, `config/browser-shell-policy.json`, plus an explicit
  `--shell-policy <path>` override. Missing policy means standard visible shell.
- Keep policy validation fail-safe: invalid JSON, unsupported versions, unknown properties,
  oversized files, and impossible command combinations fall back to the standard visible shell and
  log a warning.
- Allow `primaryToolbar:hidden` to hide the full wrapper toolbar, including Back, Forward, Reload,
  address entry, Go, Settings, Diagnostics, compatibility status, and current-origin controls. This
  matches line-of-business operation where browser-like escape paths are intentionally suppressed.
- Treat full-toolbar hidden mode as an operational tradeoff, not a trust UI or security boundary.
  Origin and compatibility status are not visible while the toolbar is hidden; recovery is through
  command-line policy replacement or a known-good `--shell-policy` path.
- Keep the native Windows close affordance visible and OS-owned. Do not implement custom kiosk
  chrome in this phase.
- Keep compatibility profiles, user-approved compatibility decisions, startup profile selection,
  local-content loading, diagnostics logging, WebView2 storage, and modal synchronization separate
  from shell presentation.
- Do not disable WebView2 security, Chromium sandboxing, or browser accelerator keys as part of
  this first shell-policy phase. Navigation accelerator suppression is tracked separately in
  Phase 26 because `Ctrl+F` intentionally relies on WebView2 find/browser accelerator behavior.
- Support offline command-line operations only: `--export-shell-policy <path>`,
  `--apply-shell-policy <source> --shell-policy <target>`, and `--reset-user-settings`. These
  commands should exit before WebView2 starts and must not elevate permissions or bypass operating
  system ACLs.
- Define `--reset-user-settings` narrowly: reset only user-managed initial URL and user
  compatibility allow/deny decisions. Do not reset shell policy, compatibility profiles, WebView2
  user data, cookies, local storage, release assets, or package configuration.

Rejected scope:

- a general-user Settings toggle for restricted shell mode;
- in-app policy editing;
- including shell policy in portable user settings import/export;
- hiding native close;
- kiosk, lockdown, DLP, or enterprise deployment guarantees;
- origin allow-list navigation control;
- arbitrary script rewriting, host object expansion, ActiveX/COM, or native bridges; and
- treating legacy `window.open` chrome hints as the administrator shell-policy model.

Known constraints:

- Hiding wrapper navigation controls does not stop page script navigation, redirects, clicked links,
  or all WebView2/browser accelerators.
- Toolbar command visibility and host/browser accelerator suppression are separate policy layers.
  Hiding Back, Forward, or Reload does not imply keyboard suppression, and keyboard suppression
  must not silently change the visible shell.
- The existing top-level close handoff path can hide the full toolbar as a legacy chrome
  approximation. The administrator shell policy may produce the same presentation, but must remain
  a separate process-level policy rather than a `window.open` compatibility side effect.
- Policy-file write protection is an operating-system deployment responsibility. The application
  can validate and log the policy source, but it cannot make a writable file administrator-only.

Implementation gate:

1. Add `docs/browser-shell-policy.md` as the version 1 contract before implementation.
2. Add a pure Core parser/store and command-line parser with tests before WPF mutation.
3. Add template export, policy apply, and reset-user-settings command-line modes that exit before
   WebView2 initialization.
4. Apply WPF shell visibility through a presentation model that can hide the complete primary
   toolbar and does not parse localized UI text.
5. Use `docs/browser-shell-policy-manual-test.md` to validate standard mode, restricted mode,
   invalid-policy fail-safe, CLI export/apply/reset, ordinary navigation, compatibility consent,
   `Ctrl+F`, diagnostics, and native close.

Status:

- Research/design documented in `docs/browser-shell-policy.md` and the future manual validation
  gate is recorded in `docs/browser-shell-policy-manual-test.md`; no implementation is authorized
  or present in this phase.

## Phase 26: targeted navigation accelerator suppression policy

Goal: design Issue #24 as a docs-only gate for administrator suppression of Back, Forward, and
Reload host/browser accelerators without changing compatibility permission, origin navigation
policy, or WebView2 security settings.

Design decision:

- Extend the JSON administrator shell-policy contract with a separate `navigationAccelerators`
  section instead of overloading toolbar command visibility.
- Preserve `Ctrl+F` and `F3` find-in-page. Do not set
  `CoreWebView2Settings.AreBrowserAcceleratorKeysEnabled = false` globally because that disables
  browser accelerators beyond Back, Forward, and Reload.
- Use `CoreWebView2Controller.AcceleratorKeyPressed` as the design point for targeted handling.
  The implementation must decide per key whether to set `IsBrowserAcceleratorKeyEnabled = false`
  so web content can still receive the key, or `Handled = true` so the key is stopped at the host.
- Treat the initial target command group as Back, Forward, and Reload only. Measure or explicitly
  reject Ctrl+R, F5, Alt+Left, Alt+Right, browser Back/Forward keys, and Backspace-driven history
  behavior before claiming coverage.
- Keep this as workflow guidance, not kiosk/security enforcement. Page script navigation,
  redirects, clicked links, `location` assignment, form submission, and origin allow-list control
  remain outside this phase.
- Log unsupported or unrecognized accelerator requests instead of pretending the browser action is
  suppressed.
- Record the detailed WebView2/WPF evidence, candidate key matrix, and implementation options in
  `docs/navigation-accelerator-research.md`.

Rejected scope:

- disabling WebView2 security, site isolation, Chromium sandboxing, or all browser accelerators;
- suppressing `Ctrl+F`/`F3` find-in-page, text editing, page movement, print, zoom, DevTools, or
  arbitrary page-defined shortcuts;
- keylogging, per-keystroke diagnostics, page keyboard-event mutation, or IE DOM keyboard
  emulation;
- origin allow-list navigation control, kiosk lockdown, DLP, enterprise deployment guarantees, or
  native close suppression; and
- implementing Issue #24 before the key matrix and WebView2 event semantics are validated.

Implementation gate:

1. Keep `docs/browser-shell-policy.md` as the source of the JSON contract and update it before
   code changes.
2. Use `docs/navigation-accelerator-research.md` to measure the baseline key matrix before
   selecting `IsBrowserAcceleratorKeyEnabled`, `Handled`, or unsupported behavior.
3. Add pure policy tests that distinguish `historyCommands:hidden`,
   `reloadCommand:hidden`, `navigationAccelerators.historyCommands:suppressed`, and
   `navigationAccelerators.reloadCommand:suppressed`.
4. Add a WebView2-focused manual test matrix for standard mode, toolbar-hidden-only mode,
   accelerator-suppressed-only mode, combined hidden/suppressed mode, `Ctrl+F`/`F3` preservation,
   and unsupported-key logging.
5. Implement the host event handling only after deciding the `IsBrowserAcceleratorKeyEnabled`
   versus `Handled` behavior for each targeted key.
6. Verify from a normal user PowerShell. Agent-launched WebView2 behavior is not authoritative
   when it conflicts with a normal user run.

Status:

- Docs-only design gate added for Issue #24. `docs/navigation-accelerator-research.md` now records
  the official WebView2 constraints, current WPF source observations, key matrix, design options,
  and implementation gate. No implementation is authorized or present in this phase.
- Baseline measurement fixture added as
  `src/ImprovisedEosl.Spike.SyncModal/pages/navigation-accelerator-reference.html` with direct
  manual startup through `--navigation-accelerator-manual`. The fixture does not change production
  policy, WebView2 settings, or accelerator handling. Manual run instructions are recorded in
  `docs/navigation-accelerator-manual-test.md`.
