# Dialog feature reference checklist

Use `src/ImprovisedEosl.Spike.SyncModal/pages/feature-reference-ie.html` to measure `window.showModalDialog` feature-string behavior in Edge IE mode before applying parsed features to WPF child windows.

Use `src/ImprovisedEosl.Spike.SyncModal/pages/feature-reference.html` only for the richer WebView2 harness.

Run the page in each reference environment first, then repeat in the WebView2 spike after implementation.

See `docs/edge-ie-mode-reference-test.md` for Microsoft Edge IE mode setup steps.
See `docs/webview2-reference-page-smoke-test.md` for a project-local WebView2 harness check that does not produce IE compatibility evidence.

## Environment

- Date:
- Tester:
- OS version:
- Browser / IE mode version:
- IE mode setup path: local site list / Enterprise Mode Site List / other
- Test URL:
- IE mode indicator visible:
- Display scale:
- Monitor layout:
- App build or commit:
- Notes:

## How to run

1. Open `feature-reference.html` from the local test server or a trusted local site configured for Edge IE mode.
2. Run one case at a time. In Edge IE mode, prefer each row's `Run` button.
3. In the dialog, observe size, position, chrome, resize behavior, status bar, and scroll behavior.
4. Add visible notes in the dialog if needed.
5. Click `Return measurements`.
6. Use each row's `Run` button to continue through the matrix. `Run next pending case` is a convenience control and is not the authoritative IE mode path.
7. Copy `Last result` for raw evidence if needed.
8. Copy `Checklist rows` into this document, then fill any visual notes that text output cannot capture.

## Results

| Case | Feature string | Outer size | Viewport size | Position | Resize behavior | Status / scroll behavior | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| size-px | `dialogWidth:500px;dialogHeight:300px` | 516x339 | 483x283 | 718,421 | | | IE-safe page returned; parentScreen=42,119 |
| size-unitless | `dialogWidth:500;dialogHeight:300` | 1920x1080 | 1887x1024 | 8,31 | | | IE-safe page returned; unitless size opened screen-sized/maximized in this environment |
| size-spaces | `dialogWidth: 500px ; dialogHeight : 300px` | 688x433 | 673x433 | 198,253 | | | Edge IE mode returned; closingOuter=688x433 |
| size-uppercase | `DIALOGWIDTH:500px;DIALOGHEIGHT:300px` | 688x433 | 673x433 | 250,305 | Size change was possible during manual test | | Edge IE mode returned; closingOuter=815x525 |
| size-invalid | `dialogWidth:abc;dialogHeight:def` | 520x559 | 487x520 | 716,311 | | vertical scrollbar was longer than usual | IE-safe page returned; invalid size used IE-mode default-ish dialog size |
| size-decimal | `dialogWidth:500.8px;dialogHeight:300.2px` | 516x339 | 483x283 | 718,421 | | | IE-safe page returned; decimal values behaved like 500px/300px in this environment |
| size-zero | `dialogWidth:0px;dialogHeight:0px` | 266x139 | 233x100 | 968,571 | | | IE-safe page returned; zero values were clamped to a small minimum |
| size-negative | `dialogWidth:-500px;dialogHeight:-300px` | 520x559 | 487x520 | 716,311 | | | IE-safe page returned; negative size behaved like invalid size |
| size-huge | `dialogWidth:5000px;dialogHeight:3000px` | 1920x1080 | 1887x1024 | 8,31 | | | IE-safe page returned; huge size opened screen-sized/maximized in this environment |
| center-yes | `dialogWidth:500px;dialogHeight:300px;center:yes` | 516x339 | 483x283 | 718,421 | | | IE-safe page returned |
| center-no | `dialogWidth:500px;dialogHeight:300px;center:no` | 516x339 | 483x283 | 38,61 | | | IE-safe page returned; visually appeared near the browser content area's top-left |
| center-omitted | `dialogWidth:500px;dialogHeight:300px` | 516x339 | 483x283 | 718,421 | | | IE-safe page returned twice; after moving the previous dialog, the next omitted-center dialog still opened centered |
| position-explicit | `dialogWidth:500px;dialogHeight:300px;dialogLeft:120px;dialogTop:80px` | 516x339 | 483x283 | 128,111 | | | IE-safe page returned; requested left/top 120,80 produced observed screenLeft/screenTop 128,111 |
| position-center-and-explicit | `dialogWidth:500px;dialogHeight:300px;center:yes;dialogLeft:120px;dialogTop:80px` | 516x339 | 483x283 | 128,111 | | | IE-safe page returned; same observed position as position-explicit, so explicit left/top override center:yes |
| position-negative | `dialogWidth:500px;dialogHeight:300px;dialogLeft:-200px;dialogTop:-100px` | 516x339 | 483x283 | 8,31 | | | IE-safe page returned; negative position was clamped near the visible screen top-left |
| position-offscreen | `dialogWidth:500px;dialogHeight:300px;dialogLeft:5000px;dialogTop:3000px` | 516x339 | 483x283 | 1412,772 | | | IE-safe page returned; offscreen position was clamped near the visible screen bottom-right |
| resizable-yes | `dialogWidth:500px;dialogHeight:300px;resizable:yes` |  |  |  | Size change was possible during corrected Edge IE mode test | | Corrected Edge IE mode run returned, but child measurement buttons did not work |
| resizable-no | `dialogWidth:500px;dialogHeight:300px;resizable:no` | 516x339 | 483x283 | 718,421 | Size change was not possible during corrected Edge IE mode test | | IE-safe page returned; parentScreen=42,119 |
| resizable-omitted | `dialogWidth:500px;dialogHeight:300px` | 516x339 | 483x283 | 718,421 | Size change was not possible during corrected Edge IE mode test | | IE-safe page returned |
| status-yes | `dialogWidth:500px;dialogHeight:300px;status:yes` | 516x363 | 483x283 | 718,421 | | status bar visible; outer height increased by 24px | IE-safe page returned |
| status-no | `dialogWidth:500px;dialogHeight:300px;status:no` | 516x339 | 483x283 | 718,421 | | no status bar; same as base size | IE-safe page returned |
| scroll-yes | `dialogWidth:500px;dialogHeight:300px;scroll:yes` | 516x339 | 483x283 | 718,421 | | scrollbars present / base viewport size | IE-safe page returned |
| scroll-no | `dialogWidth:500px;dialogHeight:300px;scroll:no` | 516x339 | 500x300 | 718,421 | | scrollbars removed; client size matched requested dialog size | IE-safe page returned |
| duplicate-size | `dialogWidth:400px;dialogWidth:700px;dialogHeight:250px;dialogHeight:450px` | 716x489 | 683x433 | 618,346 | | | IE-safe page returned; duplicate width/height used the last values |
| equals-separator | `dialogWidth=500px;dialogHeight=300px;center=yes` | 516x339 | 483x283 | 718,421 | | | IE-safe page returned; equals separator behaved like colon separator for measured width/height/center |
| unknown-mixed | `dialogWidth:500px;dialogHeight:300px;foo:bar;edge:legacy` | | | | | | |

## Raw result history JSON

Paste the `Result history JSON` field here when useful.

## Compatibility decision notes

- Width / height policy: IE-safe page observations show `dialogWidth:500px;dialogHeight:300px` produced outer size `516x339` and client size `483x283` at 100% display scale. Unitless `dialogWidth:500;dialogHeight:300` produced outer size `1920x1080` and client size `1887x1024`, so the spike must not treat unitless size values as pixels. Decimal `500.8px` / `300.2px` behaved like `500px` / `300px`, so the parser truncates decimal size values. Duplicate width/height values used the last value. Negative sizes behaved like invalid sizes, so the parser ignores negative width/height values. Zero sizes were clamped to a small minimum, and huge sizes opened screen-sized/maximized; exact WPF mapping still needs application-policy work.
- Left / top policy: IE-safe page observation for `dialogLeft:120px;dialogTop:80px` produced observed screen position `128,111`. Adding `center:yes` to the same explicit position also produced `128,111`, so explicit left/top override centering. Negative position `-200,-100` was clamped to `8,31`; offscreen position `5000,3000` was clamped to `1412,772` for a `516x339` outer window on a `1920x1080` screen. The observed positions suggest visible screen position includes browser/dialog chrome offsets, and final WPF mapping needs screen-aware clamping. `dialogLeft` / `dialogTop` unitless handling still needs direct measurement before changing parser policy.
- Centering policy: IE-safe page observation for `center:yes` produced observed position `718,421`. `center:no` produced observed position `38,61` and visually appeared near the browser content area's top-left, not centered. Omitted `center` produced observed position `718,421` twice and stayed centered after manually moving the previous dialog, so the spike treats omitted `center` as `center:yes`. Explicit `dialogLeft` / `dialogTop` take precedence over `center:yes`.
- Resizable policy: Corrected manual Edge IE mode observations confirmed that `resizable:yes` allowed resizing, `resizable:no` prevented resizing, and omitted `resizable` also prevented resizing. Current spike policy maps omitted `resizable` and `resizable:no` to `NoResize`.
- Status policy: `status:yes` made the status bar visible and increased outer height from `339` to `363` while the client viewport stayed `483x283`. `status:no` matched the base no-status-bar size. The spike still logs `status` as unsupported until WPF chrome/status-bar emulation is deliberately implemented.
- Scroll policy: `scroll:yes` matched the base viewport `483x283`; `scroll:no` removed scrollbars and produced client size `500x300` while outer size stayed `516x339`. The spike still logs `scroll` as unsupported until faithful WebView2/page-level behavior is designed.
- Parser differences to correct: `dialogWidth` and `dialogHeight` now require a `px` suffix in the spike parser. `dialogLeft` and `dialogTop` unit handling still needs direct IE-safe measurement.
- Separator policy: IE-safe page observation for `dialogWidth=500px;dialogHeight=300px;center=yes` produced the same size and centered position as the colon-separated `center:yes` case, so the parser accepts both `:` and `=` separators.
- Behavior that will be logged as approximated:
- Behavior that will be logged as unsupported: `status`, `scroll`, unknown fields.
