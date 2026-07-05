# Dialog feature application design

This document defines the intended design for applying parsed `showModalDialog` feature strings to the WPF child window.

It is a design note only. It does not claim that the current parser or future window behavior is Internet Explorer compatible until the reference tests in `docs/dialog-feature-reference-checklist.md` are completed.

## Goals

- Keep legacy call sites unchanged.
- Keep parser behavior separate from WPF window behavior.
- Make every IE compatibility assumption visible and testable.
- Allow measured IE / Edge IE mode behavior to replace provisional assumptions with small code changes.
- Log unsupported and approximated behavior without pretending it is compatible.

## Non-goals

- Reproduce the full Internet Explorer window chrome.
- Emulate every historical browser theme or OS theme.
- Treat `status` or `scroll` as supported before reference evidence exists.
- Apply `timeoutMs` as a legacy browser feature. It remains a spike diagnostic.

## Proposed layers

```text
raw feature string
        |
        v
DialogFeatureParser
        |
        v
DialogFeatures
        |
        v
DialogFeatureApplicationPolicy
        |
        v
DialogWindowOptions
        |
        v
WPF DialogWindow
```

### DialogFeatureParser

Responsibility:

- Parse names and values from the raw feature string.
- Preserve unknown fields for logging.
- Avoid WPF or screen-specific decisions.
- Avoid final compatibility judgments that depend on IE reference behavior.

### DialogFeatureApplicationPolicy

Responsibility:

- Convert `DialogFeatures` into window options.
- Apply measured or documented compatibility policy.
- Record diagnostics for ignored, clamped, unsupported, or approximated values.
- Keep all screen, DPI, and WPF-specific decisions out of the parser.

This policy should be deterministic and unit-testable without creating a real WebView2.

### DialogWindowOptions

Suggested shape:

```csharp
public sealed record DialogWindowOptions(
    double? Width,
    double? Height,
    double? Left,
    double? Top,
    bool Center,
    ResizeMode ResizeMode,
    DialogFeaturePolicyStatus PolicyStatus,
    IReadOnlyList<DialogFeatureDiagnostic> Diagnostics);
```

This is a design sketch, not an implementation requirement. The final type may use project-local names.

The spike originally used `DialogFeaturePolicyStatus.ProvisionalUntilIeReferenceMeasured`.
Recorded Edge IE mode evidence now supports the measured MVP subset, so current runtime logs use
`ReferenceValidated`. Clamping and unsupported fields remain explicitly documented approximations.

### DialogFeatureDiagnostic

Suggested diagnostic categories:

- `applied`
- `ignored`
- `clamped`
- `approximated`
- `unsupported`
- `invalid`

Diagnostics should include:

- feature name
- raw value where available
- reason
- applied value where applicable

## Measured MVP mapping

These mappings define the measured MVP subset. Safety clamps and unsupported presentation
features remain explicit approximations rather than claims of complete IE parity.

| Feature | WPF target | Compatibility status |
| --- | --- | --- |
| `dialogWidth` | `Window.Width` | Measured outer-frame compensation at 100% scale; screen-aware safety clamp |
| `dialogHeight` | `Window.Height` | Measured outer-frame compensation at 100% scale; screen-aware safety clamp |
| `dialogLeft` | `Window.Left` | Measured absolute screen intent; clamped to visible work area |
| `dialogTop` | `Window.Top` | Measured absolute screen intent; clamped to visible work area |
| `center` | `WindowStartupLocation` or calculated `Left` / `Top` | Omitted `center` defaults to `center:yes`; explicit left/top override centering |
| `resizable` | `ResizeMode.CanResize` or `ResizeMode.NoResize` | Corrected Edge IE mode measurement showed `resizable:yes` enables resizing; omitted `resizable` and `resizable:no` disable resizing |
| `status` | diagnostic only | Likely unsupported or approximated |
| `scroll` | diagnostic only | Likely unsupported or delegated to page CSS/Chromium |
| unknown fields | diagnostic only | Ignore and log |
| malformed values | diagnostic only | Ignore and log |
| `timeoutMs` | runner timeout | Spike-only diagnostic, not legacy compatibility |

## Compatibility decisions

The recorded reference results resolved the MVP size, position, center, and resize decisions.
The following list remains useful as the measurement inventory:

- Whether `dialogWidth` and `dialogHeight` target outer window size or content viewport size.
- Whether unitless numbers are accepted exactly like `px`.
- How decimal values are handled.
- How zero, negative, very small, and huge sizes are handled.
- Whether coordinates are absolute screen coordinates.
- Whether `resizable:no` behavior varies by IE mode configuration; corrected Edge IE mode measurement showed it disables resizing.
- Whether `status` changes visible IE mode chrome.
- Whether `scroll` changes dialog-level scrolling or page-level scrolling.

Current measurement update:

- `dialogWidth:500px;dialogHeight:300px` produced a normal dialog.
- `dialogWidth:500;dialogHeight:300` produced a screen-sized/maximized dialog in the measured Edge IE mode environment.
- Therefore, `dialogWidth` and `dialogHeight` must not treat unitless numbers as pixel values.
- `dialogWidth:500.8px;dialogHeight:300.2px` produced the same measured size as `500px` / `300px`. Therefore, decimal size values are truncated toward zero.
- `dialogWidth:-500px;dialogHeight:-300px` produced the same measured default-ish size as invalid text values. Therefore, negative width/height values are ignored rather than clamped to the small minimum.
- `dialogWidth:0px;dialogHeight:0px` produced a small clamped dialog, while `dialogWidth:5000px;dialogHeight:3000px` produced a screen-sized/maximized dialog. Exact WPF clamp values remain policy work because they depend on chrome, screen, and DPI mapping.
- Duplicate size fields used the last value: `dialogWidth:400px;dialogWidth:700px;dialogHeight:250px;dialogHeight:450px` produced the measured size for `700px` / `450px`.
- Omitted `center` produced the same centered position as `center:yes` in repeated Edge IE mode runs, including after moving the previous dialog. Therefore, omitted `center` defaults to `center:yes`.
- `dialogLeft:120px;dialogTop:80px` produced observed `screenLeft=128` and `screenTop=111` in the measured Edge IE mode environment. This confirms explicit position is honored, while the chrome/screen offset still needs to be handled deliberately when mapping to WPF.
- `center:yes;dialogLeft:120px;dialogTop:80px` also produced observed `screenLeft=128` and `screenTop=111`. Therefore, explicit `dialogLeft` / `dialogTop` override centering.
- `dialogLeft:-200px;dialogTop:-100px` produced observed `screenLeft=8` and `screenTop=31`, while `dialogLeft:5000px;dialogTop:3000px` produced observed `screenLeft=1412` and `screenTop=772` for a `516x339` outer window on a `1920x1080` screen. Therefore, offscreen positions are clamped into the visible screen area, with browser/dialog chrome offsets still needing WPF-specific mapping.
- `dialogWidth=500px;dialogHeight=300px;center=yes` produced the same measured size and centered position as the equivalent colon-separated case. Therefore, both `:` and `=` separators are accepted.
- Omitted `resizable` did not allow manual resizing in the corrected Edge IE mode measurement. Therefore, omitted `resizable` defaults to no resize.
- `status:yes` showed a status bar and increased outer height while keeping the same client viewport; `status:no` matched the base no-status-bar size. WPF status-bar emulation remains out of MVP until designed.
- `scroll:no` removed scrollbars and changed the client viewport to match the requested size, while `scroll:yes` matched the base viewport. Faithful `scroll` behavior remains unsupported until WebView2/page-level handling is designed.

## Test strategy

### Unit tests

Tests cover `DialogFeatureApplicationPolicy` and its explicit approximation diagnostics.

Unit tests should cover:

- width and height application
- left and top application
- centering default
- explicit position versus center precedence
- resize mode mapping
- invalid numeric values
- excessive dimensions and clamping
- unsupported `status` and `scroll` diagnostics
- unknown feature diagnostics

Test names should distinguish measured behavior from explicit approximations.

Current tests use reference-validated names for the measured MVP subset. Diagnostics still name
clamping and unsupported behavior rather than implying complete historical IE parity.

### Manual tests

Use:

- `src/ImprovisedEosl.Spike.SyncModal/pages/feature-reference.html`
- `src/ImprovisedEosl.Spike.SyncModal/pages/feature-dialog.html`
- `docs/dialog-feature-reference-checklist.md`
- `docs/edge-ie-mode-reference-test.md`

Manual comparison must include:

- Edge IE mode reference result
- WebView2 spike result
- accepted mismatch reason if not identical
- diagnostic log output for unsupported or approximated behavior

## Implementation order

1. Add `DialogWindowOptions` and diagnostic model without changing runtime behavior. Done in spike commit after this design.
2. Add `DialogFeatureApplicationPolicy` with provisional rules behind tests. Done in spike commit after this design.
3. Log the calculated options and diagnostics before applying them to WPF. Done in spike commit after this design.
4. After IE reference testing, update policy expectations. Done for the MVP feature subset.
5. Apply safe MVP features to WPF: width, height, position, center, resize mode. Done for the spike.
6. Keep `status` and `scroll` as diagnostics until a faithful behavior is defined. Current behavior.

## Implemented MVP checklist

- Apply measured width and height to the child WPF window, accounting for the fact that Edge IE mode measurements expose both outer size and client viewport size.
- For the measured 100% scale environment, applying WPF outer window size as requested width plus `16` and requested height plus `39` best matches the Edge IE mode `500px` / `300px` observation (`516x339` outer).
- Treat zero and huge sizes with screen-aware clamping in the WPF application layer, not in the parser.
- Treat invalid or negative width/height as omitted/default size behavior.
- Apply explicit `dialogLeft` / `dialogTop` before centering decisions; explicit coordinates override `center:yes`.
- Clamp negative and offscreen positions into the visible screen area.
- Map `center` omitted to centered placement when no explicit position is present.
- Map only `resizable:yes` to a resizable WPF window; omitted `resizable` and `resizable:no` should disable resizing.
- Keep `status`, `scroll`, and unknown fields in diagnostics only for MVP.
- Keep repeatable WebView2/manual checks comparing applied WPF behavior against `docs/dialog-feature-reference-checklist.md`.

## Safety rules

- Do not disable WebView2 security features to emulate old dialog behavior.
- Do not expose feature behavior to unapproved origins.
- Do not treat off-screen or huge windows as harmless; clamp or log according to documented policy.
- Do not let feature parsing affect parent WebView2 while the synchronous host call is blocked.
- Keep all WPF window mutation on the child STA thread.
- `PolicyStatus` is `ReferenceValidated` because the supporting evidence is recorded in
  `docs/dialog-feature-reference-checklist.md`.
