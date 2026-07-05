# Dialog argument reference checklist

Use `src/ImprovisedEosl.Spike.SyncModal/pages/argument-reference-ie.html` in Microsoft Edge IE mode to measure the historical direct-string boundary separately from object and array arguments.

Microsoft's previous-version documentation says a direct string `varArgIn` is limited to 4,096 characters and longer strings are truncated. It defines `varArgIn` as a `Variant`, so this checklist does not assume that nested strings in objects or arrays share that limit.

## Environment

- Date: 2026-06-28
- Tester: project owner
- OS version:
- Browser / IE mode version:
- Test URL: `http://127.0.0.1:18080/argument-reference-ie.html`
- IE mode indicator visible: yes
- App build or commit: pre-public-baseline validation build
- Notes: All direct and nested strings retained their requested length and end marker.

## Procedure

1. Start the WPF spike so the local server is listening on port `18080`.
2. Open `http://127.0.0.1:18080/argument-reference-ie.html` in Edge IE mode.
3. Run each row separately.
4. In the child dialog, confirm the displayed measurement and click `Return measurement`.
5. Paste the generated checklist rows below.

If the child does not remain in IE mode, add both exact URLs to the local IE mode page list:

- `http://127.0.0.1:18080/argument-reference-ie.html`
- `http://127.0.0.1:18080/argument-dialog-ie.html`

## Results

| Case | Argument kind | Requested length | Result |
| --- | --- | ---: | --- |
| string-4000 | string | 4000 | kind=string;receivedLength=4000;suffix=xxxxxxxxxx\|END:4000\| |
| string-4096 | string | 4096 | kind=string;receivedLength=4096;suffix=xxxxxxxxxx\|END:4096\| |
| string-4097 | string | 4097 | kind=string;receivedLength=4097;suffix=xxxxxxxxxx\|END:4097\| |
| string-5000 | string | 5000 | kind=string;receivedLength=5000;suffix=xxxxxxxxxx\|END:5000\| |
| object-string-5000 | object | 5000 | kind=object;receivedLength=5000;suffix=xxxxxxxxxx\|END:5000\| |
| array-string-5000 | array | 5000 | kind=array;receivedLength=5000;suffix=xxxxxxxxxx\|END:5000\| |

## Decision criteria

- Direct strings and strings nested in objects and arrays were measured separately.
- Historical truncation would be emulated only if it appeared in the current Edge IE mode target.
- A direct-string result would not be generalized to JSON objects or arrays without evidence.

## Decision

The measured Edge IE mode environment did not reproduce the historical 4,096-character truncation documented for a direct string `varArgIn`. Direct strings of 4,097 and 5,000 characters, plus 5,000-character strings nested in an object and array, arrived intact with their end markers.

The MVP therefore retains its 1 MiB UTF-8 JSON safety boundary and does not add 4,096-character truncation. The older Microsoft statement remains useful historical documentation, but it is not the compatibility behavior observed for the current Edge IE mode target.
