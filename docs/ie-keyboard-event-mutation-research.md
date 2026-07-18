# IE keyboard event mutation research

Issue #17 asks whether Improvised EOSL can safely reproduce IE-era keyboard event mutation,
especially code shaped like `event.keyCode = 0`. This document is research and design only. It
does not authorize or describe an implemented compatibility shim.

## Decision

Keyboard event compatibility could become a high-value Improvised EOSL feature because legacy
applications often place workflow controls in keyboard handlers. The current evidence justifies a
measurement gate, but not implementation.

The reviewed IE event-object API confirms that `keyCode`, `returnValue`, and `cancelBubble` were
writable properties and that the current event was available through `window.event`. That proves
the pattern is genuinely compatible with IE-era code. It does not by itself prove what a specific
EOSL application expected `event.keyCode = 0` to do, or that assignment to zero always cancelled a
default action.

Current WebView2 uses Chromium DOM events. Its supported host accelerator API operates at a
different layer and cannot reproduce a script mutating an event object. A future bounded
JavaScript shim may be feasible for a measured cancellation pattern, but generic writable IE
event-object parity is neither safe nor justified.

Therefore:

- do not implement Issue #17 yet;
- first obtain a minimal real pattern or controlled reference measurement;
- consider only an exact-origin, separately approved, top-level-document shim;
- describe any accepted behavior as a narrow cancellation compatibility feature, not IE DOM
  emulation; and
- reject native or broad script mechanisms that exceed that contract.

## Source precedence

Compatibility decisions use this order of evidence:

1. Project-local measurements from an agreed IE reference environment and current WebView2.
2. Microsoft documentation for the historical IE event object.
3. Current Microsoft WebView2 documentation and observed wrapper behavior.
4. Current web-platform documentation as background for deprecated modern properties.

MDN is used here to inventory modern deprecated properties. It does not replace an IE reference
measurement or establish an Improvised EOSL compatibility contract.

## Evidence boundary

This research distinguishes four layers that must not be inferred from one another:

1. The raw physical key and Windows message.
2. WebView2 host accelerator handling.
3. The DOM event and values visible to page JavaScript.
4. The final default action and application-visible result.

For example, changing a JavaScript-visible `keyCode` value does not prove that Chromium changed
the underlying key or suppressed its default action. Likewise, handling a WebView2 accelerator
does not reproduce the event object observed by page handlers.

No target EOSL page or captured failing handler is currently part of the repository. The IE origin
of writable event properties is established; the exact compatibility outcome required by a real
application remains unproven.

## Adjacent legacy patterns

| Pattern | Research finding | Project decision |
|---|---|---|
| `event.keyCode = 0` | IE exposed writable `IHTMLEventObj.keyCode`. Modern `KeyboardEvent.keyCode` is deprecated and read-only. The reviewed IE documentation does not establish one universal cancellation meaning for assignment to zero. | Measure downstream reads and final default behavior. Do not promise mutation parity. |
| `window.event` | IE exposed the current event through `IHTMLWindow2.event`. A deprecated `Window.event` also exists in some modern browsers, but code that relies on it is fragile. | Measure current WebView2 first. Do not install a general global-event replacement. |
| `event.returnValue = false` | IE documented this as a way to cancel a cancellable keyboard event. Modern DOM retains `returnValue` as a deprecated alias related to default prevention. | Prefer native behavior when it works. A shim must not override it without a demonstrated gap. |
| `event.cancelBubble = true` | IE used it to stop bubbling. Modern DOM retains the deprecated property with propagation-stopping behavior. | Prefer native behavior and test capture/target/bubble phases independently. |
| `keypress` | A legacy event with browser-dependent character behavior. | Inventory only. Do not recreate a complete legacy keyboard dispatch sequence. |
| `charCode` | Deprecated, read-only character-code exposure associated primarily with `keypress`. | Measure reads only; no writable compatibility promise. |
| `which` | Deprecated, read-only legacy numeric exposure. | Measure reads only; no writable compatibility promise. |
| `keyIdentifier` | Deprecated and non-standard. The reviewed IE `IHTMLEventObj` evidence does not establish it as part of the same IE mutation contract. | Do not label it IE-derived without target-page or reference evidence. Inventory only. |

These properties must be evaluated separately. Finding native WebView2 support for one does not
justify a bundle that changes all keyboard events.

## WebView2 feasibility

### Supported mechanisms and their limits

`CoreWebView2Controller.AcceleratorKeyPressed` allows the host to handle browser accelerator keys.
It is suitable only when the required compatibility behavior is itself a host/browser accelerator
decision. It does not expose DOM handler ordering, `window.event`, property assignments, or the
event target, so it is not a general solution for Issue #17.

`AddScriptToExecuteOnDocumentCreated` can install JavaScript before page script. A controlled
experiment could test whether an individual native keyboard event can receive a shadowing
`keyCode` accessor that:

- initially returns the native value;
- observes a page assignment;
- returns the assigned value to later page code; and
- calls `preventDefault()` only when reference evidence proves that the specific assignment was
  intended to cancel the event.

This is only a feasibility hypothesis. The experiment must establish property configurability,
event extensibility, handler ordering, behavior after `stopPropagation`, browser-default timing,
and effects on frameworks that inspect property descriptors. Failure in any of those areas is a
reason to reject the approach rather than broaden interception.

WebView2 applies document-created scripts to future top-level documents and child-frame
navigations. An initial experiment must therefore exit before installing behavior in every child
frame and prove that top-level origin approval cannot affect frame events. Removing the script
later does not alter documents that already ran it, which is why allow and revoke require reload.

Synthetic redispatch is not an acceptable fallback. A synthetic keyboard event is not trusted,
does not recreate the original browser default action, and can duplicate application handlers.

### Candidate bounded contract

If measurements justify implementation, the smallest defensible contract would be:

- one separately named keyboard-cancellation compatibility API;
- exact normalized HTTP(S) origin approval;
- top-level documents only;
- only trusted, cancellable keyboard events;
- only the event types and assignment values proven by the reference fixture;
- native `preventDefault()` and, only when separately proven, `stopPropagation()` as the final
  effect;
- no synthetic event, key substitution, or mutation of the underlying Windows input; and
- no claim that every IE event property is writable.

The compatibility status and consent UI must identify this API independently. Approval for any
existing API must not imply approval here.

## Security, origin, and permission model

Keyboard handlers can observe sensitive input. A compatibility layer must not become a keylogger
or a cross-origin observation channel.

- Default state is off.
- Permission is per exact normalized HTTP(S) origin and per compatibility API.
- The actual current top-level WebView2 source is authoritative; JavaScript origin strings remain
  untrusted claims.
- Initial scope excludes all iframes. A top-level approval does not authorize a cross-origin child
  document, and the existing top-level source check is insufficient to authenticate a frame.
- Opaque, `file:`, extension, and unsupported origins fail closed.
- Allow, deny, and revoke use the existing compatibility decision model, but enablement takes
  effect only after reload because instrumentation must exist before page handlers run.
- Diagnostics must never contain typed characters, numeric key codes, modifier combinations,
  focused element values, or field contents. They may record only the origin, API decision, and
  aggregate bounded outcome counters such as `native`, `mapped-cancel`, or `blocked`; never one
  record per keyboard event.
- No new OS permission is implied. The user-facing prompt is an Improvised EOSL behavior-change
  decision, not permission for native keyboard capture.

## Explicitly rejected scope

The following are rejected unless a later issue receives explicit design and implementation
approval:

- complete IE DOM or `IHTMLEventObj` emulation;
- enabling keyboard mutation compatibility silently for all origins;
- arbitrary rewriting of page scripts or inline handlers;
- wrapping every listener registration or replacing `EventTarget` globally;
- broad replacement of `KeyboardEvent.prototype`;
- synthetic keyboard-event redispatch;
- a per-keystroke host object, native bridge, Windows hook, or raw-input listener;
- ActiveX or COM exposure;
- disabling WebView2, Chromium, site-isolation, sandbox, or browser accelerator protections; and
- treating F1/`onhelp` handling as proof of general keyboard mutation compatibility.

## Required measurement matrix

Before an implementation proposal, create one minimal fixture from a real failing pattern or an
agreed reference case and record all of the following in the IE reference environment and current
WebView2:

| Dimension | Required observations |
|---|---|
| Event | `keydown`, `keypress`, and `keyup` separately; trusted/cancellable flags |
| Handler context | inline handler, `onkeydown` property, and `addEventListener`; capture/target/bubble order |
| Mutation | value before assignment, assignment result, same-handler read, later-handler read |
| Cancellation | `defaultPrevented`, `returnValue`, `cancelBubble`, propagation, and final visible default action |
| Legacy values | `keyCode`, `charCode`, `which`, and `keyIdentifier` presence and descriptors |
| Global event | `window.event` identity during nested and sequential handlers and its value after dispatch |
| Targets | non-editable content, text input, textarea, select, and contenteditable |
| Host behavior | ordinary keys, F1, Ctrl+F, navigation accelerators, and IME/composition boundaries |
| Lifecycle | initial load, same-origin navigation, cross-origin navigation, allow/reload, revoke/reload |
| Frames | prove no instrumentation in same-origin and cross-origin frames for the initial scope |
| Diagnostics | prove logs contain no key or field data |

Results must separate JavaScript-visible values from final browser behavior. A single console log
showing `keyCode` changed is not sufficient evidence.

## Current measurement snapshot

The first shared-fixture manual pass was recorded on 2026-07-18 in WebView2 / Improvised EOSL and
Microsoft Edge IE mode. This pass is a measurement snapshot, not a compatibility contract:

- `event.keyCode = 0` did not change same-handler or later-handler `keyCode` reads, and did not
  stop measured editable input in either environment.
- `window.event === event` was true during the measured inline and document-bubble `keydown`
  handlers in both environments.
- `event.returnValue = false` canceled measured visible input in WebView2 but not in Edge IE mode;
  the `preventDefault()` control canceled input in both.
- `event.cancelBubble = true` and `stopPropagation()` both prevented measured document-bubble
  `keydown` propagation in both environments.
- `keypress`, `charCode`, and `which` matched between WebView2 and Edge IE mode for measured
  lowercase `f`, shifted `F`, and `Enter`.
- `keyIdentifier` was missing from measured event snapshots and descriptor inventory in both
  environments.

The current aggregate gate result is to keep the fixture/docs path and avoid behavior-changing
keyboard shims. #46 and #48 remain research items; #47, #49, and the measured subset of #50 are
tentative native-sufficient paths; #51 remains docs-only / rejected unless real target evidence
appears.

External legacy-code research after this pass found actual application/forum patterns that write
`window.event.keyCode`, especially Enter-to-Tab remapping with `keyCode = 9` and suppression
bundles that combine `keyCode = 0`, `returnValue=false`, and inline handler `return false`.
Those patterns are narrower than generic writable event emulation but broader than the first #46
measurement. `keyboard-legacy-patterns.html` exists to measure those focused patterns with compact
visible outcomes instead of a full event-row log.

## Exit choices after measurement

The measurement review must choose exactly one outcome:

1. Reject compatibility because the required behavior cannot be reproduced without broad or
   unsafe interception.
2. Retain a fixture and documentation only because native WebView2 behavior is already sufficient
   or target demand is not established.
3. Propose a bounded shim with an explicit contract, API identifier, origin/permission behavior,
   failure diagnostics, automated policy tests, and a normal-user manual validation plan.

Outcome 3 requires separate implementation approval. It must not be inferred from approval of
this research document.

## Reviewed sources

- Microsoft Learn previous versions: `IHTMLEventObj interface`
  - https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/platform-apis/aa703876(v=vs.85)
- Microsoft Learn previous versions: `onkeydown event`
  - https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/platform-apis/aa743038(v=vs.85)
- Microsoft Learn previous versions: `cancelBubble property`
  - https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/platform-apis/aa703875(v=vs.85)
- Microsoft Learn: `CoreWebView2Controller.AcceleratorKeyPressed`
  - https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2controller.acceleratorkeypressed
- Microsoft Learn: `CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync`
  - https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.addscripttoexecuteondocumentcreatedasync
- MDN: `KeyboardEvent.keyCode`, `KeyboardEvent.charCode`, `UIEvent.which`,
  `Event.returnValue`, `Event.cancelBubble`, `Window.event`, and `KeyboardEvent.keyIdentifier`
  - https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/keyCode
  - https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/charCode
  - https://developer.mozilla.org/en-US/docs/Web/API/UIEvent/which
  - https://developer.mozilla.org/en-US/docs/Web/API/Event/returnValue
  - https://developer.mozilla.org/en-US/docs/Web/API/Event/cancelBubble
  - https://developer.mozilla.org/en-US/docs/Web/API/Window/event
  - https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/keyIdentifier
