# ADR 0004 — Aislamiento de tenants fail-closed

**Estado:** Aceptado  
**Fecha:** 2026-10-19  
**Decisores:** Mario

---

## Contexto

Con base compartida + `TenantId` (ADR 0002) y filtros globales de EF (ADR 0005), un filtro “fail-open” (`!HasTenant || TenantId == …`) devolvía **todas** las filas cuando no había tenant en el contexto. Eso es un riesgo de fuga entre tenants en seed, jobs o bugs de middleware.

## Decisión

- Filtro **fail-closed**: `e.TenantId == _tenantContext.CurrentTenantIdOrEmpty`.
- Sin tenant ambient → `CurrentTenantIdOrEmpty` es `Guid.Empty` → **ninguna** fila `ITenantOwned` visible.
- Aplicar el filtro **una sola vez** recorriendo los tipos del modelo que implementan `ITenantOwned`.
- Seed, migraciones de datos y procesos de sistema que necesiten ver todos los tenants usan **`IgnoreQueryFilters()`** de forma explícita (y documentada).

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| Fail-open (`!HasTenant \|\| …`) | Conveniente para seed, pero peligroso: olvido de SetTenant = leak. |
| Exigir tenant siempre (excepción al consultar) | Más ruidoso; el filtro vacío ya bloquea lecturas sin romper inserts. |
| Filtros manuales por consulta | Fácil de olvidar; el global + IgnoreQueryFilters es más seguro. |

## Consecuencias

- Tests de aislamiento deben cubrir: tenant A no ve B; sin tenant → vacío.
- Código de sistema (seed, allocator cross-check) debe llamar `IgnoreQueryFilters()` cuando lea datos multi-tenant a propósito.
- Relacionado con persistencia EF/SQL Server en ADR 0005.
