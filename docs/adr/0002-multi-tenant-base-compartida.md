# ADR 0002 — Multi-tenant con base compartida y TenantId

**Estado:** Aceptado  
**Fecha:** 2026-10-19  
**Decisores:** Mario (dueño del sistema)

---

## Contexto

En legacy, cada empresa suele tener su propia base (`_Base*`) con el **mismo esquema** y datos distintos. La conexión se cambia a mano en `Conexion.vb`. Para el producto nuevo hace falta multi-empresa sin redeploy por cliente.

## Decisión

Usar **una base de datos compartida** con aislamiento por **`TenantId`** en todas las entidades de negocio (`ITenantOwned`), filtros globales en EF Core y resolución del tenant desde el **claim del JWT**.

No usar (en el MVP) una base SQL por tenant ni schema-per-tenant.

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| Database-per-tenant | Más aislamiento, pero más ops/costo y migrations N veces; overkill para portfolio/MVP |
| Schema-per-tenant | Complejidad de migraciones y tooling EF sin beneficio claro aún |
| Mantener una BD por cliente como legacy | No escala como SaaS; revive el anti-patrón de `Conexion.vb` |

## Consecuencias

**Positivas:** un solo pipeline de migraciones; seed multi-tenant sencillo; tests de aislamiento en una sola instancia SQL.

**Negativas / mitigaciones:** riesgo de fuga de datos entre tenants → query filters + tests de integración obligatorios; índices compuestos con `TenantId`; no confiar solo en el cliente para enviar el tenant.

**Relacionado:** `docs/legacy/owner-decisions.md` §5.
