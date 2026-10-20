# Pymen web (Angular)

SPA for work-order management against the Pymen API.

## Stack

- Angular 21 (standalone, signals, `@if`/`@for`, lazy routes)
- Angular Material
- Vitest unit tests

API DTOs are **hand-written TypeScript models** under `src/app/core/api/models.ts` (mirroring Application DTOs). OpenAPI codegen was skipped for Fase 1 simplicity; regenerate manually when contracts change.

## Dev

1. Start the API (`http` profile → `http://localhost:5207`).
2. From this folder: `npm start` (proxy → API via `proxy.conf.json`).
3. Open `http://localhost:4200`.

Demo users: `admin.t1@demo.local`, `planner.t1@demo.local`, `operator.t1@demo.local` (passwords from repo `.env`).

## Tests

```bash
npm test
```
