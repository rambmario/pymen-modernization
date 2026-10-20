# Worklog — Pymen Modernization

Registro cronológico de sesiones de trabajo.

---

## 2026-10-19 — Fase 0: análisis legacy e inicialización del repo

**Hora (aprox.):** 19:10–20:00 (UTC-3)

### Objetivo de la sesión

Inicializar `pymen-modernization` y documentar el sistema legacy Pymen (VB.NET) sin escribir código de la aplicación nueva.

### Qué se hizo

**Creados:**

- `README.md` (borrador en inglés)
- `CHANGELOG.md`
- `.gitignore` (.NET / Angular / VS + `*.bak`, `*.mdf`, `*.ldf`)
- `docs/worklog.md`, `docs/ai-usage.md`
- `docs/adr/0001-estrategia-de-modernizacion.md`
- `docs/legacy/legacy-analysis.md`
- `docs/legacy/migration-proposal.md`
- `git init` en el repo nuevo

**Analizado (solo lectura):** `D:\VBNetAnterior\Pymen` — solución, `Pymen.vbproj`, `Conexion.vb`, entidades/clases de OT/producto/stock, formularios de estados OT/OTI, inventario de módulos, métricas, reportes.

### Decisiones tomadas y por qué

- Documentación de proceso/análisis en **español**; README y commits en **inglés** (regla del proyecto).
- Tratar `Orden_compra` como **Work Order / OT de cliente** en el modelo nuevo (el nombre legacy es engañoso).
- MVP = flujo producción + 1 reporte OT (QuestPDF), no facturación electrónica ni finanzas.
- Estrategia **strangler fig** (ADR 0001).
- Sanitizar credenciales, IPs y nombres de clientes en toda documentación pública.

### Problemas encontrados y cómo se resolvieron

- SQL Server Express local no respondió: no se pudo introspeccionar el `.mdf`. Se inferió el modelo desde `Entidad`/`Clase` y se listó en preguntas abiertas.
- Detección automática de “forms por herencia” falló (herencia no estándar): se usó conteo de `frm*.vb` (~876).
- Sistema muy grande: se priorizó núcleo de producción y se dejaron huecos explícitos en “Preguntas abiertas”.

### Próximo paso

Fase 1 foundation: solution .NET 10 Clean Architecture + Angular shell + tenancy skeleton (sin migrar aún toda la lógica OT), o spike de extracción de SP desde un entorno SQL con la base de desarrollo.

---

## 2026-10-19 — Seguimiento: enriquecimiento del análisis (subagente)

**Hora (aprox.):** 19:17 (UTC-3)

### Objetivo

Incorporar hallazgos del análisis paralelo del núcleo de producción al documento legacy (sin código de app).

### Qué se hizo

- Actualizado `docs/legacy/legacy-analysis.md`: fórmula de consumo en `frmDetalleIngreso_producto.Guardar`, cálculo de costo en `frmControlCosto.btnCalcularCosto_Click`, horas de planta, `Ot_lote_quimico`, stub `Movimiento_etapa`, ACL/password en claro con rutas de archivo, preguntas abiertas refinadas (`Nota_pedido` sin capa datos, escritura de estado `A`, remito vs stock).

### Decisiones

- Mantener MVP igual; el detalle de BOM/stock refuerza que el ingreso de producción debe estar en el slice MVP.

### Problemas

- Probe SQL local ya falló (sin instancia): sin cambio.

### Próximo paso

Sin cambio: Fase 1 foundation o extracción de SP.

---

## 2026-10-19 — Historial unificado antes del primer push

**Hora (aprox.):** 19:57 (UTC-3)

### Objetivo de la sesión

Dejar un único commit limpio previo al primer push, incluyendo la regla Cursor y la sanitización de nombres de reportes.

### Qué se hizo

- `git reset --soft` a la raíz + commit nuevo (historial squash a 1 commit).
- Incluido `.cursor/rules/registro.mdc`.
- Incluido cambio de nombres de reportes en `docs/legacy/legacy-analysis.md` (`rptOT_Produccion_EmpresaC.rpt`, `OT_EmpresaD_con_membrete.rpt` en lugar de nombres reales de clientes).
- Mensaje del commit único: `docs: legacy analysis and migration proposal for Pymen`.
- Verificado con `git log` que no queden commits anteriores.

### Decisiones tomadas y por qué

- Squash antes del primer push para un historial público limpio (un solo commit de Fase 0).
- Versionar `registro.mdc` en el repo para que la regla de portfolio viaje con el proyecto.

### Problemas encontrados y cómo se resolvieron

- Había 2 commits locales + cambios sin stage; se staged todo y se recreó un commit raíz único.

### Próximo paso

Primer push del repo (cuando se configure el remoto).

---

## 2026-10-19 — Fase 1 / Paso 0: decisiones del dueño

**Hora (aprox.):** 20:02 (UTC-3)

### Objetivo de la sesión

Registrar decisiones de Mario que cierran preguntas abiertas de Fase 0 y alinear análisis/propuesta/ADR antes de crear la solución .NET.

### Qué se hizo

- Creado `docs/legacy/owner-decisions.md`.
- Actualizado inventario y preguntas abiertas en `docs/legacy/legacy-analysis.md` (remito sin stock; estado `A` dudoso; tenancy).
- Alineado `docs/legacy/migration-proposal.md` a base compartida + `TenantId`.
- Creado ADR `docs/adr/0002-multi-tenant-base-compartida.md`.
- Actualizados `CHANGELOG.md` y `docs/ai-usage.md`.

### Decisiones tomadas y por qué

- Multi-tenant shared DB porque `_Base*` ya son el mismo esquema.
- Estados OT nuevos explícitos; anulación = `Cancelled`.

### Problemas encontrados y cómo se resolvieron

- Ninguno en este paso documental.

### Próximo paso

Paso 1: solución .NET 10 Clean Architecture en `src/` + proyectos de test.

---

## 2026-10-19 — Consolidación del historial inicial

**Hora (aprox.):** 20:08 (UTC-3)

### Objetivo de la sesión

Consolidar el historial git en un único commit limpio antes del primer push (Fase 1 / Paso 0).

### Qué se hizo

- `git reset --soft` a la raíz + `git commit --amend` con el mensaje acordado.
- Incluidos `AGENTS.md`, `.cursor/rules/registro.mdc` y la documentación en `docs/`.
- Mensaje del commit: `docs: legacy analysis, migration proposal and owner decisions for Pymen`.
- Verificado `git log` (un solo commit) y `git status` (working tree limpio).

### Decisiones tomadas y por qué

- Un commit inicial público para facilitar el primer push y la lectura del portfolio.
- Documentar solo el alcance canónico acordado con el dueño.

### Problemas encontrados y cómo se resolvieron

- El historial ya estaba en un commit raíz; se reafirmó con amend tras actualizar el registro.

### Próximo paso

Fase 1 / Paso 1: solución .NET 10 Clean Architecture en `src/` (esperar OK).

---

## 2026-10-19 — Fase 1 / Paso 1: solución .NET 10

**Hora (aprox.):** 20:57 (UTC-3)

### Objetivo de la sesión

Crear la solución Clean Architecture (.NET 10) con proyectos, props compartidos, tests smoke y ADR de estructura.

### Qué se hizo

- `Pymen.sln` con `src/Pymen.{Domain,Application,Infrastructure,Api}` y `tests/Pymen.{Domain.Tests,Api.IntegrationTests}`.
- Referencias Clean Architecture; FluentAssertions + Mvc.Testing.
- `Directory.Build.props`, `.editorconfig`, `global.json`, `nuget.config` (solo nuget.org).
- ADR `docs/adr/0003-estructura-de-solucion.md`.
- Smoke tests: dominio + `/health` vía `WebApplicationFactory`.
- `dotnet build` / `dotnet test` en verde.

### Decisiones tomadas y por qué

- Formato `.sln` clásico (no `.slnx` por defecto del SDK 10) por compatibilidad.
- `nuget.config` local con nuget.org para no depender de un feed privado inaccesible.
- `TreatWarningsAsErrors` desde el inicio.

### Problemas encontrados y cómo se resolvieron

- `dotnet new sln` creó `.slnx` vacío; se regeneró con `--format sln`.
- Feed NuGet privado timeout: aislado con `nuget.config` del repo.

### Próximo paso

Paso 2 — dominio (entidades, estados OT, tests unitarios). Esperar OK.

---

## 2026-10-19 — Fase 1 / Paso 2: dominio

**Hora (aprox.):** 21:17 (UTC-3)

### Objetivo de la sesión

Implementar el dominio mínimo del núcleo de producción con estados validados, numeración por tenant y tests unitarios.

### Qué se hizo

- Entidades: `Tenant`, `Customer`, `Product`, `BillOfMaterialsLine`, `Warehouse`, `StockBalance`, `WorkOrder`, `WorkOrderLine`, `WorkOrderOperation`, `OperationType`, `DeliveryNote`, `DeliveryNoteLine`, `ProductionReceipt`.
- `ITenantOwned` en entidades de negocio; `DomainException` para reglas.
- Máquina de estados OT y operaciones; numeración POS+secuencia en `Tenant.AllocateWorkOrderNumber`.
- Remito solo documenta; `ProductionReceipt.Apply` mueve stock vía BOM.
- 24 tests unitarios en verde.

### Decisiones tomadas y por qué

- `Tenant` no implementa `ITenantOwned` (es la raíz de tenancy; su `Id` es el `TenantId` de los demás).
- Cancelación de OT solo desde `Draft`/`InProduction`; operaciones cancelables desde `Pending`/`Started`.

### Problemas encontrados y cómo se resolvieron

- Ninguno bloqueante.

### Próximo paso

Paso 3 — EF Core, filtro TenantId, docker-compose, seed. Esperar OK.

---

## 2026-10-19 — refactor(domain): TimeProvider, cancel ops, Guid v7

**Hora (aprox.):** 21:22 (UTC-3)

### Objetivo

Ajustes de dominio pedidos antes del Paso 3.

### Qué se hizo

- `Guid.CreateVersion7()` en `Entity`.
- `WorkOrder.Cancel` cancela operaciones Pending/Started.
- Fechas vía `TimeProvider`; tests con `FakeTimeProvider`.
- Props duplicadas quitadas de los `.csproj`.
- Pregunta abierta en `owner-decisions.md` sobre entregar OT con operaciones abiertas.
- Tests nuevos de cancelación en cascada y timestamps.

### Próximo paso

Paso 3 (persistencia).

---

## 2026-10-19 — Fase 1 / Paso 3: EF Core + SQL Server

**Hora (aprox.):** 21:22–21:35 (UTC-3)

### Objetivo de la sesión

Persistencia EF Core, filtros TenantId, migración inicial, docker-compose, seed ficticio, rowversion en Tenant, índices únicos y test de numeración concurrente.

### Qué se hizo

- `PymenDbContext` + Fluent configurations; query filters por tenant.
- `AmbientTenantContext`, `IWorkOrderNumberAllocator` / `WorkOrderNumberAllocator` con reintentos por `DbUpdateConcurrencyException`.
- `Tenant.RowVersion`; índices únicos `(TenantId, Number|Sku|Code)`.
- Migración `InitialCreate`; `DemoDataSeeder`; `docker-compose.yml` (SQL Server 2022).
- API: `/health` (liveness) vs `/health/ready` (DB); `SkipSeed` para smoke.
- Integration test de dos OT en paralelo (Testcontainers; skip sin Docker).
- ADR 0005 (persistencia EF/SQL Server).

### Decisiones tomadas y por qué

- Optimistic concurrency en Tenant (no bloqueo pesimista) + unique index como red de seguridad.
- Smoke de API sin SQL real (`Connect Timeout=1` + SkipSeed) para no depender de Docker en CI local.

### Problemas encontrados y cómo se resolvieron

- `Assert.Skip` en xUnit 2 colisionaba con `AsyncEnumerable.Skip` (CS0411): se usó `Xunit.SkippableFact` + `Skip.If`.
- `/health` incluía el check de DB: Predicate excluye tag `ready`.

### Próximo paso

Revisión Paso 3 / fix(infra), luego Paso 4. Esperar OK.

---

## 2026-10-19 — fix(infra): fail-closed tenant filter, FKs, .env

**Hora (aprox.):** 21:46 (UTC-3)

### Objetivo

Ajustes de revisión del Paso 3 antes del Paso 4.

### Qué se hizo

- Filtro fail-closed `CurrentTenantIdOrEmpty` aplicado a todos los `ITenantOwned` en un solo loop; seed con `IgnoreQueryFilters` explícito.
- ADR 0004 (aislamiento fail-closed); persistencia renumerada a ADR 0005.
- FKs `DeleteBehavior.Restrict` + migración `AddRestrictForeignKeys`.
- Integration test de aislamiento A vs B / sin tenant.
- Contraseña SQL en `.env` (+ `.env.example`); docker-compose y tests la leen vía `DotEnv`.
- Suite completa con Docker Desktop: **29 passed, 0 skipped** (26 domain + 3 integration).

### Próximo paso

Paso 4. Esperar OK.

---

## 2026-10-19 — Fase 1 / Paso 4: API Identity + JWT

**Hora (aprox.):** 22:26 (UTC-3)

### Objetivo

API Minimal: Identity/JWT, roles, tenant claim middleware, endpoints de catálogo/OT/ops/remitos, OpenAPI+Scalar, tests de integración.

### Qué se hizo

- Identity (`ApplicationUser` + TenantId), roles Admin/Planner/Operator, JWT con claim `tenant_id`.
- Middleware fail-closed: sin claim → 401; nunca TenantId en body/query.
- Endpoints agrupados: auth, customers, products, work-orders (+ start/deliver/invoice/cancel), operations, delivery-notes.
- FluentValidation → 400; DomainException → 422; concurrencia → 409; cross-tenant → 404.
- Seed de usuarios demo con passwords desde `.env`; Scalar + OpenAPI; migración `AddIdentity`.
- Integration tests: login, flujo OT, cross-tenant 404, Operator 403.
- ADR 0006.

### Problemas y solución

- Seed de usuarios fallaba si la DB compartida ya tenía otros tenants: factory API usa catalog `Pymen_Api_Integration`.
- Create OT en dos SaveChanges chocaba con RowVersion: allocator persiste header+líneas en un solo save + `ChangeTracker.Clear` en retries.

### Próximo paso

Paso 5+. Esperar OK.

---

## 2026-10-19 — fix(auth) + refactor(application) post Paso 4

**Hora (aprox.):** 22:56 (UTC-3)

### Objetivo

Hardening de auth y mover casos de uso a Application antes del Paso 5.

### Qué se hizo

**fix(auth):** ValidateOnStart JWT sin fallback; FallbackPolicy; rate limit login; seed Dev/`Seed:Enabled`; sin DELETE OT; tests 401/429/arranque.

**refactor(application):** `IPymenDbContext`, `CatalogService` / `WorkOrderService` / `DeliveryNoteService` (FindWorkOrder unificado); endpoints delgados; `tests/Pymen.Application.Tests`.

### Tests (Docker Desktop)

**42 passed, 0 skipped** (26 domain + 6 application + 10 integration).

### Próximo paso

Paso 5. Esperar OK.

---

## 2026-10-19 — Fase 1 / Paso 5: frontend Angular

**Hora (aprox.):** 23:07–23:20 (UTC-3)

### Objetivo

SPA de OT con auth JWT/roles, Material, tests unitarios; proxy + CORS Dev; ADR 0007.

### Qué se hizo

- ADR 0006: trade-off Application ↔ EF (`IPymenDbContext` / sin repos genéricos) — commit `docs` previo.
- `/web`: Angular 21 standalone + signals + Material + lazy routes; login; token memoria/`sessionStorage`; interceptor + guards.
- Pantallas: listado OT (filtros estado/cliente, paginación), detalle (líneas/ops + acciones por rol), alta OT.
- Snackbar ProblemDetails 400/409/422; mensajes 403/404; modelos TS a mano (ADR 0007).
- API: CORS Dev `localhost:4200`; `GET /api/operation-types`.

### Tests

- .NET (Docker): **42 passed, 0 skipped** (26 domain + 6 application + 10 integration).
- Angular (Vitest): **11 passed, 0 skipped**.

### Próximo paso

Esperar OK del Paso 5.

---

## 2026-10-20 — Paginación API/web + Paso 6 CI

**Hora (aprox.):** 07:15–07:30 (UTC-3)

### Objetivo

Paginación/filtros server-side de OT, refactor Angular, y CI GitHub Actions + Dependabot.

### Qué se hizo

- `feat(api)`: `GET /api/work-orders` → `{ items, page, pageSize, totalCount }`; filtros `status`/`customerId`; pageSize máx. 100; test integración + unitario Application.
- `refactor(web)`: lista usa paginación servidor; `takeUntilDestroyed`; `withComponentInputBinding` + `input()` en detalle; tests actualizados.
- Paso 6: `.github/workflows/ci.yml` (backend + frontend), Dependabot semanal, badge README, ADR 0008. Credenciales JWT/SQL generadas en el job.

### Tests (local)

- .NET: **44 passed, 0 skipped** (26 domain + 7 application + 11 integration).
- Angular: **11 passed, 0 skipped**.

### CI (GitHub Actions)

- Repo: https://github.com/rambmario/pymen-modernization
- Primer push: Backend falló (`setup-dotnet` cache exigía `packages.lock.json`).
- Fix `89dc95c` → run [39407553970](https://github.com/rambmario/pymen-modernization/actions/runs/39407553970): **Backend ✓ + Frontend ✓** (artifact `backend-test-results`).

### Próximo paso

Esperar OK.

---

## 2026-10-20 — Follow-up: triage Dependabot

**Hora (aprox.):** 09:46 (UTC-3)

### Objetivo

Limpiar PRs de Dependabot post Paso 6 sin meter majors a ciegas.

### Qué se hizo

**Merged (CI verde):** #21 SkippableFact, #17 TimeProvider.Testing, #19 Scalar, #2 setup-dotnet, #3 setup-node, #4 checkout, #22 cache.

**Closed (major / CI rojo):** Angular 22 piecemeal (#5–14), Vitest 5, TypeScript 7, FluentValidation 12, coverlet 10, Test.Sdk 18, xunit.runner 4, upload-artifact 7 (#1, backend rojo en ese PR).

**Config:** Dependabot con grupos + ignore de majors Angular/TS/Vitest/FluentValidation/coverlet/Test.Sdk/xunit.runner.

### Próximo paso

Esperar OK (Fase 2 o Angular 22 coordinado).
