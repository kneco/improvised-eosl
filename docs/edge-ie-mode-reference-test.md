# Edge IE mode reference test

This guide explains how to run the dialog feature reference pages in Microsoft Edge IE mode.

The goal is to measure legacy `window.showModalDialog` feature-string and argument-boundary behavior before applying compatibility decisions to the WPF/WebView2 wrapper.

## Microsoft references

- [What is Internet Explorer (IE) mode?](https://learn.microsoft.com/en-us/deployedge/edge-ie-mode)
- [Configure local site list for Internet Explorer (IE) mode](https://learn.microsoft.com/en-us/deployedge/edge-ie-mode-local-site-list)
- [Configure IE mode policies](https://learn.microsoft.com/en-us/deployedge/edge-ie-mode-policies)
- [Enterprise site configuration strategy](https://learn.microsoft.com/en-us/deployedge/edge-ie-mode-sitelist)

Important points from the Microsoft documentation:

- IE mode uses the Trident MSHTML engine from Internet Explorer 11 for configured legacy sites.
- Only configured sites use IE mode; other sites remain normal modern Edge pages.
- Local IE mode testing can be enabled from `edge://settings/defaultBrowser` by allowing sites to be reloaded in Internet Explorer mode.
- The user-local IE mode site list is temporary by default, so it is suitable for reference testing but not product configuration.
- Enterprise Mode Site List policy is the durable enterprise configuration path.

## Recommended manual path

This path avoids editing Group Policy and is best for one-person reference testing.

1. Build and run the WPF spike.
2. In the app, click `Open IE feature reference`.
3. Copy the URL from the app address bar. It should usually be `http://127.0.0.1:18080/feature-reference-ie.html`.
4. Open Microsoft Edge.
5. Go to `edge://settings/defaultBrowser`.
6. Set `Allow sites to be reloaded in Internet Explorer mode` to `Allow`.
7. Paste the copied `feature-reference-ie.html` URL into Edge.
8. Open Edge menu `...`.
9. Choose `Reload in Internet Explorer mode`.
10. Confirm that the IE mode indicator appears near the address bar.
11. Use the per-row `Run` button for the case you want to measure.
12. In the child dialog, click `Return measurements`.
13. Confirm that `Last result` and `Checklist rows` are updated.
14. Record results in `docs/dialog-feature-reference-checklist.md`.

Notes:

- The local test server prefers port `18080` to avoid re-adding IE mode pages on every restart. If that port is unavailable, the app falls back to a dynamic port and the URL must be re-added for that run.
- Keep the WPF spike running while testing in Edge; it owns the local HTTP server.
- If `Reload in Internet Explorer mode` is unavailable, Edge policy or organization management may be disabling local IE mode testing.
- If a case opens normally but `window.showModalDialog` is unavailable, the page is probably not actually in IE mode.
- In Edge IE mode, prefer the per-row `Run` buttons. `Run next pending case` is a convenience control and should not be treated as the authoritative reference-test path.
- If the parent page is in IE mode but the child dialog does not open, add both `feature-reference-ie.html` and `feature-dialog-ie.html` to the IE mode page list for the current local port. The preferred entries are `http://127.0.0.1:18080/feature-reference-ie.html` and `http://127.0.0.1:18080/feature-dialog-ie.html`.
- For the direct-string 4,096-character boundary, use `http://127.0.0.1:18080/argument-reference-ie.html` and record results in `docs/dialog-argument-reference-checklist.md`. Add `argument-dialog-ie.html` to the IE mode page list if the child is not opened in IE mode.

## Current smoke result

Observed in manual testing:

- `feature-reference.html` can be opened in Edge IE mode from the local test server.
- The IE mode indicator appears for the parent page.
- The per-row `size-px` `Run` button opens a child dialog.
- `Return measurements` in the child dialog returns to the parent page.
- A corrected later run showed the child dialog's `Return measurements` and `Cancel` controls may not work reliably in Edge IE mode. Treat numeric child-page measurements as unreliable unless the child page visibly returns data.
- `feature-reference-ie.html` and `feature-dialog-ie.html` were added as simpler IE-mode-safe pages to avoid that child-script reliability issue.
- The IE-safe pages successfully returned measurements for `size-px` and `resizable-no`.

This initial smoke only confirmed that the reference-test path was usable. The later completed
measurements and final MVP decisions are recorded in `docs/dialog-feature-reference-checklist.md`
and `docs/dialog-feature-compatibility.md`.

## Enterprise Mode Site List path

This path is closer to how enterprise sites are normally configured, but it requires policy configuration.

1. Create an Enterprise Mode Site List XML containing the exact local test URL or a stable test host.
2. Configure Microsoft Edge policy `Configure Internet Explorer integration` to `Internet Explorer mode`.
3. Configure Microsoft Edge policy `Configure the Enterprise Mode Site List` to point to the XML file.
4. Restart Edge or wait for policy refresh.
5. Navigate to the test URL.
6. Confirm that the IE mode indicator appears.

For this project, the local-site-list path is enough for early feature measurement. Use the Enterprise Mode Site List path only if local reload testing is blocked or if repeatable enterprise-style evidence is needed.

## What to record

For each run, record:

- Windows version
- Microsoft Edge version
- Whether local site list or Enterprise Mode Site List was used
- URL used for the test
- Display scale
- Monitor count and layout
- Whether the IE mode indicator was visible
- Each case result from `feature-reference.html`
- Any visible behavior not captured by the JSON measurement

## Known limits

- Edge IE mode is a supported compatibility surface, but it is not identical to every historical IE deployment.
- The spike prefers localhost port `18080`. Dynamic localhost ports are used only as fallback and remain awkward for Enterprise Mode Site List testing.
- `file://` testing is not preferred because local-file security behavior may differ from HTTP-hosted enterprise applications.
- Measurements involving outer window size, client viewport size, and DPI can vary by OS theme, display scale, and Edge/IE mode version.

## Work that can proceed before manual reference testing

While waiting for manual IE mode results, it is safe to:

- keep parser changes provisional
- improve documentation
- add measurement-only diagnostics to the current WebView2 spike
- prepare parser tests that encode current assumptions as temporary expectations
- design the WPF feature application layer behind an adapter without enabling it

Do not:

- claim the current parser is IE-compatible
- apply size or position features as final behavior
- remove unknown or malformed feature logging
- treat `status` or `scroll` as faithfully implemented without evidence
