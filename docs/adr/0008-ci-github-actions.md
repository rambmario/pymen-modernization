# ADR 0008 — CI con GitHub Actions

**Estado:** Aceptado  
**Fecha:** 2026-10-20  
**Decisores:** Mario

---

## Contexto

Paso 6 de la Fase 1: hay API .NET, tests con Testcontainers y SPA Angular. Hace falta un pipeline reproducible en cada push/PR sin secretos reales en el repo.

## Decisión

- Workflow `.github/workflows/ci.yml` en **push a `main`** y **pull_request**.
- Job **backend**: `setup-dotnet` desde `global.json`, restore, build **Release** (warnings as errors vía `Directory.Build.props`), `dotnet test` con Testcontainers (Docker en `ubuntu-latest`), artifact de TRX + cobertura Coverlet.
- Job **frontend**: `setup-node` con cache npm, `npm ci`, lint si existe script, Vitest, build de producción.
- Job **Infrastructure (Bicep)** (Fase 2): `az bicep lint` + `az bicep build` sobre `infra/main.bicep` **sin** cuenta Azure; falla si el linter reporta Error o Warning.
- Credenciales de test (**JWT** y **MSSQL_SA_PASSWORD**) **generadas en el job**; no hay secrets de producción en el workflow.
- Deploy a Azure: workflow aparte `deploy-azure.yml` (solo `workflow_dispatch`; se salta sin variables OIDC — ver ADR 0009 / `docs/azure-setup.md`).
- **Dependabot** semanal para NuGet, npm (`/web`) y GitHub Actions.
- Badge de CI en el README.

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| Azure Pipelines | Overhead extra para portfolio; GitHub Actions alcanza. |
| Secretos de repo para JWT/SQL de tests | Innecesario; se pueden generar efímeros por run. |
| Un solo job monolítico | Fallas de front/back menos claras; paralelismo perdido. |

## Consecuencias

- Los tests de integración dejan de skippearse en CI (Docker disponible).
- Cache de NuGet (setup-dotnet) y npm acelera runs posteriores.
- Dependabot abrirá PRs semanales; conviene revisarlos antes de merge.
