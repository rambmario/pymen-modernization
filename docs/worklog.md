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
