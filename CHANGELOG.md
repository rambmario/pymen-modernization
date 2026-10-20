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
