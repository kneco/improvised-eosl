# Diagnostics

## User interface

The diagnostic panel is hidden by default so the normal browser surface does not permanently reserve space for implementation logs.

- Use the toolbar `診断ログ` button to show the 180-pixel log panel.
- While visible, the button changes to `ログを閉じる`.
- Start with `--show-diagnostics` to make the panel visible immediately.
- Hiding the panel does not stop file logging.

## File logging

The current unpackaged WPF build writes:

```text
<application-directory>\artifacts\sync-modal-poc.log
```

The active file rotates at 5 MiB:

- `sync-modal-poc.log`: current log
- `sync-modal-poc.log.1`: previous log

Only one backup is retained. Rotation and concurrent append operations are protected by an in-process lock.

Parent WebView2 initialization records the Runtime version, OS version string, and active user
data folder so an MVP validation result can be tied to its execution environment.

## Data handling

Diagnostic logs can contain origins, URL paths, feature strings, error details, and truncated JSON values. Query strings and fragments are omitted from child URL logs, and untrusted payload text is truncated to 512 characters, but the log must still be treated as potentially sensitive test data.

Production packaging and retention policy remain out of MVP scope. The current location is appropriate for the unpackaged experimental build, not an enterprise deployment contract.

## Automated checks

- Unit tests verify file rotation and backup replacement behavior.
- Automatic smoke runs log whether the panel initialized visible or hidden.
- Feature smoke passes both with the default hidden state and with `--show-diagnostics`.
