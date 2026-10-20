# ADR 0003 — Estructura de la solución (.NET 10 Clean Architecture)

**Estado:** Aceptado  
**Fecha:** 2026-10-19  
**Decisores:** Mario

---

## Contexto

Fase 1 necesita una base de código moderna antes del dominio de producción. Hay que fijar capas, dependencias y layout del repo para que los pasos siguientes (dominio, EF, API, Angular) encajen sin rearmar la solución.

## Decisión

Usar **Clean Architecture** en .NET 10 con esta estructura:

```
Pymen.sln
src/
  Pymen.Domain          # entidades y reglas; sin dependencias de infra
  Pymen.Application     # casos de uso; solo Domain
  Pymen.Infrastructure  # EF, Identity, externos; Application + Domain
  Pymen.Api             # host HTTP; Application + Infrastructure
tests/
  Pymen.Domain.Tests
  Pymen.Api.IntegrationTests
Directory.Build.props   # nullable, TreatWarningsAsErrors, LangVersion latest
.editorconfig
global.json             # SDK 10.0.x
nuget.config            # solo nuget.org (evita feeds privados rotos en CI/local)
```

**Dirección de dependencias:** Domain ← Application ← Infrastructure; Api → Application + Infrastructure. Nunca Domain/Application → Infrastructure.

**Tests:** xUnit + FluentAssertions; Domain unitario; Api con `WebApplicationFactory`.

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| Monolito único proyecto | Acopla UI/persistencia al dominio desde el día 1 |
| Vertical slice sin capas nombradas | Válido más adelante; ahora conviene capas explícitas para portfolio/enseñanza |
| Formato `.slnx` por defecto del SDK 10 | Menos soporte en tooling legado; se usa `.sln` clásico |

## Consecuencias

**Positivas:** reglas de negocio aislables; tests de dominio sin DB; CI simple (`dotnet build` / `dotnet test`).

**Negativas:** más proyectos que un starter mínimo; DI de Application/Infrastructure empieza vacío hasta Pasos 2–4.

**Seguimiento:** dominio (Paso 2), EF + tenancy (Paso 3), Identity/JWT (Paso 4).
