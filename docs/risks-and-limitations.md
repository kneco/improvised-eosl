# Risks and limitations

## Research source discipline

General IE compatibility research is not a substitute for measured behavior. Dialog feature
decisions use the corrected Edge IE mode evidence in this repository. Product support and
lifecycle statements use current Microsoft documentation. See
`docs/ie-compat-research-review.md` for the disposition of the Cowork research files.

## Fundamental limitation

Improvised EOSL does not reproduce Internet Explorer.

It only attempts selected behavioral compatibility on top of Chromium.

## Technical risks

### WebView2 reentrancy

Synchronous host-object calls and nested UI message loops may deadlock or violate WebView2 threading constraints.

The central MVP experiment is to determine whether a child WebView2 on another STA thread remains interactive while the parent JavaScript call is synchronously blocked.

Two-level nesting has also passed: a child synchronous call opened a grandchild on another
STA and propagated its result back through the blocked chain. This remains an unusual runtime
model, so the MVP limits chains to four open dialogs and does not treat the result as a general
guarantee against future WebView2 runtime changes or resource exhaustion.

Induced child renderer and shared browser-process crashes now return a structured failure and
release the blocked caller. Browser-process recovery recreates the parent WebView2 and restores
its last URL, but renderer-owned state such as the JavaScript stack, DOM mutations, and unsaved
form input is inherently lost. Multi-process UDF sharing remains unvalidated. See
`docs/webview2-process-failure.md`.

An induced child renderer hang showed that same-origin parent and child WebViews can receive
the same `RenderProcessUnresponsive` event. Separate STA ownership isolates UI message pumps,
but not necessarily Chromium renderer processes. The child now closes after a failed
five-second responsiveness probe (or a second notification), and the parent independently
probes before taking action. A parent-only persistent hang showed that a normal reload can be
ignored by the stuck renderer; the implemented last resort restarts the shared browser process
and recreates the parent WebView2. This loses renderer-owned in-memory state in every affected
WebView.

`RenderProcessUnresponsive` delivery time was highly variable in automatic testing, including
one run with no notification within 120 seconds after native input. Production recovery remains
event-driven; the finite 45-second fallback exists only in the automatic test mode. A future
product heartbeat would need an explicit compatibility policy because long synchronous scripts
are common in the target applications and must not be killed silently.

Native HWND ownership now prevents interaction with the calling app window while a child is
open and restores nested owners in order. Windows foreground focus restoration remains best
effort, and mixed-DPI, multi-monitor, minimize/restore, and taskbar behavior still need a
broader usability pass. See `docs/modal-window-ownership.md`.

### Session sharing

Authentication state, cookies, storage, certificates, and integrated authentication must behave consistently between parent and child WebViews.

### Serialization mismatch

IE-era applications may pass values that cannot be represented safely as JSON.

### Behavioral mismatch

Even when the API shape is reproduced, subtle ordering, event, focus, or window-lifecycle differences may break the application.

### Security exposure

Restoring previously restricted browser behaviors may increase security risk.

Compatibility features must be opt-in and origin-scoped.

The current host broker validates JavaScript origin claims against the actual parent WebView2 source before applying origin approval. This reduces direct-broker spoofing risk but does not make the experimental wrapper a security product.

Child dialogs may navigate cross-origin over HTTP(S), matching the needs of some legacy workflows. Local-file and executable schemes are blocked, but ordinary web-origin risks still apply to the child content.

JSON payload and feature-string limits are wrapper safety policy, not historical IE behavior. A legacy application exceeding those limits will be rejected and requires an explicit future compatibility decision rather than silent limit removal.

Diagnostic files may contain sensitive test data such as origins, paths, errors, and truncated payloads. The MVP rotates files but does not provide enterprise retention, redaction, export, or access-control tooling.

## Explicitly unsupported areas

- ActiveX
- Browser Helper Objects
- NPAPI
- Trident layout quirks
- proprietary IE DOM behavior
- old TLS and cipher suites
- unsupported authentication mechanisms
- production support guarantees

## Operational disclaimer

This project must not be represented as a safe substitute for modernization.

At most, it may serve as:

- a compatibility experiment
- a temporary test environment
- an impact-analysis tool
- a migration discovery aid
