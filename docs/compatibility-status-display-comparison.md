# Compatibility status display comparison

## Decision to make

Issue #2 asks for a more compact compatibility indicator near or inside the address bar. The
existing implementation plan also says that a status icon must not replace the textual state.
These requirements are compatible if "textual state" means a persistent, human-readable state
label rather than the current full diagnostic sentence and origin on a separate toolbar row.

This work changes only how an existing `CompatibilityStatus` is presented. It must not change
origin normalization, approval policy, host-object exposure, compatibility API scope, or logging.
The indicator is a reflection of the native policy result, not a security boundary.

## Information that must survive compaction

The current display combines three kinds of information:

1. policy state: off, permission needed, enabled, or blocked;
2. enabled compatibility API: `showModalDialog`, `window.open` features,
   `window.close` handoff, or multiple known APIs; and
3. the normalized origin to which that result applies, including an opaque origin.

The address bar shows the current document URL, but it is not a substitute for associating a
compatibility result with its normalized origin. Redirects, default ports, opaque documents, and
status updates during navigation make that association worth preserving explicitly.

There is also a current representation gap: for an ordinary HTTP(S) origin, `GetStatus()` does
not distinguish a persisted explicit denial from an origin where no API has been detected or
decided. Both can be displayed as `Compatibility: off`. Issue #2 requires denial, undecided, and
detected/pending states not to be ambiguous. The compact view therefore needs a structured
presentation result that preserves the policy's existing denial information; it must not infer
denial by parsing a label or create a new denial rule.

## Options

| Option | Persistent visual content | Origin/API detail | Accessibility | Layout cost | Risk |
| --- | --- | --- | --- | --- | --- |
| A. Keep the separate full-width text row | Full English diagnostic sentence and origin | Always visible | Existing text is directly announced | Highest; permanently reduces WebView height | Low implementation risk, but does not meet the compact-layout objective |
| B. Icon only | State icon | Tooltip only | An accessible name can be added, but sighted keyboard and touch users can miss detail | Lowest | Reject: meaning depends too heavily on icon/color and conflicts with the existing text requirement |
| C. Icon plus short state label adjacent to the address bar | Icon and a stable label such as `互換: 未決定`, `互換: 検出済み`, `互換: 有効`, or `互換: 拒否` | Exact normalized origin and decided/detected API names in accessible description and an on-demand detail surface | Focusable status control with an accessible name containing state, origin, and APIs; icon/color are redundant cues | Low | Recommended: meets compaction without discarding the textual state |
| D. Put icon and text inside the editable address field | Icon and short label overlaid within the URL control | Same as C | Requires custom focus, hit-testing, text-selection, high-contrast, and screen-reader behavior | Lowest apparent chrome, highest control complexity | Defer: tightly couples trust information to URL editing and creates avoidable WPF accessibility risk |

## Recommended contract

Use option C. Place one compact status control next to the address field on the existing command
row and remove the separate full-width status row only after the replacement passes validation.

The persistent visible label communicates the policy category:

| Native policy result | Short visible label | Required detail |
| --- | --- | --- |
| no detection, grant, or denial | `互換: 未決定` | normalized origin; no API detected and no user decision recorded |
| legacy API detected and decision pending | `互換: 検出済み` | normalized origin; detected API names; permission decision is pending |
| one or more APIs allowed | `互換: 有効` | normalized origin; every enabled API name |
| one or more APIs explicitly denied and none allowed | `互換: 拒否` | normalized origin; every explicitly denied API name |
| origin cannot participate in compatibility policy, such as an opaque origin | `互換: ブロック` | origin value and bounded reason |
| initialization or browser recovery | `互換: 確認中` | bounded operational explanation; do not imply an allow or deny result |
| browser recovery failed | `互換: エラー` | bounded failure explanation; do not present this as policy denial |

The short Japanese labels are presentation strings, not replacements for the existing policy
labels in `ImprovisedEosl.Core`. Product copy should continue to use resource-style UI keys.

The control must:

- retain a readable text label at all times; icon shape and color are supplementary;
- expose an accessible name or description containing the complete state, normalized origin, and
  relevant API names;
- be keyboard-focusable if the complete detail is available through interaction;
- provide the same complete detail without requiring pointer hover alone;
- use state-specific icon geometry in addition to color, with usable Windows high-contrast output;
- update from the same native `CompatibilityStatus` source used today;
- avoid offering Allow, Deny, or Revoke directly in this first layout change; existing consent and
  settings flows remain authoritative.

## State-model prerequisite

`CompatibilityStatus` currently exposes only `Origin` and an English `Label`. The UI would have
to parse that label to distinguish short states and API names, which would make localization text
an accidental policy interface. It also cannot distinguish an explicit denial from an untouched
HTTP(S) origin. Before changing the layout, extend the presentation input with structured,
non-visual state plus enabled, denied, and detected API data while preserving the existing
diagnostic label where compatibility with logs or tests requires it. The structured result belongs
in Core; icon selection and localized short labels belong in the WPF shell.

This is a representation change, not a new policy decision. Existing allow, deny, configured
grant, pending detection, and opaque-origin behavior remains authoritative.

## Validation comparison

Automated policy tests should cover the structured representation for:

- untouched HTTP(S) origin with no detection or decision;
- pending detection;
- each individually enabled API;
- multiple enabled APIs;
- each individually denied API and multiple denied APIs;
- mixed per-API allow and deny decisions without collapsing either list;
- opaque origin; and
- transitions after Allow, Deny, and Revoke.

WPF-level checks should verify that every state maps to a non-empty localized short label,
state-specific icon, and complete accessible text without parsing the diagnostic label.

Manual validation is still required for:

- navigation and redirects between origins;
- Allow and reload, Deny, and Revoke transitions;
- keyboard focus and access to full detail without hover;
- screen-reader announcement of state, normalized origin, and enabled APIs;
- 100%, 150%, and 200% display scale, narrow-window layout, Windows high contrast, and light/dark
  Windows themes; and
- initialization, browser recovery, and browser-recovery-failure status without a misleading
  compatibility policy indication.

## Explicit non-goals

- changing compatibility consent or revocation behavior;
- treating the indicator as proof that page content is trustworthy;
- hiding the address bar or browser commands;
- adding administrator-enforced policy;
- redesigning the application color palette or general command icons; and
- expanding the supported compatibility APIs.
