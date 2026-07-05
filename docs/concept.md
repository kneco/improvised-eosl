# Concept

## Problem

Many legacy enterprise web applications were written for Internet Explorer-specific behaviors.

Some applications can be migrated with simple polyfills. Others rely on browser behavior that changes program control flow.

The most important example is `window.showModalDialog()`.

Unlike a normal popup, `showModalDialog()` blocks the caller and returns a value synchronously.

```javascript
const result = window.showModalDialog(
  "/dialog.html",
  { customerId: 123 },
  "dialogWidth:600px;dialogHeight:400px"
);

if (result) {
  updateCustomer(result);
}
```

Replacing this with `window.open()` or a Promise usually requires application code changes.

## Product idea

Improvised EOSL wraps Chromium through WebView2 and restores selected legacy expectations by combining:

- early JavaScript injection
- synchronous WebView2 host objects
- native C# window management
- site-specific compatibility profiles
- compatibility call logging

## User experience

The user-facing browser should resemble Microsoft Edge closely enough that ordinary users can operate it without retraining.

The goal is not a pixel-perfect clone.

The goal is consistency in:

- tabs
- navigation controls
- window behavior
- downloads
- authentication dialogs
- printing
- context menus
- keyboard shortcuts

## Intended tone

The project may be published as an experimental or joke-adjacent tool.

Its tone can be humorous, but its technical documentation must remain precise.

Suggested tagline:

> これは延命装置ではありません。延命しているように見える実験装置です。
