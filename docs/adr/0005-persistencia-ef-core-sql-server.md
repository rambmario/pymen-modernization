# ADR 0005 — Persistencia con EF Core y SQL Server

**Estado:** Aceptado  
**Fecha:** 2026-10-19  
**Decisores:** Mario

---

## Contexto

Paso 3 de la Fase 1: el dominio ya modela OT, catálogo e inventario. Hace falta persistencia relacional con aislamiento por tenant, numeración segura bajo concurrencia y entorno local reproducible.

## Decisión

- **EF Core 10 + SQL Server** en `Pymen.Infrastructure` (`PymenDbContext`, Fluent API, migraciones).
- **Filtros globales** por `TenantId` vía `ITenantContext` / `AmbientTenantContext` en todas las entidades `ITenantOwned`.
- **Numeración de OT:** secuencia en `Tenant.NextWorkOrderSequence` con token **`RowVersion`** (optimistic concurrency) y reintentos en `WorkOrderNumberAllocator`; índice único `(TenantId, Number)` en `WorkOrders`.
- Índices únicos por tenant: Product `(TenantId, Sku)`, Warehouse/OperationType `(TenantId, Code)`.
- **docker-compose** con SQL Server 2022; seed ficticio en Development (`DemoDataSeeder`); health liveness (`/health`) vs readiness (`/health/ready`).
- Test de concurrencia de numeración con Testcontainers; se omite si Docker no está disponible.

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| Dapper / SQL crudo | Más control, pero más boilerplate y menos alineado al ritmo de Fase 1. |
| Bloqueo pesimista / SERIALIZABLE en secuencia | Más contention; rowversion + reintentos alcanza para el MVP. |
| Esquema por tenant | Ya descartado en ADR 0002 (shared DB + TenantId). |

## Consecuencias

- Migraciones viven en Infrastructure; Design-time factory para `dotnet ef`.
- CI/local sin Docker: el test de concurrencia se skipea; smoke de API no depende de SQL.
- Concurrencia de numeración cubierta por rowversion + unique index + reintentos.
