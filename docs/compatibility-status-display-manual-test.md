# Compact compatibility status manual test

## Latest partial result

Passed in a normal user session on 2026-07-07:

- basic layout;
- keyboard operation and the complete detail window;
- undecided to detected to allowed transition;
- explicit denial display;
- status update after revocation; and
- status update after an origin change.

The review identified one layout correction: the status control between the address field and Go
split one navigation operation into two visual groups. The corrected order is address field, Go,
compatibility status, Settings, and Diagnostics. The corrected Release build was restarted and
the new order passed visual confirmation in the normal user session on 2026-07-07.

Passed through Windows UI Automation on 2026-07-07 at the current desktop scale:

- the status button rendered on the address row without overlap at a restored window width of
  approximately 705 pixels;
- the former full-width status row was absent;
- the status button exposed the complete normalized origin and enabled/denied/detected API lists
  through UI Automation;
- pointer activation opened an owned detail window, and the complete text wrapped without the
  clipping observed in the rejected native MessageBox implementation; and
- the detail window exposed a focused Close action and could be dismissed with Escape.

This is not the complete manual gate. Screen-reader announcement, Windows high contrast and
light/dark themes, multiple resolutions, and 100%/150%/200% DPI checks below are explicitly
deferred to a later normal-user test session.

Run this checklist from a normal user PowerShell. Agent-launched WebView2 processes are not a
reliable UI validation environment on every machine.

## Start

```powershell
dotnet run --no-build --project src\ImprovisedEosl.Spike.SyncModal\ImprovisedEosl.Spike.SyncModal.csproj
```

Use the built-in compatibility test page and at least one ordinary HTTP(S) site. Do not weaken
WebView2 security or sandbox settings for this check.

## Layout and detail access

1. Confirm that the compatibility indicator is on the same row as the address field and that the
   former full-width status row no longer consumes browser height.
2. Confirm that the address field and Go remain adjacent, followed by the compatibility indicator.
3. Resize the main window until the address field reaches its minimum practical width. Confirm the
   short status label and command buttons do not overlap the editable URL.
4. Tab to the status indicator. Confirm that focus is visible and Enter or Space opens a detail
   dialog without changing compatibility permission.
5. Confirm the same complete detail is available from the tooltip but does not require hover.

## Policy-state transitions

For every state, confirm that icon shape and short text both change and that the detail includes
the normalized origin plus enabled, denied, and detected API lists.

1. Open an untouched ordinary origin and confirm `互換: 未決定`.
2. Trigger a known legacy API and confirm `互換: 検出済み` before choosing a decision.
3. Allow the detected API, reload, and confirm `互換: 有効` with the exact enabled API.
4. Revoke the decision in Settings and confirm the indicator returns to the applicable undecided
   or detected state after reload.
5. Deny a detected API and confirm `互換: 拒否` with the exact denied API.
6. Use the test/profile fixtures to create multiple enabled APIs and mixed allow/deny decisions;
   confirm no API is omitted from the detail.
7. Navigate to local/opaque content where applicable and confirm `互換: ブロック` is distinct
   from an explicit denial.

## Navigation and operational states

1. Navigate and redirect between two origins. Confirm status detail follows the actual current
   normalized origin and never retains the previous origin's decision.
2. During startup, confirm `互換: 確認中` does not imply Allow or Deny.
3. If using the documented browser-recovery fixture, confirm recovery also shows
   `互換: 確認中`; a failed recovery must show `互換: エラー` and explicitly state that it is not
   a permission decision.

## Accessibility and display environments

1. With a Windows screen reader, focus the indicator and confirm it announces the state,
   normalized origin, and enabled/denied/detected APIs.
2. Check Windows high contrast and light/dark system themes. State meaning must remain available
   from icon shape and text without relying on color.
3. Check 100%, 150%, and 200% display scale. Confirm icon strokes, text, focus indication, tooltip,
   and detail dialog are readable and not clipped.

Record OS, WebView2 Runtime, display scale, window width, screen reader, and observed results.
