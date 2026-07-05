# IE compatibility research review

## Decision

The Cowork research files are useful background material and a good feature-inventory draft,
but they are not adopted wholesale as the compatibility contract. This review adopts the
parts supported by Microsoft documentation, the corrected Edge IE mode measurements, or the
current implementation tests.

Source precedence for this project is:

1. Project-local Edge IE mode measurements for `showModalDialog` feature behavior.
2. Current Microsoft documentation for WebView2 and product lifecycle claims.
3. Current code and automated/manual evidence for implementation-status claims.
4. General browser research as non-normative background.

## Adopted findings

- WebView2 does not support IE mode. Selected legacy behaviors must be implemented explicitly.
- Synchronous blocking, child usability, return propagation, arguments, session sharing, and
  measured dialog features remain the correct MVP priority order.
- `status` and `scroll` have real measured IE effects but remain intentionally unsupported in
  the MVP.
- ActiveX, VBScript, BHO, complete IE DOM emulation, and old document modes remain out of scope.
- Consumer entry points for IE mode were restricted in Edge 141/142 after active exploitation;
  enterprise policy behavior was not removed by those changes.
- Microsoft states that IE mode is supported through at least 2029 on supported operating
  systems, with at least one year of notice before retirement. This is not a fixed 2029
  retirement commitment.
- Multi-monitor and DPI repeatability remain meaningful follow-up risks.

## Corrections not adopted

| Research claim | Project decision |
|---|---|
| Dialog arguments and return values historically support only JSON and have a 4,096-character limit. | Corrected. Microsoft documents `varArgIn` as a `Variant` and permits arrays and other types. Older documentation states that a direct string is truncated after 4,096 characters, but the 2026-06-28 Edge IE mode measurement preserved direct strings through 5,000 characters and also preserved 5,000-character nested strings. JSON and the 1 MiB UTF-8 bound remain explicit safety limits of this MVP. |
| Unitless `dialogWidth` / `dialogHeight` are pixels. | Rejected. The corrected Edge IE mode run opened screen-sized/maximized; the MVP requires `px` for these size fields. |
| Zero dialog size is rejected. | Rejected. The measured IE result used a small minimum; the MVP parses zero and clamps it in application policy. |
| Chromium `window.outerWidth` / `outerHeight` equal the WPF native window bounds including `+16/+39`. | Rejected. WebView2 reported the page-visible values while native WPF bounds were logged separately as `516x339`. |
| Closing with the title-bar X invokes the injected JavaScript `window.close()` and preserves `returnValue`. | Rejected. Native X close currently follows cancellation and returns `undefined`; it does not execute the JavaScript close override. |
| IE11 supports Fetch, Promises, and async/await natively. | Rejected as a general support claim. It must not be used for project scope or compatibility decisions. |
| `document.all` is simply unsupported in Chromium Edge. | Rejected as too broad; modern engines retain special web-compatibility behavior. It is outside this MVP regardless. |
| Windows 10 IE mode ended universally in October 2025. | Rejected. OS editions, LTSC/ESU, and Edge servicing differ; use Microsoft's supported-OS lifecycle guidance. |
| IE mode, MSHTML, Chakra, and ActiveX have a confirmed retirement date at the end of 2029. | Rejected. The official wording is support through at least 2029 with advance notice. |
| WebBrowser receives a final security update in June 2026. | Not substantiated by the reviewed Microsoft sources and not adopted. |
| Recreating generic ActiveXObject or arbitrary COM activation is a suitable next feature. | Rejected. It conflicts with MVP scope and would create a high-risk native execution bridge. |

## Adopted backlog impact

- Keep `status` and `scroll` as measured-but-unsupported diagnostics for the MVP.
- Keep the corrected `px` size parsing and existing clamp policy.
- Preserve separate native WPF and JavaScript-visible window measurements.
- Retain title-bar X cancellation as a distinct manual/automation test case.
- Add broader DPI and multi-monitor checks before claiming close placement compatibility.
- Do not add ActiveX or general COM exposure to the compatibility broker.
- Do not emulate the historical direct-string 4,096-character truncation in the default MVP profile; the current Edge IE mode target preserved every measured direct and nested 5,000-character string.

## Detailed showModalDialog specification review

The Cowork `showModalDialog` detailed-spec draft was reviewed on 2026-06-28 as a useful
edge-case inventory, not as a normative contract. The draft itself was not retained after
review; the adopted decisions and corrections are recorded below.

Adopted and covered by current measurements or tests:

- A title-bar X with no child close message returns JavaScript `undefined`; explicit child `returnValue` plus `window.close()` returns that value.
- Duplicate size fields use the last value.
- `:` and `=` separators may be mixed; empty entries and a trailing semicolon are ignored by the wrapper parser.
- Microsoft-documented Boolean aliases `yes/no`, `on/off`, and `1/0` are accepted. `true/false` and an empty value remain documented wrapper extensions rather than measured IE claims.
- Explicit `dialogLeft` or `dialogTop` overrides centering.
- Unknown fields are ignored for behavior and logged as unsupported.

Not adopted as written:

- The 4,096-character rule must not be applied to the current Edge IE mode contract. Microsoft documents it for a direct string `varArgIn`, but the current reference environment preserved measured direct and nested strings through 5,000 characters.
- Unitless width and height are not treated as pixels in this MVP because the corrected Edge IE mode measurement opened screen-sized, despite older Microsoft documentation describing pixels as the default unit.
- Absolute and relative CSS units are not added to the MVP parser without current Edge IE mode measurements.
- Empty features do not promise content-driven auto-sizing; current default WPF sizing is an approximation.
- Firefox and Chrome comparisons are irrelevant to the Edge IE mode compatibility contract and remain non-normative.

## Reviewed sources

- Microsoft Learn: `Differences between Microsoft Edge and WebView2`
  - https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/browser-features
- Microsoft Lifecycle FAQ: `Internet Explorer and Microsoft Edge`
  - https://learn.microsoft.com/en-us/lifecycle/faq/internet-explorer-microsoft-edge
- Microsoft Browser Vulnerability Research: `Securing the Future: Changes to Internet Explorer Mode in Microsoft Edge`
  - https://microsoftedge.github.io/edgevr/posts/Changes-to-Internet-Explorer-Mode-in-Microsoft-Edge/
- Microsoft Learn previous versions: `showModalDialog method (Internet Explorer)`
  - https://learn.microsoft.com/en-us/previous-versions/ms536759(v=vs.85)
- Project evidence:
  - `docs/dialog-feature-reference-checklist.md`
  - `docs/dialog-feature-compatibility.md`
  - `docs/webview2-reference-page-smoke-test.md`
  - `docs/technical-feasibility.md`

## Cowork source disposition

The four Cowork source drafts were removed after review rather than committed as parallel,
non-normative specifications:

- `ie-compat-required-features.md`: inventory items supported by evidence are represented in the
  implementation plan and compatibility documents.
- `ie-compat-general-research.md`: useful background only; no project decision depends on it.
- `ie-mode-deprecation-roadmap.md`: contained date-specific assertions that require current
  Microsoft lifecycle verification and must not be treated as a stored project contract.
- `showmodaldialog-detailed-spec.md`: adopted edge cases and corrections are recorded in this
  review and the measured compatibility documents.

This review is the retained disposition record. Removing the drafts avoids broken precedence,
duplicate compatibility contracts, and stale lifecycle claims.
