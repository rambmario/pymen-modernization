# ADR 0006 — Autenticación Identity + JWT y roles

**Estado:** Aceptado  
**Fecha:** 2026-10-19  
**Decisores:** Mario

---

## Contexto

Paso 4 de la Fase 1: la API necesita autenticación multi-tenant sin aceptar `TenantId` del cliente (body/query), con roles alineados al piso de planta (Admin / Planner / Operator).

## Decisión

- **ASP.NET Identity** en el mismo `PymenDbContext` (`ApplicationUser` con `TenantId`).
- **JWT** Bearer; claim obligatorio `tenant_id`. Middleware carga `ITenantContext`; sin claim → **401**.
- Roles: **Admin** (todo), **Planner** (OT/catálogo/remitos), **Operator** (solo start/finish/cancel de operaciones).
- Recurso de otro tenant → **404** (filtro fail-closed; no revelar existencia).
- Contraseñas demo y `JWT_SIGNING_KEY` solo en **`.env` / user-secrets**, nunca en código.
- OpenAPI + **Scalar**; FluentValidation → 400; `DomainException` → 422; concurrencia → 409.

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| TenantId en header/query | Spoofing fácil; viola ADR 0004. |
| Auth0 / Entra externos | Overhead para Fase 1 portfolio. |
| 403 en cross-tenant | Filtra existencia de IDs ajenos. |

## Consecuencias

- Seed de usuarios demo por tenant tras datos de negocio.
- Tests de integración cubren login, flujo OT, cross-tenant 404 y Operator 403.
