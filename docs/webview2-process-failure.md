# WebView2 process failure behavior

## Microsoft recovery contract

Microsoft documents `CoreWebView2.ProcessFailed` as the diagnostic and recovery event for WebView2 process failures:

- `RenderProcessExited`: the main-frame renderer is replaced by an error page; the host may reload or recreate the WebView.
- `BrowserProcessExited`: the WebView is closed and the host must recreate it.
- `RenderProcessUnresponsive`: the event may repeat while the renderer remains unresponsive; the host decides whether to wait, reload, or close.
- GPU and utility process exits are normally recreated automatically and do not require WebView recreation.

Sources:

- https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/process-related-events
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2processfailedkind

## MVP dialog policy

| Failure kind | Modal child behavior |
| --- | --- |
| `RenderProcessExited` | Return structured `child-process-failure`, close and dispose the child WebView, and resume the blocked caller. |
| `BrowserProcessExited` | Return a structured child failure, release the blocked caller, wait for the environment exit notification, then recreate the parent WebView2 control and navigate back to its last URL. |
| `RenderProcessUnresponsive` | Start an asynchronous responsiveness probe on the first notification. Reset the count if it completes; otherwise close the child after a five-second grace period. A second notification before the grace period ends also closes the child. |
| GPU, utility, frame-only, sandbox, plugin, or unknown exit | Log details and allow WebView2's documented automatic or scoped recovery behavior. |

Exit results include `processFailedKind`, `reason`, and `exitCode`. Unresponsive results include
the notification count and the action that ended the grace period. A process failure is never
reported as a successful dialog return or as cancellation.

## Automatic validation

Run:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --process-failure-auto
```

This mode uses WebView2's CDP call API and the experimental `Page.crash` test command to crash only the child renderer after navigation. The crash switch is carried by native test configuration; it is not exposed through a page-callable host method.

The 2026-06-28 run produced:

- failure kind `RenderProcessExited`
- reason `Crashed`
- a structured `child-process-failure` return
- synchronous parent resumption after about 1.2 seconds, before the 10-second test timeout
- child WebView disposal and normal automatic-test process exit

For coordinated browser-process recovery, run:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --browser-process-failure-auto
```

This test-only mode obtains the shared browser PID from the child `CoreWebView2` and terminates
that process after child navigation. It is native test configuration and is not exposed to web
content. The 2026-06-28 run verified this order:

1. Child and parent received `BrowserProcessExited / Unexpected`.
2. The child returned a structured `child-process-failure` and restored its native owner.
3. The blocked parent host call returned.
4. The parent waited for `CoreWebView2Environment.BrowserProcessExited` (`Failed`).
5. The closed parent control was disposed and replaced with a newly initialized WebView2.
6. The replacement navigated to the previous URL and completed the automatic probe.

The environment and control events are deliberately coordinated because Microsoft does not
guarantee their ordering. The implementation creates a new control rather than attempting to
reuse the closed control, matching Microsoft's recovery guidance.

For renderer-unresponsive handling, run:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --unresponsive-auto
```

This mode starts an infinite loop in the child renderer and sends one native test click to the
child. The input is necessary because WebView2 defines this event in terms of unresponsiveness
to user input; an infinite loop without input did not raise the event within the 60-second test
window on the current Runtime. The test input path is native-only and is not exposed to web
content.

The 2026-06-28 run observed `RenderProcessUnresponsive / Unresponsive` about 16 seconds after
the click. The responsiveness probe did not complete, so the five-second grace period closed
the child and returned `child-process-failure` after about 22.5 seconds total. Native owner
state was restored before the parent resumed.

The parent received the same unresponsive event after the blocked host call returned. This is
evidence that same-origin parent and child WebViews can be affected by the same renderer
process despite using separate STA threads. Closing the child allowed the parent responsiveness
probe to complete, so its count reset and no parent reload was needed. If the parent remains
unresponsive for five seconds, the shell restarts the shared browser process and uses the
existing browser-exit recovery path to recreate the parent control. A second notification can
trigger the same recovery before the grace period expires.

For a parent-only persistent hang, run:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --parent-unresponsive-auto
```

The first experiment requested a normal `Reload()` after the failed probe. The hung renderer
did not navigate, and another unresponsive notification arrived about 30 seconds later. The
MVP therefore treats browser-process restart as the last-resort parent recovery action instead
of claiming that reload is reliable. That failed experiment also left the old host process
unable to complete normal shutdown while the renderer remained hung, locking its executable;
the corrected in-process browser restart avoids leaving the renderer in that state.

The corrected 2026-06-28 run terminated the browser process after the five-second grace period,
observed `BrowserProcessExited`, recreated the parent WebView2, restored its prior URL, and
successfully called the test Host Object from the recovered page. Persistent profile data is
retained through the shared UDF; renderer-owned in-memory page state is lost.

Observed event latency was not deterministic: runs produced the first unresponsive event after
about 16 seconds and 56 seconds, while another run produced no event within 120 seconds despite
the same native click. `--parent-unresponsive-auto` therefore has a test-only 45-second watchdog
that invokes the same browser-restart path when the Runtime notification has not arrived. The
watchdog is not enabled in normal browsing and must not be mistaken for an application-wide
page-execution timeout.

## Remaining work

- Recovery restores the last URL, cookies, and persistent profile data, but cannot reconstruct
  in-memory DOM state, unsaved form input, or the JavaScript stack that was lost with the renderer.
- Multiple simultaneously running wrapper processes sharing one UDF have not been recovery-tested.
- CDP `Page.crash` is experimental and is only a test mechanism, not an application dependency.
