# Work-session lessons

## WebView2 validation environment

- Agent-launched WebView2 processes repeatedly failed with `GpuProcessExited` / `0xC0000022`, even
  after Windows restart, Runtime repair/update, DISM, SFC, isolated user-data folders, and
  `--disable-gpu`.
- The identical binaries and arguments succeeded from a normal user PowerShell. Treat this as an
  automation environment boundary, not evidence that application or Runtime repair is required.
- When this pattern recurs, stop after one controlled comparison and request the external
  PowerShell gate. Do not repeat OS repair or disable browser security features.

## Compatibility measurement

- Edge IE-mode visible behavior, raw `window.open` feature strings, WebView2 window-feature hints,
  and host-applied behavior are separate evidence layers.
- WebView2 reported all four display hints as false for isolated `yes` cases and true only for the
  combined `all-yes` case. Bounded synchronous raw capture was therefore required for isolated
  `scrollbars` and `status` parity.
- Omitted scrollbars default to visible and usable. Only a valid explicit `scrollbars=no` may hide
  and suppress them, because inaccessible content is a worse failure than excess chrome.
- A visible scrollbar that snaps back after input is not equivalent to disabled or hidden
  scrolling. Manual visual and interaction checks remain required.

## Permission UX and security

- Detect feature-bearing `window.open` before opening a child. Until the origin has a decision,
  return `null`, show consent, and require the operation to be retried.
- Consent offers allow all currently known features, allow a selected subset, or deny all.
  "Allow all" never includes future compatibility APIs.
- Persist grants and denials separately to avoid repeated prompts. Local-file loopback decisions
  remain session-only. Same-origin HTTP(S) child restrictions remain explicit.

## Release and cleanup

- Validate the self-contained package, push release notes, then push the version tag.
- Confirm both the GitHub Actions conclusion and the uploaded ZIP asset/digest before reporting the
  release complete.
- Once the remote asset is verified, local `dist`, verification binaries, merged topic branches,
  and one-off installers are disposable. Keep reproducible scripts and evidence documents.

## Context boundary for the next task

Start the next unrelated issue in a fresh context. Re-check the current branch, HEAD, clean
worktree, open GitHub issues, and the relevant documentation before choosing work. Do not reopen
the completed popup-feature investigation unless repository or runtime evidence has changed.
