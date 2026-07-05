# Technical feasibility

## Scope

This document evaluates the MVP hypothesis:

> Can JavaScript in the parent WebView2 synchronously call a host object, keep that call blocked, let the user operate a child WebView2 on a separate STA thread, and synchronously return the child result to the parent JavaScript call?

No broad implementation should start until this synchronization model has been proven with a minimal proof of concept.

## Source review

Local project documents reviewed:

- `README.md`
- `AGENTS.md`
- `docs/architecture.md`
- `docs/codex-handoff.md`
- `docs/concept.md`
- `docs/implementation-plan.md`
- `docs/mvp-show-modal-dialog.md`
- `docs/risks-and-limitations.md`

Microsoft documentation reviewed:

- [Threading model for WebView2 apps](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/threading-model)
- [CoreWebView2.AddHostObjectToScript](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.addhostobjecttoscript)
- [WebView2 in WPF apps](https://learn.microsoft.com/en-us/microsoft-edge/webview2/platforms/wpf)
- [WebView2 in WinUI 3 apps](https://learn.microsoft.com/en-us/microsoft-edge/webview2/platforms/winui3-windows-app-sdk)
- [Manage user data folders](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/user-data-folder)

## Summary conclusion

The central hypothesis is proven for the experimental MVP in the measured environment. Official
documentation did not guarantee this design; the conclusion rests on the local automatic and
manual validation described below.

Initial local PoC result:

- The WPF/WebView2 MVP on `main` builds and runs.
- The parent synchronous host-object method was entered on thread 1.
- While that call remained blocked, child WebView2 windows on separate STA threads ran DOM timers and returned values.
- Three consecutive automatic dialog cycles completed without deadlock.
- The parent page now calls `window.showModalDialog(...)` through an injected shim.
- HTTP-based cookie and `localStorage` sharing were validated with parent and child pages loaded from `127.0.0.1`.

The synchronization, interaction, session-sharing, cancellation, navigation-failure, timeout,
process-failure, and origin-gating paths have now been validated. See `docs/mvp-readiness.md` for
the acceptance decision and residual risks.

The documentation supports these building blocks:

- WebView2 must run on an STA UI thread with a message pump.
- All callbacks and calls into a WebView2 instance must occur on that instance's creating UI thread.
- Synchronous host object proxies exist and can block JavaScript while native host code runs.
- A WebView2 session can be shared by WebView2 controls that use the same user data folder.

The documentation also introduces hard constraints:

- WebView2 does not support reentrant nested message loops inside WebView2 callbacks.
- Blocking a WebView2 UI thread prevents that WebView2 from processing callbacks and async completions.
- While JavaScript is blocked in a synchronous host object call, native code cannot call back into that same JavaScript context.
- Synchronous host object proxies are explicitly described as a reliability risk compared with async proxies.

Therefore, the MVP must prove a narrow model:

1. The parent host object call must not create a modal child by running a nested message loop on the parent WebView2 UI thread.
2. The child WebView2 must be created, initialized, and operated entirely on a separate STA thread with its own message pump.
3. The parent synchronous host method may wait for a native synchronization primitive, but must not call into the parent WebView2 or parent JavaScript while the synchronous call is outstanding.
4. The child must communicate the final value to native code, not by requiring the parent WebView2 to process a callback while blocked.
5. The proof of concept must detect hangs, failed initialization, and close/cancel paths explicitly.

## WebView2 constraints

### STA and thread affinity

Microsoft states that WebView2 is COM-based and must run on an STA thread. A WebView2 must be created on a UI thread with a message pump, callbacks occur on that thread, and calls into that WebView2 must be made on that same thread.

Impact:

- Parent and child WebView2 controls cannot be freely accessed across threads.
- A separate child STA is acceptable only if all child WebView2 work is marshaled to that child STA.
- Shared services must not hold `CoreWebView2` objects and call them from arbitrary worker threads.

### Reentrancy

Microsoft documents that WebView2 callbacks and completion handlers run serially, and that synchronously creating modal UI or nested message loops inside a WebView2 event handler is unsupported. Their example says creating a new WebView2 inside a synchronous modal dialog from a WebView2 message callback can hang.

Impact:

- Do not implement `showModalDialog()` by calling WPF `ShowDialog()` or WinForms `ShowDialog()` on the parent WebView2 UI thread from a WebView2 callback.
- If the synchronous host method is invoked on the parent UI thread, any nested dispatcher frame on that same thread is high risk.
- The first PoC must record exactly which thread the host method runs on and whether the parent WebView2 UI thread remains blocked.

### Blocking

Microsoft warns that WebView2 relies on the UI thread message pump for event callbacks and async completions; blocking that pump prevents WebView2 work from completing.

Impact:

- The parent WebView2 is expected to be blocked while preserving legacy JavaScript semantics.
- During that block, the parent WebView2 should be treated as unavailable for navigation, script execution, async completion, or host callbacks.
- The child must not depend on parent WebView2 async APIs completing during the blocked interval.

### Synchronous host objects

Microsoft documents `window.chrome.webview.hostObjects.sync.{name}` and says calls synchronously block running script while communicating cross-process with host code. The same documentation recommends async proxies because sync proxies can cause reliability issues. It also states that native code cannot call back into JavaScript while JavaScript is blocked on a synchronous native call; such attempts fail with `ERROR_POSSIBLE_DEADLOCK`.

Impact:

- A synchronous return value is technically supported at the host object boundary.
- The host method must not call `ExecuteScriptAsync` or invoke a JavaScript callback in the parent while the parent script is synchronously waiting.
- Return value propagation must be native-to-parent-return-value only.

### Session sharing

Microsoft documents that WebView2 browser data is stored in a user data folder, and WebView2 controls using the same UDF share the same WebView2 session. A UDF can have only one WebView2 session at a time.

Impact:

- Session sharing should be tested by creating parent and child WebView2 controls with the same custom user data folder or shared `CoreWebView2Environment`.
- The PoC must verify cookie or storage visibility between parent and child.
- Multiple independent UDFs would violate the MVP session-sharing goal.
- Current PoC result: child WebView2 sees parent cookie and `localStorage`; parent sees child cookie immediately after return and child `localStorage` after the parent event loop resumes.
- `sessionStorage` is not treated as shared across parent and child top-level windows.

## Central hypothesis assessment

### What appears feasible

- JavaScript can call a synchronous host object and receive a synchronous primitive or serialized result.
- Native code can start or signal another STA thread.
- A child WebView2 can be interactive if its own STA thread and message pump are not blocked.
- The child can return a JSON-compatible value to native code on close.
- The native host method can return that serialized value to the blocked parent JavaScript call.

### What is not guaranteed

- Microsoft documentation does not explicitly bless blocking a synchronous host object call for the lifetime of an interactive child window.
- It is unclear from documentation alone whether the thread serving the parent host object call is the parent UI thread, another COM/RPC thread, or dependent on host object implementation details.
- It is unclear whether a long-running synchronous host object call has watchdogs, COM pumping side effects, focus problems, shutdown issues, or browser-process responsiveness limits.
- Sharing one WebView2 environment or UDF across WebView2 controls on different STA threads must be proven experimentally.

### Required PoC pass/fail criteria

Pass only if all of the following are true:

- Parent JavaScript after `showModalDialog()` does not execute until child close.
- Child WebView2 remains keyboard- and mouse-interactive during the parent wait.
- Child can navigate or at least process DOM events during the parent wait.
- Child can set and return a JSON-compatible value.
- Parent receives the value synchronously as the return value of the original call.
- Parent host code never calls back into parent JavaScript during the blocked call.
- Repeated open/close cycles complete without deadlock.
- Timeout and forced-close paths are logged as failures, not treated as compatibility success.

Current automatic PoC status:

- Passed: parent JavaScript returned only after child close.
- Passed: child DOM timers continued while the parent synchronous call was blocked.
- Passed: JSON-compatible child return values reached the parent synchronously.
- Passed: three repeated open/close cycles completed.
- Passed: real keyboard/mouse interaction in the child window.
- Passed: parent-to-child cookie and `localStorage` sharing over HTTP.
- Passed with timing caveat: child-to-parent `localStorage` visibility after the parent event loop resumes.
- Passed: cancellation returned `undefined` in manual validation.
- Passed: missing child URL returned a structured navigation-failure result.
- Passed: timeout returned a structured timeout result and closed the child window.
- Passed: a child synchronous host call opened an interactive grandchild WebView2 on a third STA, received its value, and returned the nested value to the original parent call without deadlock.
- Passed: nested calls are limited to four open dialog levels; the next call returns a structured `nested-dialog-depth-exceeded` result.
- Passed: approval is re-evaluated against each child document's actual origin. A child navigated from approved `127.0.0.1` to unapproved `localhost` could not open a nested dialog.
- Passed: the calling HWND is assigned as native owner and disabled while each child is open. Close, timeout, renderer failure, and four-level nested unwind restore the previous enabled state.
- Passed for child renderer exit: an induced `RenderProcessExited / Crashed` event returned a structured failure, disposed the child, and resumed the parent synchronously before timeout.
- Passed: an induced shared `BrowserProcessExited` released the blocked child call, restored native modality, waited for the environment exit event, recreated the parent WebView2 control, and navigated back to the previous URL.
- Passed: an induced child renderer hang produced `RenderProcessUnresponsive`; a failed five-second responsiveness probe closed the child, restored native modality, and returned a structured failure before the dialog timeout.
- Observed: the parent received the same unresponsive event after the synchronous call unwound, showing that separate-STA same-origin WebViews may share a renderer process. The parent probe then completed and reset its count without reload.
- Passed: a parent-only persistent renderer hang ignored a normal reload request, but browser-process restart followed by parent WebView2 recreation restored the previous URL and test Host Object operation.
- Observed: `RenderProcessUnresponsive` latency varied from about 16 seconds to no event within 120 seconds after equivalent injected input. The automatic mode has a test-only watchdog; production remains event-driven to avoid treating legitimate long-running legacy scripts as hangs.
- Not yet validated: recovery when multiple wrapper processes share one UDF.

Fail if any of the following occur:

- Child WebView2 hangs or fails to initialize while the parent sync call is blocked.
- Parent WebView2 callbacks are required to finish the child flow.
- Any implementation needs to disable browser security features to work.
- Any implementation depends on nested message loops on the parent WebView2 UI thread.
- Return value propagation requires changing legacy caller code to async.

## Original contradictions and resolutions

### Contradictions

- Resolved: browser-shell polish was deferred until the higher-priority compatibility behavior passed.
- Resolved: session sharing moved ahead of feature expansion and passed for cookies and `localStorage`.
- Resolved: the synchronous host method may block the parent UI thread only while all child
  WebView2 work remains on separate STAs and no parent WebView2 callback is required.

### Resolved assumptions

- The MVP uses .NET 8, WPF, Evergreen WebView2, an unpackaged application model, and a custom UDF.
- The exact minimum Windows and WebView2 Runtime versions remain release-environment metadata,
  not a production support promise.
- Dialog URL, redirect, origin, payload, cancellation, timeout, initialization, and process-failure
  outcomes are implemented and documented at their enforcement points.
- Nested `showModalDialog()` is supported for already-approved child origins by creating each next WebView2 on another STA. The MVP caps the chain at four open dialog levels and does not prompt for new approval from a child window.
- Cross-origin child dialogs, non-HTTP schemes, and local test pages have explicit fail-closed policies.
- Serialization is intentionally a JSON projection and does not preserve prototypes. The MVP now limits payloads to 1 MiB UTF-8 and depth 64, rejects malformed/cyclic/oversized values explicitly, and truncates payload logs. See `docs/json-payload-boundary.md`.
- Focus ownership is application-modal: the calling HWND is disabled and restored through the native owner chain, while Alt+Tab and unrelated applications remain available. Mixed-DPI and broader shell behavior remain unmeasured.

## Technical risks

- Deadlock if parent UI thread is blocked and WebView2 needs that thread to complete the host call.
- Deadlock or failure if native code calls back into parent JavaScript during a sync host call.
- Child WebView2 hang if created through a nested modal loop or on the wrong STA.
- Shared UDF conflicts if parent and child environments are created inconsistently.
- Focus and modality mismatch because a child on another STA is not automatically equivalent to browser-native modal dialog behavior.
- Security exposure from exposing synchronous native methods to arbitrary origins.
- Reliability issues from long-running synchronous host object calls.
- Unsupported IE-era argument or return value types that cannot safely round-trip through JSON.
- Future WebView2 runtime behavior changes because the model relies on a difficult sync-host-object edge case.

## WPF vs WinUI 3 for MVP

### WPF

Advantages:

- Mature desktop UI model with straightforward multi-window and dispatcher-per-STA patterns.
- WebView2 WPF control is directly documented and widely used.
- Lower packaging complexity for a narrow experimental PoC.
- Easier to create isolated child windows on dedicated STA threads.
- Better fit for proving synchronization behavior before investing in modern shell polish.

Disadvantages:

- Standard WPF WebView2 is hosted through `HwndHost` and can have airspace issues where WebView2 obscures overlapping WPF UI.
- Edge-like modern chrome may require more custom work later.

### WinUI 3

Advantages:

- Better long-term fit for modern Windows visual style.
- WebView2 in WinUI 3 supports custom WebView2 environments in current Windows App SDK versions.
- Useful if the product direction prioritizes a modern Windows shell after the MVP.

Disadvantages:

- Windows App SDK and packaging choices add complexity before the synchronization model is proven.
- The WebView2 WinUI 3 documentation calls out platform-specific behavior and custom environment support history.
- Separate STA child-window experimentation is likely to be simpler in WPF.

### Recommendation

Use WPF for the MVP proof of concept.

The MVP is a synchronization and threading experiment, not a visual-shell experiment. WPF minimizes moving parts around window creation, STA dispatchers, and unpackaged local testing. WinUI 3 can be reconsidered after the core model is proven.

## Validated implementation assumptions

- The parent synchronous host object method can block until child close without WebView2 terminating or hanging the host call.
- The child WebView2 can be initialized and used on a separate STA while the parent call is blocked.
- Parent and child can share session data through the same custom user data folder or environment.
- The host can obtain the child return value without calling parent JavaScript during the blocked call.
- The implementation can enforce allowed origins before exposing the compatibility host object.
- A manual test page is acceptable for proving interactive child behavior that unit tests cannot reliably cover.
- A minimal browser shell can navigate ordinary sites without injecting compatibility behavior into non-allowed origins.
- Legacy API discovery can use a low-privilege detector, but the full synchronous host object must remain gated by explicit origin approval.
- For synchronous APIs such as `showModalDialog`, first-call detection may not preserve the original control flow. MVP should prompt, persist approval, and reload before enabling full compatibility.
- Dialog feature strings are compatibility behavior, not UI polish. The measured MVP subset is
  reference-validated; safety clamps and unsupported fields remain explicit approximations.

## Recommended next step

Perform the clean-process release verification in `docs/mvp-readiness.md`, then tag the
experimental MVP or begin the post-MVP project-structure and visual-design work.
