# Decisiones del dueño del sistema (Mario)

**Fecha:** 2026-10-19  
**Contexto:** Cierre de preguntas abiertas de Fase 0 antes de Foundation (Fase 1).  
**Fuente:** confirmación explícita del dueño del sistema.

---

## Decisiones cerradas

### 1. Semántica Cliente / Proveedor

| Concepto legacy | Significado de negocio | Módulo canónico |
|-----------------|------------------------|-----------------|
| **Cliente** | **Ventas** (OT, remitos, facturación a cliente) | `Cliente` |
| **Proveedor** | **Compras** (OC a proveedor, facturas de compra) | `Proveedor` |

En el modelo nuevo: `Customer` (ventas) y, más adelante, `Supplier` / `PurchaseOrder` (compras).

### 2. Alcance por módulo

Versiones **canónicas** a analizar y migrar:

| Dominio | Carpeta canónica |
|---------|------------------|
| Ventas | `Cliente` |
| Compras | `Proveedor` |
| Caja | `Caja` |

### 3. Estado `"A"` de la OT (`Orden_compra.estado`)

- En legacy el significado es **dudoso** (posiblemente “Abierta”; no confirmado con certeza).
- En el **modelo nuevo** los estados son **explícitos** (nombres claros en inglés).
- La **anulación** es un **estado propio** (`Cancelled`), no un flag ambiguo paralelo al código `"A"`.

### 4. Remito automático y stock

- El remito automático **solo documenta**.
- **No mueve stock**.
- El movimiento de stock de producto terminado / consumo de insumos sigue otros caminos (p. ej. ingreso de producción).

### 5. Bases `_Base*` y multi-tenant

- Las bases `_Base*` comparten el **mismo esquema** con **distintos datos** por empresa.
- Decisión de arquitectura: **multi-tenant con base compartida** y columna / claim **`TenantId`** (no una base SQL por tenant en el MVP).

Detalle técnico: ver ADR `docs/adr/0002-multi-tenant-base-compartida.md`.

### 6. Entrega de OT con operaciones incompletas

- **Sí** se puede marcar una OT como **entregada** aunque queden operaciones en `Pending` o `Started`.
- Confirmado por el dueño (Fase 2 / Paso 0).
- El dominio actual (`WorkOrder.MarkDelivered`) ya lo permite: solo exige transición `InProduction` → `Delivered`; **no** exige operaciones `Finished`.
- No se cambia la lógica; se documenta con un test de dominio que fija el comportamiento.

---

## Preguntas que siguen abiertas

(No inventar; resolver en fases posteriores o con más evidencia.)

1. Significado exacto de flags legacy `cargado`, `acepto_orden_compra` (si se necesitan en el MVP).
2. Cuerpos SQL de los SP de stock/costo no versionados en el repo.
3. Destino de `Nota_pedido` (UI sin capa `ent`/`cls` en este árbol) — ¿código muerto o falta checkout?
4. `Movimiento_etapa`: stub confirmado; ¿abandonado del todo?
5. Alcance de FEAFIPLib (fase fiscal posterior).
6. Inventario fino de reportes Crystal activos vs copias muertas.
7. Estados detallados de `Recepcion` y facturas fuera del flujo OT MVP.

---

## Impacto en documentos

- `legacy-analysis.md`: inventario y preguntas abiertas actualizadas.
- `migration-proposal.md`: tenancy = shared DB + `TenantId` (alineado a esta decisión).
