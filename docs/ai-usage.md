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

---

## 2026-10-19 — fix(infra) post Paso 3

### Pedido principal

Fail-closed tenant filter, FKs Restrict, test de aislamiento, password en `.env`, tests con Docker sin skips.

### Qué generó la IA

- Cambios de filtro/FKs/migración, ADR 0004, `.env.example`, tests de aislamiento, fixture SQL compartido, registro y commit.

### Qué revisó o corrigió el desarrollador

- Pendiente revisión humana del fail-closed y del set de FKs.

### Tiempo estimado ahorrado

~1.5–2 h.

---

## 2026-10-19 — Fase 1 / Paso 4

### Pedido principal

API con Identity+JWT, roles, middleware de tenant, Minimal APIs, FluentValidation, Scalar, tests (login, flujo OT, 404 cross-tenant, 403 Operator).

### Qué generó la IA

- Capa API completa, Identity, seed de usuarios, ADR 0006, tests de integración, registro.

### Qué revisó o corrigió el desarrollador

- Pendiente revisión humana de matriz de roles y contratos de endpoints.

### Tiempo estimado ahorrado

~4–6 h de wiring auth/endpoints/tests.

---

## 2026-10-19 — fix(auth) + refactor(application)

### Pedido principal

Hardening JWT/authz/rate-limit/seed y mover casos de uso a Application con tests unitarios.

### Qué generó la IA

- Commits `fix(auth)` y `refactor(application)`, servicios, IPymenDbContext, suite Application.Tests, registro.

### Qué revisó o corrigió el desarrollador

- Pendiente revisión humana de límites de rate y superficie de servicios.

### Tiempo estimado ahorrado

~2–3 h.

---

## 2026-10-19 — Paso 5 frontend Angular

### Pedido principal

OK Paso 4 → ADR 0006 trade-off + SPA Angular en `/web` (auth, OT list/detail/create, Material, tests, proxy/CORS, ADR 0007).

### Qué generó la IA

- App Angular 21, servicios/guards/interceptor, pantallas OT, CORS + operation-types, docs/tests.

### Qué revisó o corrigió el desarrollador

- Pendiente revisión humana de UX/roles y Node (Angular 21 vs 22 por versión de Node).

### Tiempo estimado ahorrado

~6–8 h.

---

## 2026-10-20 — Paginación + CI

### Pedido principal

OK Paso 5 → paginación API/web en commits separados; Paso 6 CI + Dependabot; push y resultado del primer run.

### Qué generó la IA

- Paginación server-side, refactor Angular, workflow CI, Dependabot, ADR 0008, docs.

### Qué revisó o corrigió el desarrollador

- Pendiente revisión del primer run de CI en GitHub.

### Tiempo estimado ahorrado

~3–4 h.

---

## 2026-10-20 — Follow-up Dependabot

### Pedido principal

Follow-up tras Paso 6 (triage implícito de Dependabot).

### Qué generó la IA

- Merge de 7 PRs seguros, cierre de majors, ajuste `dependabot.yml`, worklog.

### Qué revisó o corrigió el desarrollador

- Pendiente OK humano para Fase 2 / upgrade Angular.

### Tiempo estimado ahorrado

~1 h.

---

## 2026-10-20 — Paso 7 README

### Pedido principal

OK Paso 6 → README recruiter/técnico EN, LICENSE MIT, placeholders screenshots.

### Qué generó la IA

- README completo, LICENSE, docs/images guía, worklog/CHANGELOG.

### Qué revisó o corrigió el desarrollador

- Pendiente capturas reales y OK humano del tono del README.

### Tiempo estimado ahorrado

~1–1.5 h.

---

## 2026-10-22 — Fase 2 / Paso 0

### Pedido principal

Arrancar Fase 2 Cloud & Async: cerrar pregunta 8 (OT entregada con ops incompletas), ADR 0009/0010, diagrama Mermaid; commits por paso y push; no provisionar Azure ni secretos.

### Qué generó la IA

- Cierre de decisión en `owner-decisions.md` + test de dominio `MarkDelivered` con ops Pending/Started.
- ADR 0009 (cloud Azure + costos), ADR 0010 (Outbox), `docs/architecture-cloud.md`.
- Entradas worklog / ai-usage / CHANGELOG.

### Qué revisó o corrigió el desarrollador

- Pendiente OK humano antes del Paso 1 (contenedores).
- Provisioning Azure y login quedan a cargo de Mario (no ejecutados).

### Tiempo estimado ahorrado

~1.5–2 h.

---

## 2026-10-22 — Fase 2 / Paso 1 + follow-ups

### Pedido principal

OK Paso 0 → commit comando `revisar-paso`, aclarar ADR 0010 (Basic / idempotencia consumidor), luego Paso 1 contenedores.

### Qué generó la IA

- Commits chore + docs ADR; Dockerfiles API/worker/web; compose completo; worker placeholder; nginx; Config Service Bus; ajustes CORS/HTTPS/README/registro.

### Qué revisó o corrigió el desarrollador

- Pendiente OK humano Paso 1.
- Runtime: contexto Docker Linux (máquina estaba en Windows containers); puerto SPA 8088.

### Tiempo estimado ahorrado

~2–3 h.

---

## 2026-10-22 — Fase 2 / Paso 2

### Pedido principal

OK Paso 1 → domain events + Outbox + publisher Service Bus + tests (misma tx / no doble publish).

### Qué generó la IA

- Eventos de dominio, interceptor Outbox, migración, publisher/hosted service, Azure.Messaging.ServiceBus, tests y registro.

### Qué revisó o corrigió el desarrollador

- Pendiente OK humano antes del Paso 3.

### Tiempo estimado ahorrado

~2–2.5 h.

---

## 2026-10-24 — Outbox lock/retry + Docker chore

### Pedido principal

OK Paso 2 con ajustes: pin compose tags, sin curl en API; reserva multi-réplica Outbox; retries + backoff; ADR 0010; luego Paso 3.

### Qué generó la IA

- chore Docker; claim SQL + LockedUntil/LockedBy; MaxAttempts/NextAttemptAt; tests paralelo/backoff; ADR 0010.

### Qué revisó o corrigió el desarrollador

- Pendiente al cerrar Paso 3.

### Tiempo estimado ahorrado

~1.5–2 h.

---

## 2026-10-24 — Fase 2 / Paso 3

### Pedido principal

Worker + QuestPDF + Blob + descarga remito (API + Angular) + tests.

### Qué generó la IA

- Consumer, PDF, stores, Deliver→DeliveryNote, endpoint, UI, tests Infrastructure, registro.

### Qué revisó o corrigió el desarrollador

- Pendiente OK humano; screenshots Paso 7.

### Tiempo estimado ahorrado

~2.5–3.5 h.

---

## 2026-10-24 — Fase 2 / Paso 4 + conteo tests Paso 3

### Pedido principal

Agregar conteo exacto de tests al worklog del Paso 3; seguir con Paso 4 (Bicep). Frenar si hace falta suscripción/`az login`.

### Qué generó la IA

- Tabla de tests re-verificada (54 .NET + 17 Angular).
- `/infra` Bicep (RG, MI, monitoring, storage, SB, SQL serverless, KV, ACA, SWA, budget) + `docs/azure-setup.md`.
- Registro (worklog, ai-usage, CHANGELOG). Sin login ni deploy a Azure.

### Qué revisó o corrigió el desarrollador

- Pendiente OK; suscripción/login cuando quiera provisionar.

### Tiempo estimado ahorrado

~2–3 h.

---

## 2026-10-24 — Paso 4 ajuste Entra-only / MI

### Pedido principal

Ajustar Paso 4: SQL Entra-only, SB/Storage sin listKeys, MIs separadas + RBAC mínimo, dual-mode en código, actualizar ADR 0009, `/revisar-paso`, no desplegar ni Paso 5.

### Qué generó la IA

- Bicep refactor (2 UAMI, SQL AAD-only, SB/Storage sin keys, KV JWT-only, ACA endpoints).
- `Azure.Identity` + factories; Worker dual-mode; script SQL MI users; docs/ADR.
- Build/tests + checklist revisar-paso.

### Qué revisó o corrigió el desarrollador

- Pendiente OK humano; suscripción Azure pendiente.

### Tiempo estimado ahorrado

~2–2.5 h.
