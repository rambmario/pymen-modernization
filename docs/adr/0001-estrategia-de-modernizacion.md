# ADR 0001 — Estrategia de modernización (strangler fig)

**Estado:** Aceptado  
**Fecha:** 2026-10-19  
**Decisores:** Mario (owner del portfolio)

---

## Contexto

Pymen es un monolito WinForms (.NET Framework 3.5) con ~876 formularios, ~298 tablas mapeadas, ~1500 stored procedures y cientos de reportes Crystal. Una reescritura “big bang” concentraría demasiado riesgo: reglas de stock/costo ocultas en SQL, y meses sin demo usable.

El proyecto es de **portfolio**: necesita entregables visibles por fases, documentación pública y un MVP creíble, no paridad total día uno.

## Decisión

Adoptar el patrón **strangler fig**:

1. Documentar el legacy (Fase 0) sin escribir aún la app nueva.
2. Levantar el esqueleto moderno (.NET 10 + Angular + Azure) en paralelo.
3. Migrar **porciones verticales** de dominio (empezando por el flujo de OT/producción).
4. Reemplazar Crystal Reports gradualmente (QuestPDF), empezando por un reporte MVP.
5. No intentar reescribir los 876 formularios ni portar todos los SP de una vez.
6. El sistema legacy permanece como referencia de solo lectura; no se modifica.

## Alternativas consideradas

| Alternativa | Por qué se descartó |
|-------------|---------------------|
| Big bang rewrite | Alto riesgo, largo tiempo sin valor demostrable |
| Lift-and-shift a .NET 4.8 WinForms | No moderniza UX/arquitectura ni sirve como portfolio cloud |
| Solo migrar a WPF/MAUI | Sigue acoplado a escritorio; no muestra Web API/Angular/Azure |
| Reutilizar BD + SP tal cual detrás de API | Congela deuda SQL; dificulta multi-tenant limpio |

## Consecuencias

**Positivas**

- Demos incrementales alineadas al roadmap.
- Aprendizaje continuo de reglas al migrar cada slice.
- Menor presión por paridad 1:1 con UI WinForms.

**Negativas / trade-offs**

- Durante un tiempo habrá dos “mundos” conceptuales (legacy vs nuevo).
- Hay que vivir con preguntas abiertas hasta abrir los SP en un entorno SQL.
- Algunos módulos (FE AFIP, contable) se posponen mucho.

**Seguimiento**

- MVP definido en `docs/legacy/migration-proposal.md`.
- Nuevos ADR por decisiones de tenancy, unificación Producto/Insumo, y auth.
