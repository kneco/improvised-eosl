# Compatibility origin persistence

## Purpose

User consent for a removed browser API must survive application restarts without broadening the security boundary. User-approved origins are stored independently from temporary test allowances and configured profile defaults.

## Origin identity

An origin is normalized as:

```text
scheme://host:effective-port
```

Rules:

- Only `http` and `https` origins can be approved.
- Scheme and host comparisons are case-insensitive.
- The effective port is always present, including `80` for HTTP and `443` for HTTPS.
- Paths, queries, and fragments do not participate in origin identity.
- File, data, opaque, malformed, and other non-HTTP(S) origins fail closed.
- Approval remains per API. Approval for `window.showModalDialog` does not approve future compatibility APIs.

## Storage

The current WPF implementation stores user approvals at:

```text
%LOCALAPPDATA%\ImprovisedEosl\SyncModalSpike\user-approved-compatibility.json
```

Schema version 1:

```json
{
  "Version": 1,
  "Approvals": [
    {
      "Origin": "https://legacy.example:443",
      "ApiName": "window.showModalDialog"
    }
  ]
}
```

The file is replaced through a temporary file so a partial write does not become the normal stored state.

## Failure behavior

- A missing file means no user-approved origins.
- Invalid JSON or an unsupported schema version is logged and loaded as an empty approval set.
- Invalid origins and unknown APIs in an otherwise valid file are discarded and logged.
- A write failure is logged. The approval remains valid only for the current process.
- Automatic PoC modes use runtime-only allowances and never add them to the user approval file.

## Revocation UI

The browser toolbar exposes a compatibility-specific settings window. It lists only user-approved origin/API pairs and allows selected approvals to be removed without editing JSON manually.

Revocation behavior:

- Changes are staged in the window until Save is chosen.
- A persistence failure leaves the runtime policy unchanged and shows an error.
- Revoking the current page's origin updates the visible compatibility status and reloads that page.
- Temporary automatic-test allowances and configured profile defaults are not shown or modified.
- This UI is intentionally scoped to removed-API compatibility and does not claim to be a general browser site-permissions screen.

Configured profile behavior is documented separately in `docs/compatibility-profiles.md`.
