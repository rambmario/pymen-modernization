# Pymen Modernization

Portfolio project: modernizing **Pymen**, a legacy VB.NET WinForms ERP focused on production management for SMEs (pymes), into a multi-tenant cloud stack.

## Status

**Phase 1 in progress** — .NET 10 API (Identity + JWT, work orders) and Angular SPA in `/web`.

## Goals

- Understand the legacy system end-to-end before rewriting.
- Migrate with a **strangler fig** approach (incremental slices, not a big bang).
- Ship an MVP around the **production order lifecycle** plus one key report.
- Keep the public repo free of real customer data, credentials, and database backups.

## Stack

| Layer | Choice |
|--------|--------|
| API | .NET 10 Web API, Clean Architecture |
| Data | EF Core + SQL Server |
| UI | Angular 21 (`/web`) |
| Tenancy | Shared DB + `TenantId` (fail-closed filters) |
| Background work | Azure Service Bus (later) |
| PDF reports | QuestPDF (later) |
| CI/CD | GitHub Actions → Azure (later) |

## Repository layout

```
src/            .NET solution (Api, Application, Domain, Infrastructure)
tests/          Domain, Application, API integration tests
web/            Angular SPA
docs/
  adr/          Architecture Decision Records
  legacy/       Analysis of the VB.NET system
  worklog.md
  ai-usage.md
```

## Run (dev)

1. Copy `.env.example` → `.env` and set SQL / JWT / demo passwords.
2. `docker compose up -d` (SQL Server).
3. `dotnet run --project src/Pymen.Api --launch-profile http`
4. `cd web && npm start` → `http://localhost:4200` (proxies `/api` to the API).

## Documentation (Spanish)

Process docs and ADRs are in Spanish. Application code, commit messages, and this README stay in English.

- [docs/legacy/legacy-analysis.md](docs/legacy/legacy-analysis.md)
- [docs/legacy/migration-proposal.md](docs/legacy/migration-proposal.md)
- [docs/adr/0007-frontend-angular.md](docs/adr/0007-frontend-angular.md)

## Privacy

This repository is public. Real client names, tax IDs, addresses, credentials, and connection strings from the legacy codebase are replaced with generics. Database files (`*.bak`, `*.mdf`, `*.ldf`) are never committed.

## License

To be defined.
