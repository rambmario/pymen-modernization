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
- `Jwt:SigningKey` obligatorio (≥32 chars) con **`ValidateOnStart`**; sin clave de respaldo. Config vía `IOptions<JwtOptions>` (una sola lectura; `JWT_SIGNING_KEY` puede sobreescribir).
- **FallbackPolicy** = `RequireAuthenticatedUser`; login y health con `AllowAnonymous` explícito.
- **Rate limiting** fixed-window por IP en `POST /api/auth/login` → **429**.
- Seed demo solo en **Development** (salvo `SkipSeed`) o con **`Seed:Enabled=true`**.
- Cancelación de OT solo vía `POST .../cancel` (sin DELETE).
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
| Clave JWT hardcodeada / fallback | Riesgo de arrancar inseguro en misconfig. |

## Consecuencias

- Seed de usuarios demo por tenant tras datos de negocio (solo Dev / flag).
- Tests cubren login, flujo OT, cross-tenant 404, Operator 403, arranque sin JWT, 401 por FallbackPolicy y 429 en login.
- Los casos de uso de negocio viven en Application (`*Service` + `IPymenDbContext`); la API solo adapta HTTP (refactor post Paso 4).

## Trade-off: Application depende de EF Core

`IPymenDbContext` expone `DbSet<T>` (y por tanto Application referencia EF Core). **No** se agregan repositorios genéricos: EF Core ya implementa Unit of Work + Repository. El trade-off aceptado es acoplar Application a EF a cambio de menos capas y mapeos; si más adelante hace falta desacoplar (otro store, tests sin EF), se introduce un puerto más estrecho por agregado, no un repositorio genérico.
