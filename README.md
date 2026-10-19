# Pymen Modernization

Portfolio project: modernizing **Pymen**, a legacy VB.NET WinForms ERP focused on production management for SMEs (pymes), into a multi-tenant cloud stack.

## Status

**Phase 0 — Legacy analysis** (documentation only). No application code yet.

## Goals

- Understand the legacy system end-to-end before rewriting.
- Migrate with a **strangler fig** approach (incremental slices, not a big bang).
- Ship an MVP around the **production order lifecycle** plus one key report.
- Keep the public repo free of real customer data, credentials, and database backups.

## Planned stack

| Layer | Choice |
|--------|--------|
| API | .NET 10 Web API, Clean Architecture |
| Data | EF Core + SQL Server |
| UI | Angular |
| Tenancy | Multi-tenant (isolated per company) |
| Background work | Azure Service Bus |
| PDF reports | QuestPDF (replacing Crystal Reports) |
| CI/CD | GitHub Actions → Azure |

## Repository layout

```
docs/
  adr/              Architecture Decision Records
  legacy/           Analysis of the VB.NET system
  worklog.md        Session log
  ai-usage.md       AI assistance log
CHANGELOG.md
README.md
```

## Documentation (Spanish)

Project process docs and ADRs are written in Spanish. Application code, commit messages, and this README stay in English.

Key docs:

- [docs/legacy/legacy-analysis.md](docs/legacy/legacy-analysis.md) — system inventory, data model, rules, risks
- [docs/legacy/migration-proposal.md](docs/legacy/migration-proposal.md) — MVP scope and target architecture
- [docs/adr/0001-estrategia-de-modernizacion.md](docs/adr/0001-estrategia-de-modernizacion.md) — strangler fig strategy

## Privacy

This repository is public. Real client names, tax IDs, addresses, credentials, and connection strings from the legacy codebase are replaced with generics (e.g. `Empresa A`, `Cliente 1`). Database files (`*.bak`, `*.mdf`, `*.ldf`) are never committed.

## License

To be defined.
