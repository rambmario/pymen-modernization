# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Conventional Commits](https://www.conventionalcommits.org/).

## [Unreleased]

### Added

- Repository scaffolding: README, CHANGELOG, `.gitignore`, `docs/worklog.md`, `docs/ai-usage.md`, `docs/adr/`, `docs/legacy/`.
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
