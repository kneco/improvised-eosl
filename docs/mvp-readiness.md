# MVP readiness

## Decision

The synchronous `window.showModalDialog` experimental MVP is technically complete for the
declared scope and measured environment. The later bounded `window.open()` extension is also
implemented and published in the verified `v0.1.6-mvp` release.

This decision is not a production-readiness claim. The wrapper remains an experimental,
unpackaged WPF application and does not provide full Internet Explorer compatibility,
enterprise deployment, operational support, or a security guarantee.

The broader product MVP defined in this document is implementation-complete for release
`v0.1.7-mvp`. Package and normal-user validation passed before the source was prepared as a clean
public baseline. The public tag workflow and its GitHub Release asset remain a separate publication
gate and must be verified in this repository.

## MVP closure interpretation

Two milestones must not be conflated:

- The synchronization technical MVP is closed. Its gate was synchronous blocking, usable child
  dialogs, return and argument propagation, session sharing, bounded feature parsing, and explicit
  security boundaries.
- The broader product MVP now has the explicit release exit definition below. Open issues are not
  automatically product-MVP blockers merely because they exist.

Portable settings were completed without administrator enforcement: initial URL,
compatibility decisions, and portable import/export share one UI and schema boundary while trusted
profiles remain separate. Top-level close handoff was completed as a bounded same-origin first-child handoff:
`window.close()` does not gain unrestricted native-window authority.

## Broader product-MVP exit definition

The broader product MVP is complete only when all of the following are true:

1. The release-branch changes have been reviewed. If development occurs on a separate feature
   branch, it must also be merged into the release branch. A repository whose default branch is
   itself the sole release branch has no artificial merge requirement.
2. Policy tests, builds, automatic WebView2 modes, and required normal-user manual gates pass and
   their results are recorded.
3. A new version tag is pushed and the GitHub Release workflow succeeds.
4. The expected Windows ZIP is attached to that GitHub Release and its extracted executable is
   verified from a normal user process.
5. Release cleanup is complete and the repository is left with no unintended generated artifacts.

Until those release gates pass, implementation may be feature-complete but the broader product MVP
must not be reported as released or finished.

The selected implementation, release review, local package verification, and cleanup gates
completed before the source was prepared as a clean public `main` baseline. Private development
branches, tags, release objects, issues, and commit identifiers are intentionally not part of this
public repository.

## Public baseline review

The selected release tree was reviewed before it became the public root commit. No
release-blocking code or documentation finding remains.

Review evidence:

- `git diff --check` passed.
- The Debug application build passed with only the existing `NU1900` advisory-source warning.
- All 60 policy checks passed after the final top-level-close and settings changes.
- Settings automatic/manual gates passed, including persistence, invalid-input handling, portable
  import/export, and visible D&D behavior.
- Top-level-close automatic handoff exited `0`; first-consent manual handoff returned modal result
  `617`; the ordinary-popup staging regression exited `0` with observed business DOM progress.
- The locally published `v0.1.7-mvp` self-contained package ran `--auto`,
  `--browser-settings-auto`, `--top-level-close-auto`, and `--top-level-close-popup-auto`
  sequentially from a normal user process; all four modes exited `0`.
- Completed settings and top-level-close work passed their recorded gates. Remaining work is
  classified below as post-MVP.

## v0.1.7-mvp public release verification

- The public `v0.1.7-mvp` tag must point to this repository's clean root commit.
- The public tag-triggered GitHub Actions workflow must complete every release step successfully.
- The public GitHub Release must contain exactly one expected asset,
  `ImprovisedEosl-0.1.7-mvp-win-x64.zip`.
- The locally published extracted executable passed all four required package modes from a normal
  user process with exit code `0`.
- After the public workflow succeeds, verify the uploaded asset and record its digest before
  declaring publication complete. Keep generated local artifacts out of the repository.

## Remaining-work classification

The remaining work was reviewed after settings and top-level-close handoff completed. None of the
following is a blocker for the broader product MVP defined above:

| Work item | Classification | Reason |
| --- | --- | --- |
| Release ZIP structure | Post-MVP packaging improvement | The verified folder-based ZIP is runnable; reducing root clutter does not change the compatibility result. |
| Compatibility-site indicator layout | Post-MVP UI improvement | The textual compatibility state remains visible and sufficient for the MVP security boundary. |
| Hide browser commands/address UI | Post-MVP administrator/operations feature | Current navigation controls are functional; organization-wide enforcement and managed shell policy are outside the user-managed MVP settings boundary. |
| UI-less ActiveX/COM | Explicitly post-MVP | ActiveX/COM remains outside the declared compatibility and security boundary. |
| Brown icon/window visual redesign | Post-MVP visual design | The current application and command icons are functional; branding redesign does not affect MVP behavior. |

Reopening this classification requires an explicit product-scope decision; age alone does not
silently make a work item a release blocker.

## Acceptance priorities

| Priority | Result | Evidence |
| --- | --- | --- |
| 1. Synchronous blocking | Pass | Parent JavaScript remains blocked while child DOM timers and input continue on another STA. Automatic repeated-call and nested-dialog probes pass. |
| 2. Child dialog usability | Pass | Manual keyboard, mouse, resize, close, and native application-modal ownership checks pass. Mixed-DPI and multi-monitor behavior remains a declared environment limitation. |
| 3. Return value propagation | Pass | JSON-compatible `window.returnValue`, native cancellation as `undefined`, rejected payloads, timeout, renderer failure, and browser failure all have distinct outcomes. |
| 4. Dialog argument propagation | Pass | JSON-compatible `window.dialogArguments` passes automatically. Edge IE mode measurements retained strings beyond 4,096 characters; the MVP enforces a 1 MiB UTF-8 safety limit. |
| 5. Session sharing | Pass with boundary | Cookies and `localStorage` share through the common UDF. Separate top-level `sessionStorage` is explicitly not treated as shared. Integrated authentication and client certificates are not part of the measured MVP claim. |
| 6. Feature string parsing | Pass for measured subset | Size, position, center, resize, separators, duplicate values, malformed values, and clamping are measured and implemented. `status` and `scroll` remain parsed and visibly logged as unsupported. |
| 7. UI polish | Sufficient for MVP | Ordinary browsing, address navigation, compatibility state, consent, approval revocation, hidden diagnostics, and startup profiles exist. Visual redesign and graphical profile editing remain post-MVP. |

## Security and lifecycle gates

- Compatibility permission is scoped to normalized HTTP(S) origin and API.
- JavaScript origin claims are checked against the actual current WebView2 document.
- Dialog initial URLs, redirects, and script navigation are restricted to bounded HTTP(S).
- Test-only Host Object methods are registered only for the process-local test origin.
- Parent and child native ownership is restored on close, timeout, initialization failure,
  renderer failure, and browser-process failure.
- Browser-process exit recreates the parent WebView2 and restores the last URL.
- Persistent renderer hangs use a bounded probe and documented last-resort recovery; automatic
  watchdog behavior is test-only.
- Payload and diagnostic logs are bounded.

## MVP tag verification

- [x] Policy tests and all fifteen WebView2 automatic modes passed serially from a clean process state on 2026-06-28.
- [x] Manual child interaction, title-bar X, resize, and ordinary-site browsing checks are recorded in the project test documents.
- [x] The validation environment is recorded below.
- [x] Generated validation output was removed; the test run loaded zero persisted user approvals.

These are release-verification tasks, not missing product implementation.

Validation environment:

- .NET SDK: `8.0.422`
- WebView2 Runtime: `149.0.4022.98`
- OS version string: `Microsoft Windows NT 10.0.26200.0`
- primary display: `1920x1080` logical pixels at `(0,0)`, observed WPF DPI scale `1x1`
- secondary display: `1536x864` logical pixels at `(-1920,0)`

The automatic dialog checks ran on the primary display. Mixed-DPI movement between displays
remains a residual usability risk rather than an MVP tag blocker.

## Accepted residual risks

- The synchronous Host Object design is an unusual WebView2 edge case and could change with a
  future Runtime.
- Same-origin WebViews on separate STAs can still share a Chromium renderer process.
- Unresponsive-event delivery is nondeterministic; production does not impose a generic script
  execution timeout because long synchronous scripts are plausible in target applications.
- Renderer or browser recovery loses in-memory DOM and JavaScript state.
- Foreground activation, mixed DPI, multiple monitors, taskbar grouping, and minimize/restore
  behavior need broader usability testing.
- Multiple wrapper processes sharing one UDF have not been recovery-tested.

## Post-MVP backlog

- graphical profile selection and editing
- visual design and icon integration
- packaging, signing, update, deployment, and support policy
- broader authentication and certificate validation
- mixed-DPI and multi-monitor compatibility matrix
- optional `status` and `scroll` emulation only if a real target application requires them
- migration of the executable from the `Spike.SyncModal` name into the proposed product structure
- ActiveX/COM compatibility; this remains outside the MVP security boundary
- release ZIP root-layout simplification (#11)
- compact compatibility indicator placement (#12)
- administrator-controlled browser-command visibility (#13)
- brown application/window visual redesign (#15)
