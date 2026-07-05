# `window.open()` feature reference checklist

Use this checklist to measure popup feature behavior in Microsoft Edge IE mode before implementing
feature application in WebView2/WPF. These results are independent from the existing
`showModalDialog` feature measurements.

## Environment

- Date: 2026-07-04
- Tester: user-assisted manual measurement
- OS version:
- Edge / IE mode version:
- IE mode setup path: Edge "Reload in Internet Explorer mode" (exact policy setup not recorded)
- Parent URL: `http://127.0.0.1:18080/window-open-reference-ie.html`
- IE mode indicator visible: yes
- Display scale:
- Monitor layout:
- Build: pre-public-baseline validation build

## Procedure

1. Start the application so its local test server is listening on port `18080`.
2. Open the parent URL above in Microsoft Edge and reload it in IE mode.
3. If the child does not remain in IE mode, also add
   `http://127.0.0.1:18080/window-open-child-ie.html` to the local IE mode page list.
4. Run one row at a time. Confirm the parent remains interactive while the child is open.
5. In the child, manually test resize and inspect visible chrome and scrolling.
6. Fill the observation controls and select `Return observation and close`.
7. Copy the generated checklist rows and result-history JSON below.
8. Run `named-reuse-first` followed by `named-reuse-second` and confirm whether the same native
   window and JavaScript `window.name` are reused.

## Required interpretation

- Do not infer WebView2 behavior from IE mode results.
- Record `yes`, `no`, and omitted separately; do not assume omitted means `yes`.
- Visible browser chrome is a manual observation. JavaScript dimensions alone are insufficient.
- A blocked popup or dummy/closed return object is a result, not a harness failure.
- `width`, `height`, `left`, and `top` are fixed harness controls, not new feature scope.

## Results

| Case | Feature string | Returned window | Parent modeless | Reused | Opening bounds | Closing bounds | Resize | Chrome / scroll observation | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| omitted | `width=640,height=480,left=120,top=80` | true | yes | unknown | 135,278 671x613 client=642x400 | 458,388 984x645 client=967x522 | resizable | location | |
| resizable-yes | `width=640,height=480,left=120,top=80,resizable=yes` | true | yes | unknown | 131,246 660x588 client=635x408 | 74,139 772x643 client=758x542 | resizable | location | |
| resizable-no | `width=640,height=480,left=120,top=80,resizable=no` | true | yes | unknown | not captured | not captured | resizable | location; no scrolling observed | Result reached `done`, but child did not close automatically. |
| scrollbars-yes | `width=640,height=480,left=120,top=80,scrollbars=yes` | true | yes | unknown | 131,246 660x588 client=609x408 | 131,246 660x1135 client=609x955 | resizable | location; scrollbar present; marker reachable | |
| scrollbars-no | `width=640,height=480,left=120,top=80,scrollbars=no` | true | yes | unknown | not captured | not captured | resizable | location; no scrollbar; marker unreachable | Child did not close automatically. |
| location-yes | `width=640,height=480,left=120,top=80,location=yes` | true | yes | unknown | 131,246 660x588 client=635x408 | 131,246 660x1463 client=635x1283 | resizable | location visible | |
| location-no | `width=640,height=480,left=120,top=80,location=no` | true | yes | unknown | not captured | not captured | resizable | location visible | Result return/close failed. |
| menubar-yes | `width=640,height=480,left=120,top=80,menubar=yes` | true | yes | unknown | 131,246 660x588 client=635x408 | 131,246 660x1238 client=635x1058 | resizable | location; no menu bar | |
| menubar-no | `width=640,height=480,left=120,top=80,menubar=no` | true | yes | unknown | not captured | not captured | resizable | location; no menu bar | Child did not close automatically. |
| toolbar-yes | `width=640,height=480,left=120,top=80,toolbar=yes` | true | yes | unknown | 131,246 660x588 client=635x408 | 131,246 1235x1252 client=1211x1072 | resizable | location; no toolbar | |
| toolbar-no | `width=640,height=480,left=120,top=80,toolbar=no` | true | yes | unknown | not captured | not captured | resizable | location; no toolbar | Child did not close automatically. |
| status-yes | `width=640,height=480,left=120,top=80,status=yes` | true | yes | unknown | 131,246 660x588 client=635x371 | 131,246 1315x1309 client=1291x1092 | resizable | location; status bar visible | |
| status-no | `width=640,height=480,left=120,top=80,status=no` | true | yes | unknown | not captured | not captured | resizable | location; no status bar | Child did not close automatically. |
| fullscreen-yes | `width=640,height=480,left=120,top=80,fullscreen=yes` | true | yes | unknown | 131,246 660x588 client=635x408 | 131,246 1402x1117 client=1377x937 | resizable | location; not fullscreen | |
| fullscreen-no | `width=640,height=480,left=120,top=80,fullscreen=no` | true | yes | unknown | not captured | not captured | resizable | location; not fullscreen | Child did not close automatically. |
| channelmode-yes | `width=640,height=480,left=120,top=80,channelmode=yes` | true | yes | unknown | 131,246 660x588 client=635x408 | 131,246 1029x1223 client=1005x1043 | resizable | location; no visible channel-mode change | |
| channelmode-no | `width=640,height=480,left=120,top=80,channelmode=no` | true | yes | unknown | not captured | not captured | resizable | location; no visible channel-mode change | Child did not close automatically. |
| all-no | `width=640,height=480,left=120,top=80,resizable=no,scrollbars=no,location=no,menubar=no,toolbar=no,status=no,fullscreen=no,channelmode=no` | true | yes | unknown | 131,246 660x588 client=635x408 | 131,246 660x885 client=635x705 | resizable | location | |
| all-yes | `width=640,height=480,left=120,top=80,resizable=yes,scrollbars=yes,location=yes,menubar=yes,toolbar=yes,status=yes,fullscreen=yes,channelmode=yes` | true | yes | unknown | not captured | not captured | resizable | opened as an existing Edge-window tab with location, tabs, toolbar, and scrolling | `window.opener` result return and scripted close did not work; browser window was maximized, not fullscreen. |
| named-reuse-second | `width=640,height=480,left=120,top=80,resizable=no` | true | yes | yes | 131,246 660x588 client=635x408 | 589,574 832x965 client=808x785 | resizable | location | Reused `windowOpenNamedReuse`; first remained open while second was requested. |

## Result history JSON

The parent returned structured JSON for the rows with numeric bounds above. Several `no` cases
reached `done` but did not close, while other return paths failed; those observations are recorded
manually rather than inventing missing bounds. The `all-yes` tab did not retain a usable opener.

## Mapping decision

| Feature | Edge IE mode result | WebView2 signal | Decision | Intentional difference / safety rule |
| --- | --- | --- | --- | --- |
| `resizable` | `yes`, `no`, and omitted were all resizable | not separately exposed | approximated: always resizable | Matches measured IE-mode behavior; do not claim literal `no` support. |
| `scrollbars` | `yes` showed scrolling; `no` suppressed it | exposed hint is indistinguishable; bounded raw capture returned the correct nullable Boolean in all 21 cases | supported through bounded raw capture | Apply only after explicit origin permission; omitted remains distinct. |
| `location` | visible for both `yes` and `no` | not separately exposed | unsupported; retain wrapper location UI | Do not hide the wrapper's trusted origin indicator. |
| `menubar` | absent for both isolated values | isolated `yes` and `no` both false; `all-yes` true | approximated from WebView2 hint | Combination behavior remains host-defined. |
| `toolbar` | absent for isolated values; visible when `all-yes` opened as a tab | isolated `yes` and `no` both false; `all-yes` true | approximated from WebView2 hint | Treat tab-vs-popup behavior separately from a toolbar hint. |
| `status` | visible for `yes`, absent for `no` | exposed hint is indistinguishable; bounded raw capture returned the correct nullable Boolean in all 21 cases | supported through bounded raw capture | Apply only after explicit origin permission; omitted remains distinct. |
| `fullscreen` | neither value entered fullscreen | not separately exposed | unsupported | retain visible close path |
| `channelmode` | no visible change for either value | not separately exposed | unsupported | retain visible close path |

## WebView2 observation harness

- Start the unpackaged debug application with `--window-open-observation --show-diagnostics` to
  use an isolated temporary WebView2 user-data folder and navigate directly to the harness.
- `--window-open-observation-auto` requests all 21 cases serially after parent navigation, waits
  for each child navigation, then closes the observation window before continuing. It records raw,
  exposed, resolved, and applied values so the capture and application layers remain distinguishable.
- The handler is restricted to the dedicated local parent and same-origin child paths. Other
  pages retain the existing WebView2 behavior.
- Each request logs sanitized URI, window name, `IsUserInitiated`, `HasPosition`, `Left`, `Top`,
  `HasSize`, `Width`, `Height`, and all four exposed chrome/scrollbar hints.
- The modeless observation window uses the parent's environment/profile and logs WPF bounds at
  load and close. The first measurement run deliberately applied nothing; the final validation
  harness applies only the documented bounded `scrollbars` and `status` policy.
- Runtime observation on 2026-07-04 was blocked before parent navigation by repeated WebView2
  GPU/browser-process crashes (`GpuProcessExited` with `0xC0000022`, followed by an unexpected
  browser-process exit). The same failure recurred after a Windows restart with an isolated UDF
  and `--disable-gpu`. The registered Microsoft Edge Update online-repair path completed
  successfully and updated the Runtime from `149.0.4022.98` to `150.0.4078.48`, but both default
  GPU settings and `--disable-gpu` still produced the same pre-navigation failure. DISM
  `/RestoreHealth` completed successfully, SFC reported no integrity violations, and the failure
  still reproduced after the subsequent Windows restart. This was later isolated to the
  agent-launched process environment by the successful normal-PowerShell runs below.
- Launching the same mode from a normal user PowerShell outside the Codex process environment then
  succeeded with Runtime `150.0.4078.48` and `--disable-gpu`. The omitted case emitted
  `userInitiated=true`, position `120,80`, size `640x480`, and all four exposed display hints as
  `false`. The deliberately unmodified WPF host opened at `182,182 720x520`. This isolates the
  earlier crashes to the Codex-launched process environment rather than the app, OS image, or
  repaired Runtime.
- The full serial run completed all 21 cases on 2026-07-04. Every request was user initiated,
  reported position `120,80` and size `640x480`, navigated successfully, and left the deliberately
  unmodified WPF host at `720x520`. All isolated/omitted/`all-no` cases reported menu bar, status,
  toolbar, and scroll bars as `false`; only `all-yes` reported all four as `true`. Therefore the
  exposed hints alone cannot distinguish isolated `scrollbars=yes` or `status=yes` from `no`.
- A second full serial run validated synchronous bounded raw capture: all 21 events matched the
  immediately preceding call; isolated and combined `scrollbars`/`status` values were recovered
  as the correct `true`, `false`, or omitted value without retaining or logging the raw string.
- Manual application validation passed on 2026-07-04 for distinct `status=yes`/`no` rendering and,
  after rejecting the first snap-back implementation, for `scrollbars=yes`/omitted being usable
  and explicit `scrollbars=no` having neither a visible scrollbar nor scrolling input.
