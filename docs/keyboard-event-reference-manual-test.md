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

## Start Edge IE mode reference

1. Start Improvised EOSL so the local test server is listening on port `18080`.
2. Open Microsoft Edge to:

   ```text
   http://127.0.0.1:18080/keyboard-event-reference.html
   ```

3. Reload the page in Internet Explorer mode.
4. Confirm the IE mode indicator is visible before recording reference measurements.

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
