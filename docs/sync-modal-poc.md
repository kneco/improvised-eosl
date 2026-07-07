# Sync modal PoC

## Branch

`main`

The original synchronization spike has evolved into the current MVP branch. The executable
retains its `Spike.SyncModal` project name until the post-MVP project-structure migration.

## Goal

Validate the technical hypothesis:

> Parent JavaScript calls a synchronous WebView2 host object and remains blocked while a child WebView2 on a separate STA thread remains interactive, then the child result is returned synchronously to the original parent call.

## Current environment status

The recorded validation used .NET SDK 8.0.422. Commands in this document assume `dotnet` is on
`PATH`; otherwise substitute the absolute path to the local `dotnet` executable.

The first `dotnet restore` may need to create first-run files in the current user profile.

## Project

The executable PoC source lives at:

```text
src/ImprovisedEosl.Spike.SyncModal/
```

Pure policy code used by the executable has been extracted into:

```text
src/ImprovisedEosl.Core/
src/ImprovisedEosl.ModalDialog/
```

The policy test project references these libraries directly. WPF, WebView2, host-object,
and child-STA lifecycle code intentionally remains in the executable spike.

It uses:

- WPF
- `net8.0-windows`
- Microsoft Edge WebView2
- one parent WebView2
- one synchronous COM-visible host object
- an early `window.showModalDialog` shim injected by WebView2
- a minimal local HTTP test server bound to `127.0.0.1` on an ephemeral port
- one child WPF window on a dedicated STA thread
- one child WebView2

## Deliberate constraints

- The original synchronization proof had no browser shell or compatibility profiles. The current spike added both only after the synchronization gate passed.
- Feature parsing and the measured MVP WPF window policy were added after the synchronization gate passed.
- No IE compatibility beyond the explicitly documented `showModalDialog` surface.
- No parent JavaScript callback while the synchronous host call is blocked.
- No nested modal loop on the parent WebView2 UI thread.
- The parent test page now calls `window.showModalDialog(...)`; it does not call the host object directly for dialog execution.
- Session sharing is tested for cookies and `localStorage`. `sessionStorage` is displayed for visibility but is not assumed to share across separate top-level windows.

## Manual validation steps

After .NET SDK and WebView2 Runtime are available:

```powershell
dotnet restore
dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj
```

Then:

1. Click `Block for 3 seconds`.
2. Confirm the parent page tick stops while `Ping` is blocked and resumes after return.
3. Click `Open child dialog`.
4. Confirm the parent page does not update while the child is open.
5. Confirm the child page tick continues.
6. Type in the child input.
7. Click `Return value and close`.
8. Confirm the parent receives a JSON string synchronously.
9. Repeat the child dialog cycle several times.
10. Click `Open session dialog`.
11. Confirm the child sees the parent cookie and `localStorage` values.
12. Click `Return value and close`.
13. Confirm the parent sees the child-updated cookie and `localStorage` values after the synchronous return.
14. Click `Open Google` to demonstrate ordinary browsing to a non-compatibility site.
15. Click `Open missing dialog` to verify navigation failure logging.
16. Click `Open timeout dialog` to verify timeout logging and a non-success return value.

Permission-revocation regression:

Passed in a normal user session on 2026-07-07: after allowing `window.showModalDialog`, revoking
the decision in Settings, and invoking it again on the same loaded page, no child dialog opened
and the compatibility consent prompt appeared again.

1. On the discovery test page, allow `window.showModalDialog` and complete the reload.
2. Without navigating away, open Settings, revoke that origin/API decision, and save.
3. Invoke `window.showModalDialog` again on the already loaded page.
4. Confirm that no child dialog opens and the compatibility consent prompt appears again.
5. Confirm that choosing Allow reloads the page and restores the supported synchronous call.

Manual result logging:

- Parent call start and return events are written to `sync-modal-poc.log`.
- Child focus, input, accept, and cancel events are written through `chrome.webview.postMessage`.
- The returned value includes the child input text and child DOM timer tick count.

## Automatic validation

When direct desktop interaction is unavailable, run:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --auto
```

The app writes a log to:

```text
src/ImprovisedEosl.Spike.SyncModal/bin/Debug/net8.0-windows/artifacts/sync-modal-poc.log
```

The automatic probe opens the child dialog three times from synchronous parent host-object calls. Each child page runs a DOM timer for 2.5 seconds, closes itself, returns the tick count, and the parent closes the app after the final synchronous call returns.

The local test pages use the `testProbe` host object. Production compatibility methods use the separate `compatibilityBroker`; non-test top-level navigations do not receive `testProbe`.

The already-loaded-document revocation regression has a separate automatic mode:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --revoked-permission-auto
```

It loads the local parent with a runtime grant, removes that grant without navigation, calls the
already-installed `window.showModalDialog` function, and exits successfully only when the call
returns to low-privilege discovery without opening a child dialog.

Parser tests:

```powershell
dotnet run --project tests/ImprovisedEosl.Spike.Tests/ImprovisedEosl.Spike.Tests.csproj
```

The parser tests cover size fields, position fields, booleans, timeout clamping, `:` and `=` separators, unsupported fields, and malformed values.

Latest policy-test result:

- `dotnet run --no-build --project tests/ImprovisedEosl.Spike.Tests/ImprovisedEosl.Spike.Tests.csproj`
- The current policy suite contains 63 passing checks. Earlier release notes retain their
  historical counts.

Failure-mode automatic validation:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --failure-auto
```

The failure probe opens a missing dialog URL and a dialog that intentionally stays open until the host timeout fires.

Nested-dialog automatic validation:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --nested-auto
```

This probe verifies parent-to-child-to-grandchild synchronous return propagation, the four-open-dialog depth limit, and denial when a child document's actual origin is not approved.

Child renderer failure validation:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --process-failure-auto
```

This probe crashes the child renderer through a native test-only CDP call and verifies a prompt structured failure return, child disposal, and parent synchronous resumption before the dialog timeout.

Native-close cancellation validation:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --native-close-auto
```

This probe closes the child through WPF `Window.Close()` without a JavaScript close message. It verifies that the blocked parent call resumes synchronously with JavaScript `undefined`, matching the native path used by the title-bar X.

Literal title-bar X UI validation:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --native-x-ui
```

This mode opens the same passive child without scheduling a native close. Click the child window's title-bar X. The parent validates the returned JavaScript value and exits with code 0 only when it is `undefined`.

The same mode can be used to inspect modality. While the child is open, the parent HWND must be disabled. The diagnostic log records `ownerEnabledNow=False`; child close or timeout must later record `ownerEnabledNow=True`.

Configured-profile automatic validation:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --profile-auto
```

This probe validates that configured origin permission, without a user or runtime allowance,
enables the same synchronous modal path and returns the child result.

Startup-profile automatic validation:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --startup-profile-auto --profile=automatic-configured-origin
```

This probe resolves the configured profile through the normal command-line parser and verifies
that the parent WebView2 starts at the profile's `startUrl`.

## Latest automatic result

Date: 2026-06-24

Command:

```powershell
dotnet run --no-build --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --auto
```

Result:

- Build passed with 0 warnings and 0 errors.
- Automatic run exited with code 0.
- Parent host-object call entered on thread 1.
- Child WebView2 instances ran on STA threads 4, 5, and 6.
- Each child returned after about 2.6 seconds.
- Each child reported `ticks: 10`, showing that child DOM timers ran while the parent synchronous call was blocked.
- Three repeated open/close cycles completed without deadlock.

The original remaining-validation list is complete. Current release verification is tracked in
`docs/mvp-readiness.md`.

## Latest HTTP session-sharing result

Date: 2026-06-24

Command:

```powershell
dotnet run --no-build --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --session-auto
```

Result:

- Parent and child pages loaded from `http://127.0.0.1:<ephemeral-port>/`.
- Child saw the parent cookie: `ieCompatCookie=parent-auto`.
- Child saw the parent `localStorage`: `parent-auto`.
- Child did not see parent `sessionStorage`; this is expected for separate top-level windows and is not an MVP sharing requirement.
- Child updated cookie and `localStorage`.
- Parent saw the child-updated cookie immediately after synchronous return.
- Parent saw the child-updated `localStorage` on a delayed read after the parent event loop resumed.

Design note:

- Do not use `localStorage` as the synchronous return channel. Return values must come from `window.returnValue` through the native dialog result.
- Treat cookie sharing as immediate enough for this PoC.
- Treat `localStorage` sharing as session-backed but not guaranteed to be visible to the blocked parent at the exact synchronous return boundary.

## Latest failure-mode result

Date: 2026-06-24

Command:

```powershell
dotnet run --no-build --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --failure-auto
```

Result:

- Missing child URL returned `{"kind":"navigation-failure","ok":false,"webErrorStatus":"Unknown","httpStatusCode":404}`.
- Timeout child URL returned `{"kind":"timeout","ok":false}`.
- Timeout child window was closed after the timeout result was returned.
- Both failures were logged distinctly from successful compatibility behavior.

## Latest manual result

Date: 2026-06-24

User-guided manual checks passed with the parent page calling `window.showModalDialog(...)`, not the host object directly:

- `Ping` blocked and returned.
- `Open child dialog` opened a separate child WebView2 while the parent call was blocked.
- Child focus and input events were received on the child STA thread.
- Typed child input was returned to the parent as part of the synchronous result.
- Cancel closed the child and returned `undefined`.

Representative log details:

- Parent logged `parent window.showModalDialog call starting` and `parent window.showModalDialog call returned`.
- Accept path returned `{"accepted":true,"selectedId":123,"text":"change change change","ticks":88}`.
- Cancel path returned `undefined`.

## Pass criteria

- Parent JavaScript after `ShowDialog` does not run until child close.
- Child WebView2 remains keyboard- and mouse-interactive.
- Child DOM timers continue while the parent call is blocked.
- Child return value reaches the parent synchronously.
- Repeated open and close cycles do not deadlock.
- Logs show parent, host, and child STA thread IDs.

## Fail criteria

- Child window or child WebView2 hangs during parent sync wait.
- Child requires parent WebView2 callbacks to complete.
- Parent JavaScript is called from native code while the sync host call is outstanding.
- Any nested modal loop on the parent WebView2 UI thread is required.
- The parent only works after rewriting caller code to async.
