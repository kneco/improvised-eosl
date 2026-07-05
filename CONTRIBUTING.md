# Contributing

Improvised EOSL is an experimental compatibility project, not a general Internet Explorer
replacement. Before proposing a change, read `AGENTS.md`, `docs/implementation-plan.md`, and
`docs/risks-and-limitations.md`.

Keep changes narrow and reviewable. Compatibility claims need reproducible evidence, and browser
behavior that cannot be automated should include a manual test page or checklist. Do not weaken
Chromium or WebView2 security boundaries to reproduce legacy behavior.

Run the policy tests before submitting a pull request:

```powershell
dotnet run --project tests/ImprovisedEosl.Spike.Tests/ImprovisedEosl.Spike.Tests.csproj
dotnet build ImprovisedEosl.sln --configuration Release
```

Use GitHub Issues for bugs and scoped proposals. Report security concerns privately as described
in `SECURITY.md`.
