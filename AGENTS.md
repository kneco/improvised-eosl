# AGENTS.md

## Project objective

Build an experimental Windows desktop compatibility wrapper for legacy enterprise web applications.

The first milestone is limited to synchronous `window.showModalDialog()` compatibility.

## Required behavior

Before implementing:

1. Read all files under `docs/`.
2. Identify contradictions, missing assumptions, and WebView2 constraints.
3. Update `docs/implementation-plan.md`.
4. Do not begin broad implementation until the synchronization model is validated.

## Development rules

- Keep the MVP narrow.
- Do not attempt full Internet Explorer compatibility.
- Do not silently add unsupported assumptions.
- Record technical assumptions in documentation.
- Prefer small, reviewable commits.
- Add automated tests where practical.
- Add a manual test page for browser behavior that cannot be reliably unit tested.
- Treat security boundaries explicitly.
- Do not disable browser security features merely to make the demo work.
- Log unsupported behavior instead of pretending it is compatible.
- Keep UI and compatibility logic separated.

## Work-session and context rules

- Continue in the current context while it remains accurate and easy to review.
- Switch to a fresh context when accumulated history, stale assumptions, repeated compaction,
  or unrelated work would make mistakes more likely.
- Do not keep a context merely to preserve conversational continuity when a clean handoff would
  make the next task safer.
- Before switching context, leave a concise handoff that records the base branch, current branch,
  HEAD, completed work, validation results, remaining work, known blockers, and files or changes
  that must not be modified or staged.
- Treat the latest verified repository state as authoritative after a context switch. Re-check
  the branch, HEAD, worktree, open issue, and relevant documentation instead of relying only on
  the handoff narrative.
- Preserve user-owned and unrelated untracked files across context switches.
- Explicitly decide whether to split context before starting a new issue or unrelated workstream.
  Prefer a fresh context after a merge/release/cleanup boundary when the completed session included
  lengthy manual measurements, environment repair, repeated runtime failures, or many compacted
  turns. Continue in the same context only for immediate validation or a tightly related fix.
- Treat a completed release as a strong context boundary: finish repository cleanup, write the
  handoff, then start residual-issue triage in a fresh context unless the current context is still
  short and unambiguous.
- State the context decision in the final report. If splitting, make the next task and its first
  read-only verification command explicit so the new context does not repeat completed work.

## Environment and validation lessons

- A WebView2 failure observed only when launched by an agent tool may be an automation-process
  constraint rather than an application, Runtime, or OS defect. Before repairing the machine,
  compare the same built command from a normal user PowerShell and preserve both logs.
- Do not weaken Chromium/WebView2 security or sandbox flags to make automated validation pass.
  Use a normal user-run command for the manual gate when the agent-launched process is constrained.
- Browser compatibility evidence must distinguish raw legacy input, WebView2-exposed hints, host
  policy, and final visible behavior. Do not infer one layer from another.
- A release is complete only after the tag workflow succeeds and the GitHub Release asset is
  present. A local `dist` ZIP or successful push alone is not release completion.
- After a release asset is verified, ignored local packages and one-off verification binaries may
  be removed; retain source test pages, measurement documentation, and reproducible scripts.

## Go / Stop / Chat reporting

End meaningful work reports with the next-action choices below:

- `Go`: the current result is safe to continue from. State the concrete next action that can be
  performed without additional product or scope decisions.
- `Stop`: work should not continue automatically. State the blocker, failed gate, safety concern,
  or missing authority that requires a pause.
- `Chat`: a design, scope, priority, or tradeoff discussion would improve the next decision. State
  the specific topic to decide rather than asking a generic follow-up question.

Additional reporting rules:

- Present only choices that are genuinely applicable; do not force all three labels into every
  report.
- Lead with the achieved outcome, then list validation and remaining risk briefly.
- Distinguish completed implementation from pending manual verification, external CI, review,
  merge, release, or issue closure.
- Never label a path `Go` when a required validation gate is failing or when proceeding would
  require authority the user has not granted.
- When the user replies with `Go`, continue with the stated next action. It does not silently
  authorize a materially broader scope than the action previously described.
- When the user replies with `Stop`, leave the repository in a safe state and report the exact
  resume point.
- When the user replies with `Chat`, pause mutations and discuss the named decision first.

## MVP acceptance priority

1. Synchronous blocking behavior
2. Child dialog usability
3. Return value propagation
4. Dialog argument propagation
5. Session sharing
6. Feature string parsing
7. UI polish

## Out of scope for MVP

- ActiveX
- NPAPI
- Trident layout reproduction
- old TLS or cipher support
- complete IE DOM compatibility
- production support
- enterprise deployment tooling
