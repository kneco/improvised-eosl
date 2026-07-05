# Dialog navigation security

## Purpose

An approved parent origin may choose a child dialog URL, but it must not use the native wrapper to navigate a child WebView2 to local files or executable URL schemes.

## MVP policy

- Absolute `http` and `https` URLs are allowed.
- URLs are limited to 8192 characters.
- Embedded username/password userinfo is rejected.
- `file`, `data`, `javascript`, and all other schemes are rejected.
- Invalid initial URLs are rejected before child STA or WebView2 creation.
- Every child WebView2 `NavigationStarting` event is validated with the same policy, including redirects and script-initiated navigation.
- Query strings and fragments are omitted from URL log output.

Cross-origin HTTP(S) child URLs remain allowed because legacy `showModalDialog` workflows may use them. The child receives no parent DOM reference; argument and return exchange remains limited to the validated JSON boundary.

Rejected navigation returns:

```json
{
  "kind": "invalid-dialog-url",
  "ok": false,
  "reason": "unsupported-scheme",
  "maxCharacters": 8192
}
```

## Automated checks

- Unit tests cover HTTP(S), unsupported schemes, userinfo, missing values, excessive length, and query-free log formatting.
- `--origin-guard-auto` requests `file:///C:/Windows/win.ini` through the normal shim.
- Expected integration result: `invalid-dialog-url`, one URL-rejection log, and zero child STA starts.
- `--navigation-auto` opens a valid local HTTP child and then attempts `about:blank` navigation.
- Expected runtime-navigation result: `blocked-dialog-navigation`, one child-navigation-block log, and one child STA start.
- The normal `--auto` probe must still complete all HTTP child dialogs without URL rejection.
