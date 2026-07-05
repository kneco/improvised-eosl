# Configured compatibility profiles

## Purpose

Configured profiles enable selected compatibility APIs for administrator-authored HTTP(S)
origins without requiring the discovery consent prompt. They are read-only at runtime and
remain separate from user-approved origins.

The profile file is trusted configuration. The PoC relies on operating-system file
permissions and does not authenticate or sign it; anyone who can modify the deployed file
can enable compatibility for an HTTP(S) origin.

The WPF spike reads:

```text
config/compatibility-profiles.json
```

relative to the executable directory. The source file under
`src/ImprovisedEosl.Spike.SyncModal/config/` is copied to the build output. The checked-in
file contains no enabled profiles.

## Schema version 1

```json
{
  "version": 1,
  "profiles": [
    {
      "id": "legacy-order-system",
      "displayName": "Legacy order system",
      "startUrl": "https://orders.example.com/",
      "allowedOrigins": [
        "https://orders.example.com"
      ],
      "compatibility": {
        "showModalDialog": true
      }
    }
  ]
}
```

Rules:

- `id` is required, case-insensitively unique, and limited to 128 characters.
- `displayName` is optional, defaults to `id`, and is limited to 256 characters.
- `startUrl` must be an absolute HTTP(S) URL without user information.
- `startUrl` is used when that validated profile is explicitly selected through `--profile`.
  It is distinct from the normal-startup user preference.
- `allowedOrigins` must contain 1 through 128 exact HTTP(S) origins.
- Paths, queries, fragments, user information, non-HTTP(S) schemes, and wildcards are rejected in `allowedOrigins`.
- Origin identity is normalized to `scheme://host:effective-port`.
- `compatibility.showModalDialog` must be `true` to create grants for that profile.
- Unknown properties are rejected so misspelled security settings do not silently pass.

Global limits:

- maximum file size: 1 MiB
- maximum profile count: 128
- maximum JSON depth: 32

## Permission sources

Effective permission is the union of:

```text
configured profile grants OR user/runtime grants
```

The collections remain independent:

- The compatibility settings window lists and revokes only user-approved entries.
- Revoking a user approval does not remove a matching configured grant.
- Automatic-test allowances are not written to either configuration file.
- Removing a configured grant requires editing or replacing the configured profile file and restarting the application.

## Startup profile selection

Select one validated profile by ID when starting the application:

```powershell
dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --profile=legacy-order-system
```

The separated form is also accepted:

```powershell
dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --profile legacy-order-system
```

Selection rules:

- IDs are matched case-insensitively after the profile file is validated.
- No selection preserves the existing local test start page.
- A selected profile navigates the parent WebView2 to its `startUrl`.
- `startUrl` does not implicitly grant compatibility; only `allowedOrigins` does. This permits a normal SSO or launch page to precede an allowed legacy origin.
- A missing ID, multiple selections, or an unknown/discarded profile displays an error and does not navigate.
- Profile selection cannot be combined with normal automatic validation modes.
- A graphical profile chooser remains unimplemented. The portable-settings work provides a separate user-managed
  normal-startup URL; it will not select, edit, or replace trusted compatibility profiles.

## Failure behavior

- A missing file means no configured profiles.
- Invalid JSON, an unsupported version, an oversized file, unknown root properties, or too many profiles fails the entire file closed.
- An invalid individual profile is discarded without granting any of its origins; other valid profiles continue to load.
- Diagnostics identify the file or profile index without logging page query strings or credentials.
- The application logs the number of loaded profiles and grants at startup.

## Automatic integration validation

Run:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --profile-auto
```

This mode loads `config/compatibility-profiles.auto.json`, which grants only the fixed local
test origin at `http://127.0.0.1:18080`. It deliberately does not add the normal automatic
runtime allowance. The test-only probe confirms that the effective grant came from configured
profiles, then JavaScript calls `window.showModalDialog`, waits for the child, and requires the
synchronous result to contain `accepted: true` and `selectedId: 901`. Failure exits nonzero.

The fixture is never selected during normal application startup.

Startup selection itself is covered by:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --startup-profile-auto --profile=automatic-configured-origin
```

This requires the resolved profile ID, configured permission, and final `startUrl` navigation
to agree. It deliberately uses the same `--profile` parser as normal startup.
