# ADR 0007 — Frontend Angular (SPA)

**Estado:** Aceptado  
**Fecha:** 2026-10-19  
**Decisores:** Mario

---

## Contexto

Paso 5 de la Fase 1: exponer las órdenes de trabajo con autenticación JWT y roles Admin / Planner / Operator, sin Service Bus ni PDF.

## Decisión

- **Angular 21** en `/web` (standalone components, signals, control flow `@if`/`@for`, lazy routes, Angular Material, Vitest).
- Auth: login → token en **memoria + `sessionStorage`**; interceptor Bearer; redirect a `/login` ante **401**; `authGuard` + `roleGuard`.
- Pantallas: listado OT (tabla + filtros + paginación), detalle (líneas/operaciones/acciones), alta OT.
- Errores: snackbar para ProblemDetails **400/409/422**; mensajes claros en **403/404**.
- **Modelos TypeScript a mano** (`web/src/app/core/api/models.ts`) alineados a los DTOs de Application; no se genera cliente OpenAPI en Fase 1.
- Dev: **proxy** `proxy.conf.json` → API `http://localhost:5207`; **CORS** solo en Development hacia `http://localhost:4200`.
- API auxiliar: `GET /api/operation-types` (roles Admin/Planner/Operator) para el alta y el detalle.

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| Angular 22 (última CLI al momento) | Requiere Node ≥ 24.15 / 22.22; el entorno tiene Node 24.5.0. |
| Cliente OpenAPI generado | Overhead de toolchain para contratos aún chicos; modelos a mano bastan. |
| localStorage para el JWT | Persistencia más larga de lo deseado en portfolio; sessionStorage + memoria alcanza. |
| Sin CORS (solo proxy) | Útil en `ng serve`; CORS Dev cubre llamadas directas al origen de la API. |

## Consecuencias

- UI en español; código/commits en inglés.
- Operator solo ve acciones de operaciones en el detalle; create/start/deliver/invoice/cancel OT quedan para Admin/Planner.
- Al evolucionar contratos, actualizar `models.ts` (o migrar a codegen en un ADR posterior).
