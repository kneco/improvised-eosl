# MVP: synchronous `showModalDialog()`

## Goal

Implement a minimal compatibility layer that allows existing JavaScript to call:

```javascript
const result = window.showModalDialog(url, arguments, features);
```

without rewriting the caller to asynchronous code.

## Required semantics

The MVP must attempt to preserve the following behavior:

1. The parent JavaScript call blocks.
2. A child dialog opens.
3. `window.dialogArguments` is available in the child.
4. The child can assign `window.returnValue`.
5. Closing the child returns that value synchronously.
6. The parent continues from the original call site.

## Proposed JavaScript shim

```javascript
window.showModalDialog = function (url, args, features) {
  const serialized =
    chrome.webview.hostObjects.sync.ieCompat.showModalDialog(
      new URL(url, location.href).href,
      JSON.stringify(args ?? null),
      String(features ?? "")
    );

  return serialized == null
    ? undefined
    : JSON.parse(serialized);
};
```

## Proposed host flow

```text
Parent WebView2 JavaScript
        |
        | synchronous host-object call
        v
C# compatibility host
        |
        | starts child UI on separate STA thread
        v
Child WebView2 dialog
        |
        | returns serialized window.returnValue
        v
C# compatibility host returns
        |
        v
Parent JavaScript resumes
```

## Child behavior

The child page receives:

```javascript
window.dialogArguments
```

The child page may assign:

```javascript
window.returnValue = {
  selectedId: 123,
  accepted: true
};
```

When the child closes, the host serializes and returns this value.

## Supported value types

MVP support:

- `undefined`
- `null`
- boolean
- number
- string
- array
- plain JSON object

Out of scope:

- functions
- DOM objects
- cyclic references
- COM objects
- host objects
- arbitrary prototypes

Boundary policy:

- arguments and return values are limited to 1 MiB of serialized UTF-8 JSON
- JSON depth is limited to 64
- malformed, oversized, or cyclic values are rejected explicitly
- `undefined` remains supported as the cancellation/no-return sentinel
- payload logs are truncated and include byte counts

See `docs/json-payload-boundary.md`.

## Feature string support

The measured MVP parser and WPF application contract is recorded in
`docs/dialog-feature-compatibility.md`.

- `dialogWidth`
- `dialogHeight`
- `dialogLeft`
- `dialogTop`
- `center`
- `resizable`
- `status`
- `scroll`

Unknown values must be ignored and logged.

`status` and `scroll` are parsed and logged but are not visually emulated in the MVP.

## Acceptance test

Parent page:

```javascript
const result = window.showModalDialog(
  "/dialog.html",
  { id: 123, name: "test" },
  "dialogWidth:500px;dialogHeight:300px;center:yes"
);

document.querySelector("#result").textContent =
  JSON.stringify(result);
```

Child page:

```javascript
window.returnValue = {
  selectedId: window.dialogArguments.id,
  accepted: true
};

window.close();
```

Pass conditions:

- parent code after the call does not execute while the dialog remains open
- child UI remains interactive
- arguments reach the child
- return value reaches the parent
- session state is shared
- repeated open/close cycles do not deadlock
- cancellation returns `undefined`
