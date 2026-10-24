# Pymen Modernization

[![CI](https://github.com/rambmario/pymen-modernization/actions/workflows/ci.yml/badge.svg)](https://github.com/rambmario/pymen-modernization/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white)](https://angular.dev/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Modernization of a 20-year-old VB.NET WinForms ERP for manufacturing SMEs into a multi-tenant .NET 10 + Angular platform.

---

## Context

Pymen is a manufacturing ERP I designed and maintained for ~10 years as founder of Ingesoft, used in production by SMEs. It was built as a **VB.NET WinForms** desktop app on **.NET Framework 3.5**, with **SQL Server**, **Crystal Reports**, roughly **312k LOC**, **~876 forms**, and **~1,400 stored procedures**. The legacy system is treated as **read-only** analysis input; this repository holds only the greenfield stack.

The goal is not a big-bang rewrite. The project follows a **[strangler fig](docs/adr/0001-estrategia-de-modernizacion.md)** strategy: ship a clean vertical slice (auth, tenancy, work orders) that can grow module by module while the legacy system keeps running. Client names, tax IDs, and production credentials never enter this repo.

---

## What it does

Phase 1 delivers the **production work-order (OT)** core:

- **Work-order lifecycle:** Draft → InProduction → Delivered → Invoiced (plus Cancel from Draft/InProduction).
- **Shop-floor operations** on each OT: start, finish, cancel (role-aware).
- **BOM / recipes** and **stock** modeled in the domain (consumption rules ready for Phase 2 wiring).
- **Delivery notes (remitos)** as first-class aggregates linked to work orders.
- Multi-tenant **Admin / Planner / Operator** roles with JWT auth and a Spanish Angular UI for login, OT list (server-side filters/pagination), detail, and create.

---

## Architecture

```mermaid
flowchart LR
  subgraph client [Client]
    Angular["Angular 21 SPA"]
  end
  subgraph host [API host]
    Api["Pymen.Api\nMinimal APIs + Identity/JWT"]
  end
  subgraph app [Application]
    Services["Use-case services\nCatalog / WorkOrder / DeliveryNote"]
  end
  subgraph core [Domain]
    Domain["Entities + domain rules"]
  end
  subgraph infra [Infrastructure]
    Ef["EF Core 10 + SQL Server\nSeed / Identity"]
  end
  Angular -->|HTTPS JWT| Api
  Api --> Services
  Services --> Domain
  Services --> Ef
  Ef --> Sql[("SQL Server\nshared DB + TenantId")]
```

```mermaid
stateDiagram-v2
  [*] --> Draft
  Draft --> InProduction: start
  Draft --> Cancelled: cancel
  InProduction --> Delivered: deliver
  InProduction --> Cancelled: cancel
  Delivered --> Invoiced: invoice
  Invoiced --> [*]
  Cancelled --> [*]
```

Clean Architecture layout: `Domain` ← `Application` ← `Infrastructure` / `Api`, plus `web/` for the SPA. See [ADR 0003](docs/adr/0003-estructura-de-solucion.md).

---

## Key technical decisions

- **Shared-database multi-tenancy** with `TenantId` and **fail-closed** query filters (no ambient tenant → empty results) — [ADR 0002](docs/adr/0002-multi-tenant-base-compartida.md), [ADR 0004](docs/adr/0004-aislamiento-tenants-fail-closed.md).
- **Work-order numbering concurrency** via `Tenant.RowVersion` + allocator retries — [ADR 0005](docs/adr/0005-persistencia-ef-core-sql-server.md).
- **ASP.NET Identity + JWT**, roles Admin/Planner/Operator, `FallbackPolicy = RequireAuthenticatedUser`, login rate limit — [ADR 0006](docs/adr/0006-auth-identity-jwt-roles.md).
- **Application depends on EF Core** (`IPymenDbContext` exposes `DbSet`); no generic repositories (EF already is UoW + Repository) — trade-off in [ADR 0006](docs/adr/0006-auth-identity-jwt-roles.md).
- **`TimeProvider`** for timestamps; **UUID v7** for new ids.
- **Server-side OT pagination/filters** (`items`, `page`, `pageSize`, `totalCount`; max page size 100).
- **Angular SPA** with Material, signals, lazy routes, role guards — [ADR 0007](docs/adr/0007-frontend-angular.md).
- **GitHub Actions CI** + Dependabot; ephemeral JWT/SQL passwords in CI — [ADR 0008](docs/adr/0008-ci-github-actions.md).
- **Azure demo (Phase 2):** Bicep in [`infra/`](infra/) (Container Apps, Static Web Apps Free, SQL serverless, Service Bus Basic, Blob, Key Vault, App Insights, managed identity, budget). Manual steps only — [docs/azure-setup.md](docs/azure-setup.md), [ADR 0009](docs/adr/0009-arquitectura-cloud-azure.md).

---

## Tech stack

| Layer | Choice |
|--------|--------|
| API | .NET 10, Minimal APIs, ASP.NET Identity, JWT |
| Application | Use-case services (no MediatR), FluentValidation |
| Domain | Rich entities, `DomainException`, `TimeProvider` |
| Persistence | EF Core 10, SQL Server, Testcontainers |
| Frontend | Angular 21, standalone, signals, Angular Material |
| Authz | Roles Admin / Planner / Operator; FallbackPolicy |
| Docs / CI | ADRs, worklog, GitHub Actions, Dependabot |
| Package mgmt | Central Package Management (`Directory.Packages.props`) |

---

## Getting started

**Requirements:** .NET SDK 10 (see `global.json`), Node.js 22+, Docker Desktop (**Linux containers**), Git.

```bash
# 1. Secrets / local config (never commit .env)
cp .env.example .env
# Edit MSSQL_SA_PASSWORD, JWT_SIGNING_KEY (≥32 chars), DEMO_PASSWORD_*

# 2a. Full local stack (SQL, API, worker placeholder, SPA+nginx, Azurite, Service Bus emulator)
docker compose up -d --build
# SPA  →  http://localhost:8088   (nginx proxies /api; 8080 often blocked on Windows)
# API  →  http://localhost:5207

# 2b. Or SQL only + run API/SPA on the host
docker compose up -d sqlserver
dotnet run --project src/Pymen.Api --launch-profile http   # http://localhost:5207
cd web && npm ci && npm start                              # http://localhost:4200 (proxies /api)

# 3. Optional — regenerate README screenshots (API + SPA must be running)
cd web && npm run screenshots
```

Docker SQL is published on host port **14333** (avoids clashing with a local SQL Server on 1433). The worker consumes `WorkOrderDelivered`, generates the remito PDF (QuestPDF), and stores it in Azurite/Blob; use **Descargar remito** on a Delivered/Invoiced OT.

**Demo users** (passwords come from `.env` / `DEMO_PASSWORD_*` — not listed here):

| Email | Role | Tenant |
|--------|------|--------|
| `admin.t1@demo.local` | Admin | Demo tenant 1 |
| `planner.t1@demo.local` | Planner | Demo tenant 1 |
| `operator.t1@demo.local` | Operator | Demo tenant 1 |
| `*.t2@demo.local` | same roles | Demo tenant 2 |

Operator can list/get work orders and drive shop-floor operations only. Admin/Planner manage catalog, OT lifecycle, and delivery notes.

---

## Testing

| Project | Tests | Notes |
|---------|-----------------|--------|
| `Pymen.Domain.Tests` | 30 | Pure domain unit tests |
| `Pymen.Application.Tests` | 7 | Services + InMemory EF |
| `Pymen.Infrastructure.Tests` | 2 | QuestPDF remito + worker handler idempotency |
| `Pymen.Api.IntegrationTests` | 15 | Testcontainers SQL Server (needs Docker) |
| `web` (Vitest) | 17 | Auth, guards, interceptor, OT list |

```bash
# Backend (integration tests start SQL Server via Testcontainers)
dotnet test Pymen.sln

# Frontend
cd web && npm test
```

CI runs both jobs on every push/PR to `main`: [workflow](https://github.com/rambmario/pymen-modernization/actions/workflows/ci.yml).

---

## Screenshots

Captured against the local Dev stack (`docker compose up -d`, API + SPA). Regenerate with `cd web && npm run screenshots` (reads `DEMO_PASSWORD_PLANNER` from `.env`; never hardcodes secrets).

| Screen | Image |
|--------|--------|
| Login | ![Login](docs/images/login.png) |
| Work-order list | ![Work-order list](docs/images/work-order-list.png) |
| Work-order detail | ![Work-order detail](docs/images/work-order-detail.png) |

### Async remito demo (local)

![Deliver OT → worker PDF → download remito](docs/images/demo-remito.gif)

Regenerate: `docker compose up -d --build` then `cd web && npm run demo:gif` (Playwright records the UI; ffmpeg or Docker `jrottenberg/ffmpeg` converts to GIF).

---

## Cloud-ready (Azure deferred)

Phase 2 is **deploy-ready** without a live Azure subscription (cost). IaC, OIDC workflow, and observability are in the repo; nothing is provisioned until you opt in.

| What would deploy | Detail |
|-------------------|--------|
| Container Apps | API + worker (Consumption, scale-to-zero) |
| Static Web Apps | Angular SPA (Free) |
| Azure SQL | Serverless + auto-pause, Entra-only |
| Service Bus | Basic queue `domain-events` (Outbox → PDF) |
| Blob + Key Vault + App Insights | Remito PDFs, JWT secret, traces |
| Budget alert | Soft monthly cap (e.g. ~USD 30) |

- Architecture: [ADR 0009](docs/adr/0009-arquitectura-cloud-azure.md) (deferred deploy), [ADR 0010](docs/adr/0010-mensajeria-async-outbox.md), [architecture-cloud.md](docs/architecture-cloud.md)
- Bicep: [`infra/`](infra/) — validated in CI (`az bicep build` + linter)
- Manual checklist / OIDC activation: [docs/azure-setup.md](docs/azure-setup.md)
- Deploy workflow: [`.github/workflows/deploy-azure.yml`](.github/workflows/deploy-azure.yml) (`workflow_dispatch` only; **skips** if `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` are unset)
- **Estimated demo cost:** ~USD 0–25 / month when idle-friendly (see ADR 0009). No public URL until a subscription is configured.

Local observability: OpenTelemetry → OTLP collector in compose (or console). Azure: same SDK → Application Insights when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set.

---

## AI-assisted development

This portfolio was built with **Cursor** under project rules (`.cursor/rules`, `AGENTS.md`): Spanish process docs, English code/commits, no PII, legacy tree read-only. Every session is logged in [`docs/worklog.md`](docs/worklog.md) and [`docs/ai-usage.md`](docs/ai-usage.md); architectural choices land as ADRs under [`docs/adr/`](docs/adr/).

---

## Roadmap

| Phase | Focus |
|-------|--------|
| **1** (done) | Foundation: Clean Architecture, EF Core, tenancy, Identity/JWT, Angular OT UI, CI |
| **2** (cloud-ready) | Outbox + Service Bus + QuestPDF remito; Bicep/OIDC deploy-ready (Azure deferred); OpenTelemetry |
| **3** | Commercial basics (extended delivery/invoice views, pricing, pending-delivery queries) |

---

## Author

**Mario Ramb** — Senior .NET Full Stack Developer

- LinkedIn: [linkedin.com/in/marioramb](https://www.linkedin.com/in/marioramb)
- GitHub: [github.com/rambmario](https://github.com/rambmario)

---

## License

[MIT](LICENSE)
