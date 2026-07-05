# Top-level `window.close()` pending-first-child PoC

## Purpose

Validate the revised top-level-close lifecycle gate without changing normal browsing behavior:

```text
dummy top-level -> window.open(business request) -> request held pending
                  dummy window.close() -> retained parent navigates once to business
                  -> DOM timer progresses -> synchronous modal passes -> final window closes
```

The business document runs in the original parent WebView2 and therefore retains its existing
origin-gated compatibility bridge. A real `about:blank` staging popup preserves the synchronous
`window.open()` return, but no adopted popup executes the business URL. If the parent does not close
in the same task, the candidate is released and that staging popup navigates normally.

## Rejected peer-window evidence

The first design assigned a newly initialized WebView2 to `NewWindowRequested.NewWindow`. Its WPF
window survived opener closure, but its host-object connection did not:

- synchronous object lookup failed with `remoteObjectId: 0`
- asynchronous lookup remained unresolved for ten seconds
- neither the full broker nor a minimal `Ping(string)` method was entered
- registration before assignment, after assignment, after navigation, and after navigation with a
  fresh name all produced the same boundary

The result is treated as a WebView2 constraint for this PoC, not worked around by weakening browser
security or keeping an invisible dummy browser alive.

## Automatic mode

Run from a normal user PowerShell:

```powershell
dotnet run --no-build --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --top-level-close-auto
$LASTEXITCODE
```

Expected behavior:

- the dummy launcher appears briefly and its first same-origin child request is held pending
- its close request is suppressed rather than closing the native window
- the existing window adopts the captured size/chrome and navigates once to the business URL
- the business DOM timer advances
- the harness invokes a test function that opens an automatic `showModalDialog()`
- the modal returns `accepted=true` and `selectedId=616` synchronously to the retained window
- the retained window then closes, the application exits, and `$LASTEXITCODE` is `0`

Relevant success log:

```text
top-level close auto-run passed: retained parent completed pending first-child handoff
```

The normal-user PowerShell gate passed on 2026-07-05 with `$LASTEXITCODE` equal to `0`. The log
recorded the pending child before navigation, applied the 720x520 shell with browser commands
hidden, advanced the business DOM timer, and returned modal result `616` through the retained
parent broker.

## Manual production-consent mode

Run without automatic grants:

```powershell
dotnet run --no-build --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --top-level-close-manual
```

Expected first-run behavior:

- no business document is loaded before consent
- the compatibility prompt lists `window.showModalDialog`, `window.open features`, and the separate
  `window.close handoff` decision
- choosing `すべて許可` reloads the dummy once
- the same native parent window changes to the business page with browser commands hidden
- `Open modal` returns a result containing `selectedId: 617`

This manual production-consent gate passed on 2026-07-05. The log recorded staging cancellation
before consent, `AllowAll`, one retained-parent navigation, and synchronous modal result `617`.

## Normal popup regression mode

To prove staging does not consume an ordinary popup when the parent remains open:

```powershell
dotnet run --no-build --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj -- --top-level-close-popup-auto
$LASTEXITCODE
```

The child first receives `about:blank`; on the next timer turn the handoff candidate is released and
the same child navigates to the business page. The gate requires its DOM timer to advance, closes
both windows, and exits with code `0`.

This regression gate passed in normal-user PowerShell on 2026-07-05 with exit code `0`; the host
observed successful non-blank navigation and five business DOM ticks.

## Boundaries

- The mode grants only the fixed local test origin at runtime.
- A pure policy accepts only the first same-origin HTTP(S) child and revalidates the captured parent
  origin when `window.close()` is received.
- Normal popup ownership and normal top-level close behavior remain unchanged.
- This proves one same-origin immediate launcher handoff only.
- A real `WindowProxy`, POST body, child JavaScript heap, opener DOM access, multiple successors, and
  nested opener graphs are unsupported in this milestone.
- Permission UX and production close-policy integration remain pending.
