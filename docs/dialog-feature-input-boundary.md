# Dialog feature input boundary

## Purpose

The legacy feature string is untrusted input processed synchronously on the parent host thread. Even ignored or unknown fields consume split, parse, diagnostic, and log work, so the input must have finite limits before parsing.

## MVP limits

- Maximum feature string size: 16 KiB in UTF-8.
- Maximum semicolon-delimited entries: 128.
- Null is normalized to an empty feature string.
- Rejected feature input does not create a child STA or WebView2.
- Feature logs use the same 512-character truncation and UTF-8 byte-count format as JSON payload logs.

These are wrapper safety limits, not measured Internet Explorer limits. Normal legacy feature strings are expected to be far below both thresholds.

Rejected input returns:

```json
{
  "kind": "invalid-dialog-features",
  "ok": false,
  "reason": "too-large",
  "utf8Bytes": 16385,
  "entryCount": 0,
  "maxUtf8Bytes": 16384,
  "maxEntries": 128
}
```

## Automated checks

- Unit tests cover ordinary input, null input, excessive UTF-8 size, and excessive entry count.
- `--payload-auto` submits an oversized feature string through the normal shim.
- The expected result is `invalid-dialog-features`, one feature-rejection log, and no additional child STA.
- All normal automatic modes must complete without feature rejection.
