# Local content loading

## Decision

Local HTML is opened through a loopback HTTP server owned by the application. The
parent WebView2 does not navigate directly to `file:` URLs.

This keeps local pages inside the existing HTTP(S) origin model used by the
`showModalDialog` compatibility bridge. It also lets relative HTML, CSS, script,
image, and child-dialog URLs resolve without weakening WebView2 security settings.

## User entry points

- Drop one `.html` or `.htm` file onto the main browser window.
- Enter an absolute Windows path or `file:` URL in the address bar.

The selected file opens in the existing main WebView2. Multiple files,
directories, missing files, and non-HTML files are rejected with a visible error.

## Security boundary

- Only the selected HTML file's containing directory is exposed.
- Requests that escape that directory are rejected.
- The server listens only on the IPv4 loopback interface.
- Only `GET` is accepted and responses use `Cache-Control: no-store`.
- The selected local root is replaced, not accumulated, when another directory is
  opened.
- `file:`, `data:`, and `javascript:` remain unsupported child-dialog schemes.
- The loopback origin is not persisted as a user-approved compatibility origin.
- A local compatibility approval lasts only for the current application session
  and is revoked when the selected root changes.
- An administrator may still grant the loopback origin explicitly through a
  configured compatibility profile; that is a separate trusted configuration.

The local server does not bypass the legacy API consent flow. On the first
`window.showModalDialog` call, the user is still asked whether compatibility may
be enabled for the current loopback origin.

## Compatibility limitations

- Server-side behavior such as PHP, ASP, CGI, or SSI is not executed.
- Absolute `file:` references inside a page are not rewritten.
- Browser origin changes from `file:` to loopback HTTP, which can affect code that
  inspects `location` or depends on file-origin behavior.
- Legacy character encoding is taken from BOM, HTML metadata, or browser sniffing;
  the server does not force UTF-8 for user HTML.
- Opening a different directory invalidates the current local compatibility
  approval and may require consent again.

## Manual validation

Use `manual-tests/local-content/small-parent.html`.

1. Resize the main window to a small usable size.
2. Drop `small-parent.html` onto the WebView2 area.
3. Confirm it opens in the existing main window, not a new window.
4. Press `Open local child dialog`.
5. Allow compatibility and reload when prompted, then press the button again.
6. Confirm the relative child page opens, remains interactive, and returns its
   value synchronously to the parent.
7. Drop a non-HTML file and confirm it is rejected without navigation.
