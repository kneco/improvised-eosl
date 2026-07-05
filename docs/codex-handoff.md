# Codex handoff

## First instruction to Codex

Read `README.md`, `AGENTS.md`, and every file under `docs/`.

Do not implement the full application yet.

First:

1. identify contradictions and missing assumptions
2. verify the proposed WebView2 threading and synchronous host-object model against current official Microsoft documentation
3. list the highest-risk technical unknowns
4. recommend WPF or WinUI 3 for the MVP
5. update `docs/implementation-plan.md`
6. create one minimal proof-of-concept task only

The first proof of concept must answer:

> Can a synchronous WebView2 host-object call remain blocked while a separately threaded child WebView2 stays interactive and later returns a value?

Do not add unrelated browser features.
Do not attempt full IE compatibility.
Do not prioritize visual polish before synchronization viability is proven.
