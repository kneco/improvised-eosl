# Dialog feature string compatibility

## Purpose

`window.showModalDialog(url, arguments, features)` compatibility depends on the `features` string more than normal browser UI polish. Legacy applications often rely on `dialogWidth`, `dialogHeight`, `dialogLeft`, `dialogTop`, `center`, and `resizable` to make unchanged workflows usable.

The project goal is "improvised EOSL": keep old application code working where practical. Therefore, dialog feature handling must be treated as compatibility behavior, not as optional shell decoration.

## Compatibility principle

- Bias toward Internet Explorer / Edge IE mode behavior for legacy feature parsing and dialog sizing.
- Do not treat the current parser as authoritative until reference behavior is measured.
- Do not require legacy caller code changes to get basic sizing, positioning, or modality behavior.
- Log unsupported or approximated feature behavior explicitly.
- Keep the spike-only `timeoutMs` option separate from legacy `showModalDialog` compatibility.
- Do not apply parsed features to child windows until the expected compatibility behavior is documented well enough to test.
- Reject feature strings larger than 16 KiB UTF-8 or 128 entries before parsing; these are wrapper safety limits rather than IE compatibility claims.

## Current source status

Official Microsoft documentation for WebView2 synchronization has already been used for the host-object feasibility decision.

The MVP feature-string behavior has now been measured against Microsoft Edge IE mode on the local IE-safe reference page. `docs/dialog-feature-reference-checklist.md` is the raw evidence log, and `docs/dialog-feature-application-design.md` records the implementation boundary between parsing, compatibility policy, and WPF window mutation.

This document is now the MVP compatibility contract for the measured feature subset. It is still not a full historical Internet Explorer contract; behavior may vary by OS theme, display scale, multi-monitor layout, or old IE versions outside the measured Edge IE mode environment.

## Measured MVP Compatibility Contract

### Parsing

- Feature entries are separated by `;`.
- Feature names are case-insensitive.
- Spaces around names, separators, and values are ignored.
- Both `:` and `=` separators are accepted.
- Duplicate `dialogWidth` / `dialogHeight` entries use the last value.
- Unknown fields are ignored for behavior and logged as unsupported.
- Boolean values accept `yes/no`, `true/false`, `on/off`, `1/0`, and empty value as true.
- `timeoutMs` remains spike-only and is not part of legacy compatibility.

### Size

- `dialogWidth` and `dialogHeight` require a `px` suffix for MVP parser compatibility.
- Unitless `dialogWidth` / `dialogHeight` must not be treated as pixels. The measured Edge IE mode result opened screen-sized/maximized.
- Decimal `px` values are truncated toward zero: `500.8px` behaves like `500px`.
- Negative width/height values are treated as invalid, not clamped to the small minimum.
- Invalid width/height values fall back to an IE-mode default-ish dialog size.
- Zero width/height values are clamped to a small minimum.
- Very large width/height values are clamped to the visible screen/maximized area.
- WPF application must distinguish parser behavior from screen-aware window clamping.

### Position And Centering

- Omitted `center` defaults to `center:yes`.
- `center:no` positions the dialog near the browser content area's top-left.
- Explicit `dialogLeft` / `dialogTop` override `center:yes`.
- Explicit positions are honored but observed screen position includes browser/dialog chrome offsets.
- Negative and offscreen positions are clamped into the visible screen area.
- Final WPF position application must be screen-aware and should log any approximation caused by WPF chrome or DPI differences.

### Resizing

- `resizable:yes` allows manual resizing.
- `resizable:no` prevents manual resizing.
- Omitted `resizable` also prevents manual resizing.
- MVP policy maps only explicit `resizable:yes` to resizable WPF windows.

### Status, Scroll, And Unknown Fields

- `status:yes` shows an IE status bar and increases outer height while preserving the measured client viewport.
- `status:no` matches the base no-status-bar size.
- `scroll:yes` matches the base viewport with scrollbars.
- `scroll:no` removes scrollbars and changes the client viewport to the requested dialog size in the measured page.
- MVP should continue to parse and log `status` and `scroll` as unsupported until WPF/WebView2 behavior is deliberately designed.
- Unknown fields do not change measured size or position and should be logged as unsupported.

## Feature Surface

### Size

- `dialogWidth`
- `dialogHeight`

Measured behavior:

- `500px` / `300px` produced `outer=516x339` and `client=483x283`.
- Decimal values were truncated toward zero.
- Duplicate values used the last value.
- Unitless values, invalid values, zero, negative, and huge values do not map to simple pixel application and need policy handling as described above.
- DPI and multi-monitor behavior remain future validation work.

### Position

- `dialogLeft`
- `dialogTop`
- `center`

Measured behavior:

- Omitted `center` behaves like `center:yes`.
- Explicit coordinates override centering.
- Negative and offscreen positions are clamped into the visible screen area.
- Single-monitor 100% scale behavior has been measured; multi-monitor and mixed-DPI behavior remain future validation work.

### Window behavior

- `resizable`
- `status`
- `scroll`

Measured behavior:

- `resizable:yes` enables resizing.
- `resizable:no` and omitted `resizable` disable resizing.
- `status` and `scroll` have visible measured effects but remain unsupported for MVP application.

### Parsing

Measured behavior:

- Names are case-insensitive.
- Spaces around separators are ignored.
- Both `:` and `=` are accepted.
- Duplicate size fields use the last value.
- Unknown fields are ignored and logged.
- Boolean spelling coverage remains implemented by the parser; not every spelling has separate IE-mode evidence.

## Current Parser Behavior

The current parser implements the measured, intentionally small MVP subset. It:

- accepts `;` separated entries
- accepts both `:` and `=` separators
- treats feature names case-insensitively
- removes hyphens from feature names
- requires `px` for `dialogWidth` and `dialogHeight`
- accepts optional `px` for `dialogLeft` and `dialogTop`
- accepts decimal numeric values and truncates them toward zero
- ignores negative `dialogWidth` and `dialogHeight`
- accepts `yes/no`, `true/false`, `on/off`, `1/0`, and empty value booleans
- stores unknown fields for logging
- supports `timeoutMs` as a spike-only diagnostic option
- clamps `timeoutMs` to 1000-90000 ms

The parser receives input only after `DialogFeatureInputPolicy` enforces the limits documented in `docs/dialog-feature-input-boundary.md`.

The parser itself still does not mutate the WPF child window. `DialogFeatureApplicationPolicy` converts parsed values into `DialogWindowOptions`, and the WPF dialog application layer applies the MVP-supported size, position, center, clamp, and resize behavior.

## Reference test matrix

Use `src/ImprovisedEosl.Spike.SyncModal/pages/feature-reference.html` and `src/ImprovisedEosl.Spike.SyncModal/pages/feature-dialog.html` to call `showModalDialog` with controlled feature strings. Record results in `docs/dialog-feature-reference-checklist.md`.

Use `docs/edge-ie-mode-reference-test.md` for the Microsoft Edge IE mode setup procedure.

For each case, record:

- requested feature string
- observed outer window size
- observed inner document viewport size
- observed screen position
- resize ability
- status bar / scroll behavior
- return value behavior
- browser and OS versions
- display scale and monitor layout

Minimum cases:

- `dialogWidth:500px;dialogHeight:300px`
- `dialogWidth:500;dialogHeight:300`
- `dialogWidth: 500px ; dialogHeight : 300px`
- uppercase feature names
- invalid width / height values
- decimal width / height values
- zero and negative width / height values
- very large width / height values
- `center:yes`
- `center:no`
- omitted `center`
- explicit `dialogLeft` / `dialogTop`
- `center:yes` combined with explicit `dialogLeft` / `dialogTop`
- negative and off-screen `dialogLeft` / `dialogTop`
- `resizable:yes`
- `resizable:no`
- omitted `resizable`
- `status:yes` / `status:no`
- `scroll:yes` / `scroll:no`
- duplicate features with different values
- `:` versus `=` separators
- unknown features mixed with known features

## Implementation gates

Before parsed features are applied to the WPF child window:

1. Reference behavior must be measured or explicitly marked unavailable.
2. MVP-compatible expected behavior must be documented for size, position, and resize handling.
3. The feature application design in `docs/dialog-feature-application-design.md` must remain the boundary between parsing and WPF window mutation.
4. Parser tests must be aligned with the documented expected behavior.
5. Window-application tests or repeatable manual checks must cover size, position, and resize behavior.
6. Any intentional mismatch must be logged as `approximated` or `unsupported`.

## MVP tentative decision

For MVP, the highest-value feature behavior is:

1. `dialogWidth` / `dialogHeight`
2. `dialogLeft` / `dialogTop`
3. `center`
4. `resizable`

`status` and `scroll` should remain parsed and logged, but may be approximated or unsupported if WebView2/WPF cannot reproduce their IE presentation cleanly without changing browser security or deeply faking Chromium behavior.
