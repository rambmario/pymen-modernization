# Propuesta de migración — Pymen

**Fecha:** 2026-10-19  
**Entrada:** [legacy-analysis.md](./legacy-analysis.md)  
**Estrategia:** strangler fig (ver ADR 0001)

---

## 1. Núcleo MVP (fase 1 de producto)

### Qué migrar primero

Flujo de **producción de punta a punta** más **un reporte clave**:

1. **Catálogo:** productos, insumos, BOM (`Insumo_producto`), almacenes, stock.
2. **Clientes** (mínimo necesario para emitir OT).
3. **OT (`Orden_compra`)** + líneas (`Cuerpo_orden_compra`): alta, numeración, estados `P/R/F/A`, flags de cierre/anulación.
4. **Procesos internos (`Ot_interna`)** ligados a la OT (estados `E/C/T`).
5. **Remito** asociado a OT (dispara transición a `R` cuando cantidades cuadran).
6. **Ingreso a stock / impacto de stock** al producir o remitir (reglas equivalentes a `cop_Producto_UpdateStock*`, reimplementadas en dominio + EF).
7. **Reporte clave:** **OT de producción** (equivalente a `rptOT_Produccion` / `frmReporteOTProduccion`) generado con **QuestPDF**.

### Por qué este MVP

- Es el **corazón del negocio** de Pymen (“gestión de producción”), no un módulo periférico (caja, cheques, sueldos).
- Demuestra valor de portfolio: estados, stock, PDF, multi-tenant, API + Angular.
- Acota Crystal Reports a **un** documento crítico antes de migrar cientos de `.rpt`.
- Deja fuera (por ahora) FE AFIP, bancos, contable y RRHH — alto costo regulatorio/contable.

### Fuera de MVP (explícito)

Facturación electrónica AFIP, IVA, caja/bancos/cheques, contabilidad, RRHH/sueldos, calidad completa, mantenimiento de máquinas, personalizaciones por empresa en Crystal.

---

## 2. Roadmap por fases

| Fase | Nombre | Entregable |
|------|--------|------------|
| **0** | Análisis | Este repo: análisis legacy, propuesta, ADR (actual) |
| **1** | Foundation | Solución .NET 10 Clean Architecture, EF Core, tenancy, auth (Identity/JWT), CI GitHub Actions, Angular shell |
| **2** | MVP producción | Flujo OT + stock + remito + 1 PDF OT; seed/migración de esquema núcleo |
| **3** | Comercial básico | Facturas/remitos extendidos (sin FE), precios, consultas pendientes de entrega |
| **4** | Compras | `Orden_compra_proveedor`, recepción, stock desde compra |
| **5** | Costos | Recetas, costo producido, extras (reemplazo `frmControlCosto`) |
| **6** | Calidad + mantenimiento | Auditorías, desviaciones, OT mantenimiento |
| **7** | Finanzas | Caja, bancos, cheques |
| **8** | Fiscal AR | Libros IVA + **facturación electrónica** (reemplazo FEAFIPLib) |
| **9** | RRHH | Empleados, horas, sueldos |
| **10** | Reportes masivos | Portar reportes restantes a QuestPDF; retirar Crystal |
| **11** | Contable | Asientos / cuentas (si aplica al portfolio) |

Las fases 3+ pueden reordenarse según el storytelling del portfolio; el orden anterior prioriza coherencia de dominio.

---

## 3. Arquitectura propuesta

```
┌─────────────┐     HTTPS      ┌──────────────────────────────┐
│   Angular   │ ──────────────►│  .NET 10 Web API (host)      │
└─────────────┘                │  Auth + multi-tenant middleware│
                               └──────────────┬───────────────┘
                                              │
               ┌──────────────────────────────┼──────────────────────────────┐
               ▼                              ▼                              ▼
        Application                    Domain                         Infrastructure
      (use cases/CQRS)            (entities/rules)              EF Core, Service Bus,
                                                                QuestPDF, blob storage
                                              │
                                              ▼
                                    SQL Server (shared DB
                                    + TenantId)
                                              │
                              Azure Service Bus (outbox:
                              stock recalc, PDF, emails)
                              — Fase 2+
```

| Decisión | Detalle |
|---------|---------|
| API | .NET 10 Web API |
| Estilo | Clean Architecture (Domain → Application → Infrastructure → Web) |
| ORM | EF Core + SQL Server |
| Front | Angular (SPA) |
| Multi-tenant | **Base compartida + `TenantId`** (aislamiento lógico; claim JWT). ADR 0002 |
| Async | Azure Service Bus (Fase 2+) |
| Reportes | QuestPDF (Fase 2+) |
| Deploy | Azure + GitHub Actions (deploy Azure en fase posterior; CI desde Fase 1) |
| Auth | ASP.NET Identity + JWT; passwords con hash |

### Multi-tenant vs legacy

Legacy: una instalación WinForms + BD por empresa (`_Base*`, **mismo esquema / distintos datos**).  
Nuevo: una app SaaS, **una base compartida**, filas con `TenantId` (decisión del dueño).

---

## 4. Mapeo tablas legacy → modelo nuevo

Nombres nuevos en inglés en código; conceptos de negocio pueden documentarse en español.

| Legacy | Nuevo (propuesta) | Notas |
|--------|-------------------|-------|
| `Cliente` | `Customer` | |
| `Producto` | `Product` | `es_insumo` → tipo o flag `IsRawMaterial` |
| `Insumo` | `RawMaterial` o unificar en `Product` | Decidir en ADR posterior |
| `Insumo_producto` | `BillOfMaterialsLine` | |
| `Almacen` | `Warehouse` | |
| `Stock` / `Stock_insumo` | `StockBalance` | Unificar si se unifica producto/insumo |
| `Orden_compra` | `WorkOrder` (**no** PurchaseOrder) | Renombrar para claridad |
| `Cuerpo_orden_compra` | `WorkOrderLine` | |
| `Ot_interna` | `WorkOrderOperation` / `InternalWorkOrder` | |
| `Tipo_ot_interna` | `OperationType` | |
| `Remito` / `Cuerpo_remito` | `DeliveryNote` / `DeliveryNoteLine` | |
| `Recepcion` | `GoodsReceipt` | |
| `Ingreso_producto` | `ProductionReceipt` | costos embebidos o tabla hija |
| `Orden_compra_proveedor` | `PurchaseOrder` | Fase compras |
| `Factura` | `Invoice` | Fase comercial/FE |
| `Usuario` / permisos | `ApplicationUser` + policies/roles | Hash passwords; permisos por feature, no por nombre de form WinForms |
| `Empresa` | `Tenant` + `TenantProfile` | |

Estados OT: mantener códigos legibles en dominio (`InProduction`, `Delivered`, `Invoiced`, `Cancelled`) con mapeo desde `P/R/F/A` en scripts de migración de datos (si hubiera).

---

## 5. Riesgos de la migración

| Riesgo | Mitigación |
|--------|------------|
| Reglas escondidas en **1500 SP** no versionados | Extraer SP del `.mdf` en entorno local; tests de paridad; reimplementar en C# con tests |
| Ambigüedad `Orden_compra` vs compras | Renombrar en modelo nuevo; glosario en docs |
| Stock inconsistente si se portan mal los `UpdateStock*` | Transacciones + integration tests; Service Bus con idempotencia |
| Personalizaciones por cliente en reportes/UI | MVP genérico; branding por tenant (logo) en QuestPDF |
| FE AFIP | Fase tardía; no bloquear MVP |
| Alcance inflado (876 forms) | Strangler + MVP estricto; métricas de “forms no migrados” OK |
| Datos sensibles en legacy | Nunca copiar `.mdf`/credenciales al repo público; sanitizar seeds |
| Aprendizaje Angular + .NET 10 en paralelo | Vertical slice delgado en Fase 2 |

---

## 6. Criterios de éxito del MVP

- [ ] Usuario autenticado de un tenant crea una OT con líneas.
- [ ] Puede agregar operaciones internas y cambiar `E→C→T`.
- [ ] Puede emitir remito y ver pasar la OT a “remitada” cuando corresponde.
- [ ] Stock refleja movimientos esperados.
- [ ] PDF de OT descargable (QuestPDF).
- [ ] Un segundo tenant no ve datos del primero.
- [ ] Pipeline CI verde en GitHub Actions.
