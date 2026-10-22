# ADR 0010 — Mensajería asíncrona con patrón Outbox

**Estado:** Aceptado  
**Fecha:** 2026-10-22  
**Decisores:** Mario

---

## Contexto

Tras entregar una OT (y otros hechos de dominio relevantes), hace falta trabajo asíncrono (p. ej. generar el PDF del remito con QuestPDF y guardarlo en Blob). Si la API publica **directo** a Azure Service Bus dentro del request HTTP y luego falla el `SaveChanges`, o al revés, se pierden mensajes o se procesan hechos que nunca quedaron persistidos.

Eventos iniciales de dominio: `WorkOrderDelivered`, `ProductionReceiptApplied`, `WorkOrderCancelled`.

## Decisión

Usar el **patrón transactional outbox**:

1. En la **misma transacción** de EF Core que muta el agregado, se inserta una fila en tabla `Outbox` (payload serializado, tipo de evento, `OccurredAt`, estado `Pending`, `MessageId` estable para idempotencia).
2. Un **publisher** (`BackgroundService` en la API o proceso dedicado) lee pendientes, publica a **Azure Service Bus** con `MessageId` = id de outbox, y marca la fila como publicada (con reintentos).
3. El **worker** (`Pymen.Worker`) consume la cola, procesa con idempotencia (no regenerar PDF dos veces para el mismo `MessageId` / entrega), y usa dead-letter + logs estructurados ante fallos repetidos.

No se publica al bus desde el handler HTTP antes del commit.

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| **Publicar directo desde el request** | Dual-write: commit OK + publish fail (evento perdido) o publish OK + commit fail (evento fantasma). |
| **Solo domain events in-process** | No sobrevive a reinicios ni desacopla el worker; no hay cola durable. |
| **CDC / Change Feed sobre tablas de negocio** | Más infra y acoplamiento al esquema; outbox es explícito y testeable. |
| **Saga / orquestador completo** | Sobreingeniería para “generar PDF tras entrega”. |
| **Hangfire / Quartz solo** | Útil para jobs, pero no reemplaza bus + worker portable a Azure; outbox + Service Bus es el camino cloud. |

## Consecuencias

- Consistencia at-least-once hacia el bus; consumidores deben ser **idempotentes**.
- Con **Service Bus Basic** el broker **no** ofrece duplicate detection: la idempotencia la garantiza el **consumidor** (registro de `MessageId` ya procesados). **Standard** solo si más adelante se necesitan topics o duplicate detection en el broker.
- Latency eventual: el PDF no está listo en la respuesta de `MarkDelivered`; la UI descarga cuando exista (o reintenta).
- Misma unidad de persistencia (SQL) para negocio + outbox → no hace falta 2PC.
- Tests: verificar inserción outbox en la misma transacción y que el publisher no publique dos veces el mismo `MessageId`.
- Emulador de Service Bus + Azurite en docker-compose para desarrollo local (Paso 1–3).
