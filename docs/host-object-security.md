# Host object security boundary

## Context

The parent WebView2 receives a minimal synchronous compatibility broker early enough to support removed-API discovery and synchronous `showModalDialog`. JavaScript-provided origin strings are untrusted because a page can call the broker directly and supply any string.

## Authoritative origin

The host treats the current `ParentWebView.Source` as the authoritative document URL.

For compatibility methods:

1. Normalize the JavaScript-claimed origin as `scheme://host:effective-port`.
2. Normalize the current parent WebView2 source origin.
3. Reject the call unless claimed and actual origins match.
4. Apply the per-origin, per-API compatibility policy to the actual origin.

An approved origin string supplied by a different current page does not grant access.

## Method categories

### Compatibility methods

The globally registered `compatibilityBroker` exposes only:

- `IsShowModalDialogAllowed`
- `DetectLegacyApi`
- `ShowDialog`

These methods require claimed-origin/current-document agreement. `ShowDialog` also requires the actual origin to be approved for `window.showModalDialog`.

### Test-only methods

The separate `testProbe` exposes:

- `Ping`
- `LogEvent`
- `FinishAutoRun`
- `GetMaxJsonPayloadBytes`
- `GetMaxDialogFeatureBytes`

Top-level WebView2 host objects do not provide the frame API's origin-list registration method. The app therefore registers `testProbe` when `NavigationStarting` targets the exact process-local HTTP test origin and removes it before navigation to any other origin. `FinishAutoRun` additionally requires an explicit automatic-test command-line mode. The host-side actual-origin guard remains as defense in depth.

General websites receive `compatibilityBroker` but not `testProbe`, so they cannot call test methods to block the UI, write arbitrary page logs, or close the application.

Child WebView2 documents receive the same minimal compatibility broker so approved legacy
origins can open nested dialogs. Each nested broker derives authority from that child
WebView2's current `Source`; it does not inherit the caller's claimed origin. An unapproved
child receives discovery-only behavior, logs the attempt, and cannot display consent UI from
the child STA. The user must navigate that origin at the top level, approve it, and reload.

## Automated checks

- Unit tests cover claimed/actual origin agreement, explicit ports, unsupported schemes, and local-test-origin matching.
- `--origin-guard-auto` directly calls `ShowDialog` with a spoofed claimed origin.
- The expected result is JavaScript `undefined`, one mismatch-block log, and zero child STA starts.
- All normal auto modes must complete with zero mismatch or test-method blocks.
- `--nested-auto` verifies successful same-origin nesting, the four-level depth limit, and denial after a child moves to an unapproved origin.
- `--process-failure-auto` injects a renderer crash through native test configuration. No page-callable host method exposes CDP or process termination.
- All fifteen automatic modes verify that `testProbe` is registered once for the local test origin and remains usable by the test harness, including after parent WebView2 recreation.

## Remaining boundary work

- Child dialog target URLs are limited to bounded HTTP(S) URLs and validated before child STA creation. See `docs/dialog-navigation-security.md`.
- Compatibility and test methods are split into separate host-object types. A future project split may move those types out of the WPF window class without changing their exposed surfaces.
