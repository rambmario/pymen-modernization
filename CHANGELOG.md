# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Conventional Commits](https://www.conventionalcommits.org/).

## [Unreleased]

### Added

- Recruiter-oriented English README (architecture Mermaid, ADRs, getting started, testing, screenshots placeholders); MIT LICENSE.
- Repository scaffolding: CHANGELOG, `.gitignore`, `docs/worklog.md`, `docs/ai-usage.md`, `docs/adr/`, `docs/legacy/`.
- Legacy system analysis (`docs/legacy/legacy-analysis.md`).
- Migration proposal and MVP roadmap (`docs/legacy/migration-proposal.md`).
- ADR 0001: strangler-fig modernization strategy.

### Changed

- Enriched legacy analysis with production BOM/stock consumption, shop-floor hours, permission/password paths, and refined open questions.
- Closed several open questions via owner decisions.
- Migration proposal: shared-database multi-tenancy with `TenantId`.

### Added

- `docs/legacy/owner-decisions.md` (owner confirmations for Phase 1).
- ADR 0002: shared-database multi-tenant with `TenantId`.
- .NET 10 Clean Architecture solution (`Pymen.sln`, `src/*`, `tests/*`).
- `Directory.Build.props`, `.editorconfig`, `global.json`, `nuget.config`.
- ADR 0003: solution structure.
- Domain model for production core (work orders, operations, BOM, stock, delivery notes) with unit tests.

### Changed

- Domain uses `TimeProvider` for timestamps, UUID v7 ids, and cascade-cancels open operations when a work order is cancelled.

### Added

- EF Core 10 + SQL Server persistence (`PymenDbContext`, Fluent configs, `InitialCreate` migration).
- Global `TenantId` query filters; `AmbientTenantContext`.
- `Tenant.RowVersion` + `WorkOrderNumberAllocator` retries; unique indexes for work-order numbers, SKUs, and codes per tenant.
- `docker-compose.yml` (SQL Server 2022), fictitious `DemoDataSeeder`, `/health` vs `/health/ready`.
- Parallel work-order numbering integration test (Testcontainers; skipped without Docker).
- ADR 0005: EF Core + SQL Server persistence.

### Fixed

- Tenant query filters are fail-closed (`CurrentTenantIdOrEmpty`); seed/system paths use `IgnoreQueryFilters()` explicitly (ADR 0004).
- Restrict foreign keys for tenant-owned entities and related catalog/inventory/production references; migration `AddRestrictForeignKeys`.
- Dev SQL password moved to `.env` (gitignored) with `.env.example`; docker-compose and tests load it from there.
- Integration test for tenant isolation (tenant A vs B; empty when no tenant).

### Added

- ASP.NET Identity + JWT auth with roles Admin/Planner/Operator; demo users seeded from `.env` passwords (ADR 0006).
- Tenant middleware from JWT `tenant_id` claim; Minimal API endpoints for auth, customers, products, work-orders, operations, delivery-notes.
- FluentValidation (400), DomainException ProblemDetails (422), concurrency (409); OpenAPI + Scalar.
- Integration tests: login, full work-order flow, cross-tenant 404, Operator cancel 403.

### Fixed

- JWT signing key required at startup (`ValidateOnStart`, no fallback); FallbackPolicy requires auth; login rate-limited (429); demo seed gated to Development / `Seed:Enabled`; removed DELETE work-order (cancel via POST only).

### Changed

- Application services (`CatalogService`, `WorkOrderService`, `DeliveryNoteService`) own use cases via `IPymenDbContext`; API endpoints only map HTTP ↔ services; added `Pymen.Application.Tests`.

### Added

- Angular 21 SPA in `/web`: login, work-order list/detail/create, Material UI, role-aware actions, ProblemDetails snackbars (ADR 0007).
- Dev proxy to the API; CORS for `http://localhost:4200` in Development only.
- `GET /api/operation-types` for OT create/detail labels.
- Server-side work-order list pagination/filters (`items`, `page`, `pageSize`, `totalCount`; max pageSize 100).
- GitHub Actions CI (backend + frontend), Dependabot (NuGet/npm/Actions), CI badge (ADR 0008).
- Dependabot grouped updates; majors ignored for Angular/TypeScript/Vitest/jsdom and selected NuGet packages.
- Central Package Management (`Directory.Packages.props`) so NuGet versions are defined once.

### Changed

- Angular work-order list uses server pagination; subscriptions use `takeUntilDestroyed`; detail binds route `id` via `withComponentInputBinding()`.
- Local Docker SQL published on host port **14333** (avoids clashing with a local SQL Server on 1433).

### Added

- README screenshots (`docs/images/*.png`) and Playwright regen script (`web`: `npm run screenshots`).
