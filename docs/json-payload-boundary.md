# JSON payload boundary

## Purpose

The synchronous host-object call must not accept unbounded or malformed payloads. The MVP supports JSON-compatible arguments and return values only; it does not preserve JavaScript object identity, prototypes, functions, DOM objects, host objects, or cyclic graphs.

## Limits

- Maximum serialized arguments: 1 MiB in UTF-8.
- Maximum serialized return value: 1 MiB in UTF-8.
- Maximum JSON depth: 64.
- JSON comments and trailing commas are rejected.
- JavaScript `undefined` is accepted only as the dialog return sentinel.

These are MVP safety limits, not historical Internet Explorer limits. A future compatibility profile may make the byte limit configurable, but it must retain a finite upper bound.

Microsoft's previous-version documentation states that a direct string `varArgIn` is truncated after 4,096 characters. It also defines `varArgIn` as a `Variant` that can carry arrays and other values. The 2026-06-28 Edge IE mode reference run did not reproduce that historical truncation: direct strings of 4,097 and 5,000 characters and 5,000-character strings nested in an object and array all arrived intact with their end markers. The MVP therefore retains its 1 MiB UTF-8 safety boundary and does not emulate 4,096-character truncation for the current target.

## Arguments

The parent shim applies `JSON.stringify(args ?? null)`. If JavaScript cannot produce JSON, the shim throws before entering native code. Native code independently validates size, syntax, and depth before starting the child STA.

Rejected native arguments return a structured non-success value:

```json
{
  "kind": "invalid-arguments",
  "ok": false,
  "reason": "too-large",
  "utf8Bytes": 1048580,
  "maxUtf8Bytes": 1048576
}
```

No child window is created for rejected arguments.

## Return values

The injected child compatibility script replaces `window.close()` with a function that serializes the current `window.returnValue` and posts it to the host. This supports unchanged legacy code:

```javascript
window.returnValue = { accepted: true };
window.close();
```

The host independently validates the serialized return value. Cyclic, malformed, excessively deep, or oversized return values become a structured `return-value-rejected` result. Closing without assigning `window.returnValue` returns JavaScript `undefined`.

Child WebView messages are treated as untrusted transport input. Non-object messages, non-string kinds, and non-string serialized return fields are ignored or rejected without throwing through the WebView2 event callback.

## Logging

Payload logs include UTF-8 byte counts and truncate content after 512 characters. Rejection logs record the reason and size without writing the full untrusted payload.

## Automated checks

- Unit tests cover valid JSON, malformed JSON, oversized JSON, and the `undefined` sentinel.
- Unit tests and `--payload-auto` lock the measured decision that a direct 5,000-character string remains accepted and reaches the child intact rather than being silently truncated.
- `--payload-auto` verifies that oversized arguments are rejected before child STA creation.
- `--payload-auto` verifies that a cyclic child return value is rejected and the parent synchronous call still returns.
- Existing `--auto`, `--session-auto`, and `--feature-auto` pages now use `window.returnValue` plus `window.close()` rather than a project-specific close helper.
