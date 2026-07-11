# Navigation accelerator suppression research

Issue #24 asks whether administrator JSON policy can suppress Back, Forward, and Reload operations
from both wrapper buttons and browser shortcuts. This document records the pre-implementation
research gate. It does not authorize code changes yet.

## Decision

Do not use `CoreWebView2Settings.AreBrowserAcceleratorKeysEnabled = false` for this issue. It is
too broad because it disables browser features beyond the requested navigation group, including
find-in-page.

Keep the policy split introduced in `docs/browser-shell-policy.md`:

- toolbar command visibility controls wrapper chrome only;
- navigation accelerator suppression controls targeted Back, Forward, and Reload shortcut behavior;
- neither surface is a kiosk, DLP, origin allow-list, or browser security feature.

The implementation design must be chosen after measuring the key path in the current WPF wrapper:

1. WebView2 WPF forwarded routed key event only.
2. Direct `CoreWebView2Controller.AcceleratorKeyPressed` handling if accessible and necessary.
3. No implementation if the required distinction cannot be made without suppressing unrelated
   browser behavior.

## Source evidence

Microsoft documents `AreBrowserAcceleratorKeysEnabled` as disabling all browser-specific
accelerators when set to `false`, including:

- `Ctrl+F` and `F3` for Find on Page;
- `Ctrl+R` and `F5` for Reload;
- print, zoom, DevTools, and special browser keys such as Back and Forward.

The same documentation states that text editing and movement keys such as Home, End, Page Up,
Page Down, `Ctrl+C`, `Ctrl+V`, `Ctrl+A`, and `Ctrl+Z` are not disabled by that global setting and
remain enabled unless handled in `AcceleratorKeyPressed`.

`CoreWebView2Controller.AcceleratorKeyPressed` is raised for accelerator keys whether browser
accelerators are globally enabled or not. Microsoft defines accelerator keys as keys pressed with
Ctrl or Alt, or keys that do not map to characters. In windowed mode, the event is synchronous:
the browser process waits until the handler returns or `Handled` is set. Therefore any future host
handler must do only bounded, in-memory policy matching and must avoid logging or async work inside
the event path.

`CoreWebView2AcceleratorKeyPressedEventArgs.Handled` and
`CoreWebView2AcceleratorKeyPressedEventArgs.IsBrowserAcceleratorKeyEnabled` are different
controls:

- `Handled = true` stops propagation and web content does not receive the key.
- `IsBrowserAcceleratorKeyEnabled = false` skips WebView2 browser-feature handling while allowing
  the event to continue to web content.

That distinction is the central design question for Issue #24.

Primary sources:

- Microsoft Learn:
  `CoreWebView2Settings.AreBrowserAcceleratorKeysEnabled`
  <https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2settings.arebrowseracceleratorkeysenabled>
- Microsoft Learn:
  `CoreWebView2Controller.AcceleratorKeyPressed`
  <https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2controller.acceleratorkeypressed>
- Microsoft Learn:
  `CoreWebView2AcceleratorKeyPressedEventArgs`
  <https://learn.microsoft.com/en-us/microsoft-edge/webview2/reference/winrt/microsoft_web_webview2_core/corewebview2acceleratorkeypressedeventargs>

## Current project observations

The current WPF shell does not set `AreBrowserAcceleratorKeysEnabled` and does not register a
project-owned accelerator handler.

Find-in-page currently uses two layers:

- WebView2's built-in browser accelerator behavior remains enabled for web-content focus.
- `MainWindow.PreviewKeyDown` recognizes `Ctrl+F` through `BrowserFindShortcutPolicy` so focus in
  wrapper chrome, such as the address field, can still open WebView2 Find UI.

The installed `Microsoft.Web.WebView2` WPF package documents an internal
`CoreWebView2Controller_AcceleratorKeyPressed` path that forwards accelerator input to WPF
`PreviewKeyDown` / `KeyDown` routed events. That explains why WPF shortcut handling may see some
WebView2-focused accelerators. It does not by itself prove that the project can set
`IsBrowserAcceleratorKeyEnabled` from a WPF routed event.

Implementation must therefore validate whether the current public WPF surface can access the
needed direct event args. If it cannot, a WPF routed-event implementation may still suppress some
keys by setting `KeyEventArgs.Handled`, but that would be equivalent to the stronger "host handles
the key" behavior and would not preserve page receipt of the key.

## Candidate key matrix

The first measurement fixture should use a simple page with:

- a navigation history stack long enough to go Back and Forward;
- a reload counter stored in `sessionStorage` or visible DOM state;
- focused text input and textarea cases;
- capture/bubble JavaScript key listeners that record key, code, modifier flags, and
  `defaultPrevented` without logging field contents; and
- visible markers for page navigation, reload, and JavaScript event receipt.

Minimum cases:

| Key input | Target command | Browser-default expectation | Suppressed expectation to measure | Notes |
| --- | --- | --- | --- | --- |
| `Alt+Left` | Back | Browser history back if possible | No history movement | Page receipt depends on chosen `Handled` vs `IsBrowserAcceleratorKeyEnabled` path. |
| Browser Back key | Back | Browser history back if hardware key exists | No history movement | May be unavailable on many keyboards. |
| `Alt+Right` | Forward | Browser history forward if possible | No history movement | Must test after a successful Back. |
| Browser Forward key | Forward | Browser history forward if hardware key exists | No history movement | May be unavailable on many keyboards. |
| `Ctrl+R` | Reload | Reload current document | No reload | Must not affect `Ctrl+F`. |
| `F5` | Reload | Reload current document | No reload | Function-key handling may differ from Ctrl-based handling. |
| Backspace outside editable text | Possible Back | Unknown in current WebView2 baseline | Do not claim support until measured | Issue body mentions Backspace; modern browser behavior may not navigate. |
| Backspace in input/textarea | Text editing | Delete character | Delete character | Must not be suppressed as navigation. |
| `Ctrl+F` | Find | Open WebView2 Find UI | Still open Find UI | Existing Issue #11 behavior must survive. |
| `F3` | Find next | Continue WebView2 Find session | Still continue Find session | Requires an active find session for meaningful validation. |
| `Ctrl+C` / `Ctrl+V` | Editing | Copy/paste | Copy/paste | Must remain outside navigation policy. |
| Page Up / Page Down | Movement | Page movement | Page movement | Must remain outside navigation policy. |

The built-in fixture for this baseline is
`src/ImprovisedEosl.Spike.SyncModal/pages/navigation-accelerator-reference.html`. It can be opened
from the built-in home page or directly with `--navigation-accelerator-manual`. The page does not
install any suppression hook. It records only bounded event categories and visible browser effects
so the next prototype can compare WebView2/browser behavior against WPF routed-event behavior
without relying on production policy code.

## Design options to test

### Option A: Direct browser-accelerator control

Use `CoreWebView2Controller.AcceleratorKeyPressed` and set
`IsBrowserAcceleratorKeyEnabled = false` for targeted Back, Forward, and Reload keys.

Advantages:

- closest to the policy language of "disable browser handling for this browser accelerator";
- can allow page JavaScript to receive the key when that is safer for line-of-business pages; and
- keeps `Ctrl+F`/`F3` enabled by default.

Risks:

- the WPF WebView2 wrapper may not expose the direct controller event as a stable public surface;
- the event is synchronous in windowed mode, so the handler must stay minimal; and
- page JavaScript receiving `Ctrl+R` or `F5` may surprise applications if they already bind those
  shortcuts.

### Option B: WPF routed-event handling

Use the existing WPF `PreviewKeyDown` path and set `KeyEventArgs.Handled = true` for targeted
navigation keys.

Advantages:

- matches the existing `Ctrl+F` shortcut structure;
- may be sufficient because the WPF package forwards WebView2 accelerator input into routed
  events; and
- avoids lower-level controller access if the WPF wrapper does not expose it.

Risks:

- likely prevents web content from receiving the key, matching `Handled` rather than
  `IsBrowserAcceleratorKeyEnabled`;
- may not see every hardware browser key; and
- must not become an arbitrary key-interception layer.

### Option C: Reject suppression until a narrower host surface exists

Reject Issue #24 implementation if the WPF surface cannot suppress browser handling without
over-suppressing page input or disabling find-in-page.

This is acceptable because the project must log unsupported behavior rather than silently claim
compatibility or security behavior it cannot enforce.

## Security and privacy boundary

Diagnostics may log only:

- policy source and normalized policy values;
- bounded key category names such as `reload`, `history-back`, or `unsupported`;
- action category such as `browser-default`, `browser-suppressed`, `host-handled`, or `ignored`;
- whether the event was `KeyDown` or `SystemKeyDown`.

Diagnostics must not log typed characters, text field contents, arbitrary key sequences, URLs with
query strings, or one record per ordinary keypress.

This feature must not:

- disable WebView2 sandboxing, site isolation, storage, certificates, or network security;
- block page script navigation, redirects, clicked links, form submission, or address-bar
  navigation;
- hide the native close affordance;
- mutate page keyboard events or emulate IE DOM keyboard behavior; or
- become a keylogger or native hotkey bridge.

## Implementation gate

Before implementing policy parsing or runtime suppression:

1. Use the manual measurement fixture for the key matrix above.
2. Record current standard behavior in a normal user PowerShell.
3. Prototype the smallest possible event hook behind a temporary measurement flag, not policy.
4. Compare direct-event and WPF-routed behavior where available.
5. Decide whether Issue #24 should use `IsBrowserAcceleratorKeyEnabled = false`, `Handled = true`,
   or remain unsupported.
6. Update `docs/browser-shell-policy.md`, `docs/browser-shell-policy-manual-test.md`, and
   `docs/implementation-plan.md` with the measured decision before adding production policy code.

## Fixture status

- `navigation-accelerator-reference.html` exists as a baseline-only local fixture.
- `--navigation-accelerator-manual` starts the application directly at that fixture without
  changing production browser shell policy, WebView2 security settings, or accelerator handling.
- No direct `AcceleratorKeyPressed` hook and no WPF suppression hook are present yet.
- A first normal-user baseline was recorded on 2026-07-11 in
  `docs/navigation-accelerator-manual-test.md`. It confirms the fixture loads, the history stack
  can be prepared, `Alt+Left` and `Alt+Right` move through the prepared history stack, `Ctrl+R`
  and F5 reload, `Ctrl+F` and `F3` find behavior works, Backspace outside editable controls does
  not trigger history-back navigation in the tested flow, and editable-field Backspace / copy /
  paste behavior works. The tester's keyboard has no dedicated browser Back / Forward hardware
  keys, and the current scope does not require hardware-key coverage unless target deployment
  hardware introduces it. Production suppression design remains gated on comparing a temporary direct
  `AcceleratorKeyPressed` hook with the WPF routed-event path.
- The WPF package documentation describes an internal `CoreWebView2Controller_AcceleratorKeyPressed`
  path that forwards WebView2 accelerator input into WPF key events, but the WPF control does not
  expose that direct controller event as the obvious application-level surface used by this spike.
  The next measurement therefore adds `--navigation-accelerator-wpf-suppress-manual`, a temporary
  WPF routed-event suppression mode. This can validate whether WPF `Handled=true` blocks the target
  navigation accelerators, but it does not prove the more precise
  `IsBrowserAcceleratorKeyEnabled=false` behavior.
