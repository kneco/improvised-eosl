# WebView2 reference page smoke test

This guide checks that the project-local dialog feature reference pages work inside the current WPF/WebView2 spike.

This is not an Internet Explorer compatibility measurement. It only verifies that the test harness, consent flow, result capture, and logging are usable before the Edge IE mode reference test.

## Purpose

- Confirm that the `feature-reference.html` page can be reached from the WPF app.
- Confirm that discovery-driven permission works for the local origin.
- Confirm that `Run next pending case` opens dialogs and records results.
- Confirm that `Result history JSON` and `Checklist rows` are populated.
- Confirm that measured dialog window options are logged with `appliedToWpf=true`.

## Steps

Manual smoke:

1. Build the solution.
2. Run the WPF spike app.
3. On the parent test page, click `Open feature reference`.
4. Confirm the address bar shows `http://127.0.0.1:<dynamic-port>/feature-reference.html`.
5. Click `Run next pending case`.
6. If the legacy API consent dialog appears, choose Allow and reload.
7. If the page reloads, click `Open feature reference` again if needed.
8. Click `Run next pending case`.
9. In the child dialog, click `Return measurements`.
10. Confirm the case row changes from `pending` to `done`.
11. Confirm `Last result` contains JSON for the case.
12. Confirm `Result history JSON` contains an array with the case result.
13. Confirm `Checklist rows` contains a Markdown table row.
14. Run at least one more case with `Run next pending case`.
15. Click `Clear page results` and confirm page-local results clear.

Automated feature-application smoke:

1. Build the solution.
2. Run the WPF spike app with `--feature-auto`.
3. Wait for the app to close by itself.
4. Inspect `artifacts/sync-modal-poc.log`.

The automated path opens a fixed subset of feature cases, auto-clicks the child dialog's return path, and finishes through the existing auto-run host object.

Current automated cases:

- `size-px`
- `position-explicit`
- `position-negative`
- `position-offscreen`
- `resizable-yes`
- `resizable-no`
- `status-yes`
- `scroll-no`
- `unknown-mixed`

## Expected behavior

- The first attempted `showModalDialog` call on the local origin may trigger discovery and consent.
- After Allow and reload, the full compatibility shim should run for the same local origin.
- Parent JavaScript should block while the child dialog is open.
- The child dialog should remain interactive.
- Returning measurements should resume the parent page.
- Rows should be marked `done` only for cases that produced a page result.
- `Checklist rows` are convenience output, not final compatibility evidence.
- With `--feature-auto`, each fixed case should return through the synchronous host call and the app should terminate after logging `auto-run finished`.

## Log checks

The app log should include lines similar to:

```text
calculated dialog window options; ... appliedToWpf=true
dialog feature diagnostic: feature=dialogWidth; kind=Applied; ...
```

For this smoke test, `appliedToWpf=true` is expected. Size, position, center, and resize options are now applied to the WPF child window. Exact visual parity with Edge IE mode still requires manual comparison against `docs/dialog-feature-reference-checklist.md`.

For the automated feature-application smoke, recent Chromium JavaScript measurements on the 100% scale single-monitor reference machine were:

| Case | Chromium `window.outer` | Document client size | Chromium screen position |
| --- | --- | --- | --- |
| size-px | 500x300 | 485x300 | 710,402 |
| position-explicit | 500x300 | 485x300 | 128,111 |
| position-negative | 500x300 | 485x300 | 8,31 |
| position-offscreen | 500x300 | 485x300 | 1412,772 |
| resizable-yes | 500x300 | 485x300 | 710,402 |
| resizable-no | 500x300 | 485x300 | 710,402 |
| status-yes | 500x300 | 485x300 | 710,402 |
| scroll-no | 500x300 | 485x300 | 710,402 |
| unknown-mixed | 500x300 | 485x300 | 710,402 |

The automated resize cases prove that the runtime policy chose `CanResize` or `NoResize` in logs; they do not prove manual drag behavior. Manual testing is still required for resize affordance and chrome feel.

The automated `status`, `scroll`, and unknown-field cases prove that unsupported feature diagnostics do not break synchronous return propagation. They are expected to log `Unsupported`; they are not expected to reproduce the Edge IE mode status bar or scrollbar behavior.

Chromium `window.outerWidth` / `window.outerHeight` inside an embedded WebView2 do not necessarily report the containing WPF native window's outer bounds. The runtime therefore logs `child WPF bounds` at load and close. Use those native bounds for visual window-size and placement comparison with Edge IE mode; retain the JavaScript measurements for page-visible behavior and regression detection.

The 2026-06-27 automated run at 100% DPI recorded native WPF bounds of `516x339` for every `dialogWidth:500px;dialogHeight:300px` case while Chromium reported `window.outerWidth=500` and `window.outerHeight=300`. The native bounds match the measured Edge IE mode outer size; no additional frame-size correction is currently needed.

## Manual resize validation

Manual WebView2/WPF validation on 2026-06-27 confirmed the measured resize policy:

| Case | Initial outer size | Closing outer size | Result |
| --- | --- | --- | --- |
| resizable-yes | 500x300 | 834x539 | Manual resize succeeded |
| resizable-no | 500x300 | 500x300 | Manual resize was prevented |
| resizable-omitted | 500x300 | 500x300 | Manual resize was prevented |

The runtime log also recorded `CanResize` for `resizable:yes` and `NoResize` for `resizable:no` and omitted `resizable`. This matches the corrected Edge IE mode reference behavior for the MVP resize policy.

## Manual chrome and close validation

Manual review on 2026-06-27 confirmed that the child uses a standard WPF title bar with a close button, has no extra application header, and does not clip the top of the WebView content. The native WPF bounds were `516x339` at 100% DPI.

Clicking the child page's `Cancel` button returned JavaScript `undefined`, which is the expected cancellation result. The WebView2 reference harness labels this outcome `canceled`; it does not treat the empty measurement fields as a successful measured return. Title-bar X behavior was not exercised in that 2026-06-27 observation and was validated separately on 2026-06-28.

The `--native-close-auto` mode closes the child through WPF `Window.Close()` after successful child navigation, without invoking the injected JavaScript `window.close()` override or posting a return value. It verifies that the synchronous parent call resumes with JavaScript `undefined`. This exercises the same native close path used by the title-bar X; literal pointer interaction is covered by the separate UI check below.

The `--native-x-ui` mode provides that literal UI check: it opens a passive child and waits for its title-bar X. After the click, the parent exits successfully only when the synchronous return is JavaScript `undefined`.

The 2026-06-28 literal title-bar X run passed. The child closed on its own STA thread with `result=undefined`, `ShowDialog` returned `undefined` on the parent host thread, and the parent completed with `{"returnedUndefined":true}`. No injected JavaScript close message was involved.

## Failure notes

- If `window.showModalDialog` returns `undefined` immediately, the local origin is probably still in discovery-only mode. Allow and reload, then run the case again.
- If `Run next pending case` does nothing after consent, confirm the page is still `feature-reference.html`.
- If `Result history JSON` stays empty after returning measurements, the child dialog result path is broken.
- If the child dialog does not open, check the app log for origin gating or navigation failures.
- If the page works in WebView2 but not in Edge IE mode later, treat that as an IE mode setup or legacy API behavior difference, not as a WebView2 smoke-test failure.

## What this test does not prove

- It does not prove IE / Edge IE mode feature-string behavior.
- It does not prove `dialogWidth` or `dialogHeight` are interpreted correctly.
- The automated path alone does not prove manual resize affordance; the 2026-06-27 manual validation above covers the current WPF resize policy.
- It does not prove `dialogLeft`, `dialogTop`, or `center` compatibility across different monitor and DPI configurations.
- It does not prove `status` or `scroll` support.
- It does not by itself prove visual parity with Edge IE mode.
- The automated smoke does not replace Edge IE mode reference measurements.
