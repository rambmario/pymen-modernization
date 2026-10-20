# AI usage log — Pymen Modernization

Registro de asistencia de IA (transparencia de portfolio).

---

## 2026-10-19 — Fase 0

### Pedido principal

Inicializar el repo `pymen-modernization` y producir el análisis del legacy Pymen (módulos, modelo de datos, reglas de negocio, acceso a datos, Crystal Reports, deuda técnica, métricas), propuesta de migración (MVP + arquitectura .NET 10 / Angular / Azure), ADR de strangler fig, worklog/ai-usage/CHANGELOG y commit Conventional Commits. Sin código de la app nueva. Sanitizar datos sensibles.

### Qué generó la IA

- Estructura del repositorio (README, CHANGELOG, `.gitignore`, carpetas `docs/`).
- `docs/legacy/legacy-analysis.md` (secciones a–i).
- `docs/legacy/migration-proposal.md`.
- `docs/adr/0001-estrategia-de-modernizacion.md`.
- Entradas de `docs/worklog.md` y esta bitácora.
- Exploración automatizada del árbol legacy (conteos, greps, lectura de entidades/formularios clave).

### Qué revisó o debe revisar el desarrollador

- Exactitud de estados OT (`P/R/F/A`) y OTI (`E/C/T`) frente a uso real en planta.
- Si el MVP debe incluir remito completo o solo OT + stock.
- Confirmación de que ningún secreto real quedó en el repo público.
- Completar preguntas abiertas cuando haya acceso a SQL Server / scripts de SP.

### Tiempo estimado ahorrado

~6–10 horas de inventario manual de formularios, entidades, SPs y primeros borradores de documentos.

---

## 2026-10-19 — Seguimiento subagente producción

### Pedido

Exploración en profundidad del núcleo de producción (estados, entidades, SP, cálculos, permisos, Crystal, preguntas abiertas).

### Qué generó la IA

- Informe paralelo del núcleo OT/OTI/BOM/stock ([Explore Pymen production core](05ea3a3c-ae84-43e7-8997-f75842f6274e)).
- Integración selectiva de esos hallazgos en `legacy-analysis.md` por el agente principal.

### Qué revisó o debe revisar el desarrollador

- Fórmula de consumo de receta y el flujo remito↔stock en un caso real de uso.
- Si `Nota_pedido` existe en alguna otra copia del repo o solo en BD.

### Tiempo estimado ahorrado

~1–2 horas de lectura dirigida de formularios de costo/ingreso y permisos.

---

## 2026-10-19 — Fase 1 / Paso 0

### Pedido principal

Registrar decisiones del dueño (Cliente=ventas, estado A dudoso, remito sin stock, multi-tenant shared + TenantId), actualizar preguntas abiertas y ADR.

### Qué generó la IA

- `owner-decisions.md`, ADR 0002, ediciones a `legacy-analysis.md` y `migration-proposal.md`, registro en worklog/CHANGELOG.

### Qué revisó o corrigió el desarrollador

- Las decisiones de negocio las aportó Mario; la IA solo documentó y propagó.

### Tiempo estimado ahorrado

~30–45 min de redacción cruzada entre docs.

---

## 2026-10-19 — Consolidación del historial inicial (Paso 0)

### Pedido principal

Unificar historial en un solo commit con el mensaje fijo; incluir AGENTS.md, registro.mdc y docs; verificar `git log` y `git status`.

### Qué generó la IA

- Comandos de consolidación (`reset --soft` + `amend`); entrada de worklog “Consolidación del historial inicial”; verificación.

### Qué revisó o corrigió el desarrollador

- Alcance documental y reglas del repo ya definidos por Mario; la IA solo consolidó.

### Tiempo estimado ahorrado

~15–20 min de operaciones git y chequeos.

---

## 2026-10-19 — Fase 1 / Paso 1

### Pedido principal

Crear solución .NET 10 Clean Architecture (proyectos, props, editorconfig, global.json, tests xUnit + FluentAssertions, ADR 0003).

### Qué generó la IA

- Scaffold de solución/proyectos, DI stubs, Program con health, ADR 0003, registro y commit.

### Qué revisó o corrigió el desarrollador

- Pendiente revisión humana del layout y del ADR.

### Tiempo estimado ahorrado

~1–1.5 h de armado manual de solución y pelea con NuGet/sln.

---

## 2026-10-19 — Fase 1 / Paso 2

### Pedido principal

Dominio: entidades, ITenantOwned, estados OT/operaciones, numeración, remito sin stock, ProductionReceipt con stock, tests unitarios.

### Qué generó la IA

- Código de dominio completo + suite de tests (24) + registro/CHANGELOG/commit.

### Qué revisó o corrigió el desarrollador

- Pendiente revisión de transiciones y fórmula BOM vs legacy.

### Tiempo estimado ahorrado

~2–3 h de modelado y tests de máquina de estados.

---

## 2026-10-19 — refactor(domain) + Fase 1 / Paso 3

### Pedido principal

Ajustes de dominio (Guid v7, cascade cancel, TimeProvider, props, pregunta abierta) y luego Paso 3: EF Core, TenantId, rowversion, índices únicos, test de numeración paralela.

### Qué generó la IA

- Refactor de dominio (commit previo) + capa Persistence completa, migración, docker-compose, seed, allocator con reintentos, ADR 0004, test de concurrencia, registro.

### Qué revisó o corrigió el desarrollador

- Pendiente revisión humana de filtros, índices y comportamiento del allocator bajo carga.

### Tiempo estimado ahorrado

~3–4 h de wiring EF/migraciones/Testcontainers y pelea con health/skips.
