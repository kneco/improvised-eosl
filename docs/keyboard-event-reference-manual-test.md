# Keyboard event reference manual test

Issue #17 and child issues #46 through #52 require reference measurements before any keyboard
compatibility implementation can be proposed. This checklist uses a shared local fixture for Edge
IE mode and WebView2 comparison.

The fixture is measurement-only. It must not be used as evidence that a behavior-changing shim is
approved.

## Scope

This checklist supports:

- #46: `event.keyCode = 0`
- #47: `window.event`
- #48: `event.returnValue = false`
- #49: `event.cancelBubble = true`
- #50: `keypress`, `charCode`, and `which`
- #51: `keyIdentifier`
- #52: aggregate review gate

## Boundaries

The fixture does not:

- install an Improvised EOSL compatibility API;
- expose a host object or native bridge;
- intercept native input;
- synthesize or redispatch keyboard events;
- rewrite page script;
- replace `KeyboardEvent.prototype`, `EventTarget`, or listener registration;
- change WebView2 settings; or
- weaken WebView2, Chromium, site isolation, sandbox, certificate, or browser accelerator behavior.

Application diagnostics must not be used as a per-key event log. The page shows local event
metadata for manual comparison, but copied notes must not include field contents or arbitrary
private key sequences.

## Start WebView2 reference

Run from a normal user PowerShell:

```powershell
dotnet run --configuration Release --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj
```

From the built-in home page, open `keyboard event fixture`.

Alternative direct URL while the local server is running:

```text
http://127.0.0.1:18080/keyboard-event-reference.html
```

For focused legacy Web Forms style key-write checks, use:

```text
http://127.0.0.1:18080/keyboard-legacy-patterns.html
```

## Start Edge IE mode reference

1. Start Improvised EOSL so the local test server is listening on port `18080`.
2. Open Microsoft Edge to:

   ```text
   http://127.0.0.1:18080/keyboard-event-reference.html
   ```

3. Reload the page in Internet Explorer mode.
4. Confirm the IE mode indicator is visible before recording reference measurements.

For the focused legacy-pattern page, use the same IE mode setup with:

```text
http://127.0.0.1:18080/keyboard-legacy-patterns.html
```

Record the exact IE mode setup path, Edge version, OS build, display scale, keyboard layout, and
whether IME is active.

## Required observation rules

- Record Edge IE mode and WebView2 results separately.
- Do not infer final behavior from JavaScript-visible values alone.
- Record `keydown`, `keypress`, and `keyup` separately.
- Record inline handler, property handler, and `addEventListener` cases separately.
- Record capture, target, bubble, and later-listener observations separately.
- Record editable and non-editable targets separately.
- Keep browser and host accelerator behavior separate from DOM event behavior.
- Treat IME/composition as a boundary note unless a real target page requires exact behavior.

## Minimum matrix

For each browser environment, test at least these handler actions:

| Action | Primary child issue |
| --- | --- |
| `none` | #50 / baseline |
| `event.keyCode = 0` | #46 |
| `event.keyCode = original keyCode` | #46 |
| `event.returnValue = false` | #48 |
| `event.preventDefault()` | #48 control |
| `event.cancelBubble = true` | #49 |
| `event.stopPropagation()` | #49 control |
| `returnValue=false + cancelBubble=true` | #48 / #49 interaction check |

For each action, use these target groups where practical:

| Target | Purpose |
| --- | --- |
| inline handler target | inline-event compatibility |
| property handler target | `onkeydown` / property handler compatibility |
| addEventListener target | modern listener ordering comparison |
| input target | editable text behavior |
| textarea target | multi-line editable behavior |
| select target | form-control keyboard behavior |
| contenteditable target | editable DOM behavior |

Use these key cases as a minimum:

| Case label | Key input |
| --- | --- |
| `letter-a` | `a` |
| `letter-shift-a` | `Shift+A` |
| `number-1` | `1` |
| `symbol-semicolon` | `;` or the equivalent key on the active layout |
| `space` | Space |
| `enter` | Enter |
| `escape` | Escape |
| `backspace` | Backspace |
| `delete` | Delete |
| `tab` | Tab |
| `arrow-left` | Left arrow |
| `f1` | F1 |
| `ctrl-f` | Ctrl+F |

If available, add one IME/composition observation and one browser navigation accelerator
observation. Do not treat those as part of the basic keyboard-event mutation contract.

## Result interpretation

For each child issue, classify only after comparing Edge IE mode and WebView2:

- `native sufficient`
- `unsupported/rejected`
- `docs-only`
- `candidate for separately approved bounded shim`

A bounded shim candidate must still go through #52 and a separate implementation approval. It must
define a new compatibility API permission, exact normalized HTTP(S) origin scope, top-level-only
initial scope, reload requirement for allow/revoke, frame exclusion, no synthetic redispatch, no
native key bridge, and diagnostics with no keylogging behavior.

## Focused legacy-pattern fixture

`keyboard-legacy-patterns.html` is a smaller companion page for the external legacy patterns found
after the first #46 pass. It intentionally shows only the latest compact result instead of a full
event row log.

Use it for:

- #46: `window.event.keyCode = 0`, `window.event.keyCode = 9`, and `event.keyCode = 9`;
- #48: `event.returnValue = false`, `return false`, and `preventDefault()` controls;
- #49: `event.cancelBubble = true` with same-target late listener and document-bubble visibility;
- #52: deciding whether a real legacy pattern remains after the focused measurement.

Initial focused pass:

1. Open the page in WebView2 and Edge IE mode.
2. Confirm `documentMode`, `compatMode`, and `userAgent` are visible in the environment section.
3. Keep the default inline `onkeydown` handler source.
4. For each action below, click `reset`, focus Field A, press Enter once, and record only:
   `keydown`, `keyCode readback`, `focus before`, `focus after tick`, `value length delta`,
   `document bubble`, `same-target late`, `keypress values`, and `window.event same`.

Focused actions:

| Action | Primary question |
| --- | --- |
| `none` | Baseline Enter behavior |
| `window.event.keyCode = 9` | Enter-to-Tab remapping claim |
| `event.keyCode = 9` | Handler-argument write comparison |
| `window.event.keyCode = 0` | Enter suppression by keyCode write alone |
| `event.returnValue = false` | Legacy cancellation without keyCode write |
| `window.event.keyCode = 0 + returnValue=false` | Common bundled suppression pattern |
| `return false from handler` | Inline handler return-value cancellation |
| `event.preventDefault()` | Modern cancellation control |
| `event.cancelBubble = true` | Propagation-only control |

Do not paste field contents into issue comments. If exact values are needed, use the compact JSON
from the page; it records value-length deltas rather than field text.

## 2026-07-18 focused legacy-pattern result summary

User-assisted focused measurements were recorded with `keyboard-legacy-patterns.html` in WebView2 /
Improvised EOSL and Microsoft Edge IE mode. The first version of the fixture included a hidden
submit button and focusable report textarea, which polluted the IE mode focus path. Those early
focus results are treated as fixture noise. The corrected fixture keeps the Field A/B/C probe
outside a form, adds `focus before`, and removes the compact JSON textarea from normal Tab order.

Corrected Field A + Enter results:

| Action | WebView2 result | Edge IE mode `documentMode=11` result | Current interpretation |
| --- | --- | --- | --- |
| `none` | `Enter 13 -> 13`, focus stays `field-a`, `keypress` has `13/13/13` | Same, except baseline `returnValue` readback is `undefined` instead of `true` | Baseline focus path is stable in the corrected fixture. |
| `window.event.keyCode = 9` | `Enter 13 -> 13`, focus stays `field-a` | Same | No Enter-to-Tab remapping in the supported reference environment. |
| `window.event.keyCode = 0` | `Enter 13 -> 13`, `keypress` still appears | Same | KeyCode write alone does not cancel, mutate readback, or suppress `keypress`. |
| `window.event.keyCode = 0 + returnValue=false` | `rv=false`, `dp=true`, `keypress=none` | Same | Suppression appears attributable to `returnValue=false`, not `keyCode=0`. |
| `event.returnValue = false` | `rv=false`, `dp=true`, `keypress=none` | Same | Native sufficient for the focused Enter cancellation path. |
| `event.preventDefault()` | `rv=false`, `dp=true`, `keypress=none` | Same | Matches `returnValue=false` for focused Enter cancellation. |
| `return false from handler` | `rv=true`, `dp=false`, `keypress=none` | Same | Inline handler return cancellation is distinct from `returnValue` readback but matches across environments. |
| `event.cancelBubble = true` | `document bubble=no`, `same-target late=yes`, `keypress` still appears | Same | Native sufficient for the focused propagation path. |

The focused results do not justify writable `keyCode` emulation for the Edge IE mode
`documentMode=11` reference environment. A real target codebase may still contain old IE6-8-era
`keyCode` writes, but current evidence says to look for active behavior in `returnValue=false`,
inline `return false`, `preventDefault()`, explicit `.focus()`, or framework helpers before
considering a shim.

Function-key / browser-accelerator suppression is not concluded by this Enter-focused matrix.
That product-critical question is split to #55 and should measure F1/F3/F5/F6/F10/F11/F12
browser/host outcomes directly.

## 2026-07-18 first manual measurement summary

User-assisted measurements were recorded in #46 through #52 with this fixture in WebView2 /
Improvised EOSL and Microsoft Edge IE mode. The measured subset does not authorize a
behavior-changing keyboard shim.

| Issue | Measured subset | Current result |
| --- | --- | --- |
| #46 `event.keyCode = 0` | `property handler target` `keydown` letter `a`; `input target` visible input with letter `x` | Assignment executed, but same-handler and later-handler `keyCode` remained unchanged in both environments. Text input was still added in both environments. Continue research; do not assume universal cancellation. |
| #47 `window.event` | `input target` `keydown` letter `e` | `window.event === event` was true in inline and document-bubble handlers in both environments. Tentative native sufficient for simple handler-time identity only. |
| #48 `event.returnValue = false` | `input target` visible input with letter `y`; `preventDefault()` control with letter `z` | `returnValue=false` canceled visible input in WebView2 but not in Edge IE mode. `preventDefault()` canceled in both. Difference recorded, but no shim candidate until row-level and target-pattern evidence exists. |
| #49 `event.cancelBubble = true` | `input target` `keydown` letter `c`; `stopPropagation()` control with letter `d` | Target handlers ran and `document-bubble / keydown` did not run in both environments. Tentative native sufficient for the measured input propagation path. |
| #50 `keypress`, `charCode`, `which` | `input target` lowercase `f`, `Shift+F`, and `Enter` | WebView2 and Edge IE mode matched for all measured rows. Tentative native sufficient for the measured printable-letter and Enter inventory. |
| #51 `keyIdentifier` | `input target` inventory and property descriptors | `keyIdentifier` was omitted from `before` rows and descriptor inventory reported `missing` in both environments. Treat as docs-only / reject unless target evidence appears. |
| #52 aggregate gate | Child issue review after the measurements above | Do not implement a behavior-changing keyboard shim from the current evidence. Keep #52 open until the fixture PR and child issue classifications are reviewed. |

Known gaps remain: non-editable focus reliability, same-target later listeners, additional keys
such as Backspace/Delete/Tab/arrows/F1/Ctrl+F, handler-outside `window.event` lifetime,
nested-dispatch behavior, IME/composition, and any real target-page dependency.

## Result notes

### Edge IE mode

Date:
Tester:
OS:
Edge version:
IE mode setup:
Keyboard layout:
IME/composition:
URL:

Observations:

### WebView2 / Improvised EOSL

Date:
Tester:
Build / branch:
WebView2 Runtime:
Keyboard layout:
IME/composition:
URL:

Observations:
