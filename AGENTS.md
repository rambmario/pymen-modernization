# AGENTS.md — Pymen modernization

Guidance for AI coding agents (Cursor, Claude Code, Codex) working in this repo.

## Project
Modernization of Pymen, a legacy VB.NET 3.5 WinForms + SQL Server + Crystal Reports production-management ERP, into .NET 10 + Angular. Strategy: strangler fig (see docs/adr/0001). Legacy analysis: docs/legacy/.

## Architecture rules
- Clean Architecture: Domain has no dependencies; Application depends only on Domain; Infrastructure and Api depend inward. Never reference Infrastructure from Domain or Application.
- Business rules live in Domain (entities, value objects, state transitions), not in controllers or EF configurations.
- Every tenant-owned entity implements ITenantOwned; never bypass the global TenantId query filter.
- Names in English in code; business terms explained in Spanish in docs.
- Only migrate the modules and flows defined in docs/legacy/migration-proposal.md; anything else in the legacy code is out of scope.

## Quality
- Every domain rule and state transition has a unit test (xUnit).
- Tenant isolation must stay covered by an integration test.
- No secrets, connection strings or real customer data in the repo; seeds use fictitious data only.
- Run `dotnet build` and `dotnet test` before committing; CI must stay green.

## Workflow
- One task = one commit, Conventional Commits.
- Log every session as defined in .cursor/rules/registro.mdc (worklog, ai-usage, ADR, CHANGELOG).
- If legacy behavior is unclear, add a question to docs/legacy/owner-decisions.md instead of guessing.