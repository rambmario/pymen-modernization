# ADR 0010 — Mensajería asíncrona con patrón Outbox

**Estado:** Aceptado  
**Fecha:** 2026-10-22 (actualizado 2026-10-24)  
**Decisores:** Mario

---

## Contexto

Tras entregar una OT (y otros hechos de dominio relevantes), hace falta trabajo asíncrono (p. ej. generar el PDF del remito con QuestPDF y guardarlo en Blob). Si la API publica **directo** a Azure Service Bus dentro del request HTTP y luego falla el `SaveChanges`, o al revés, se pierden mensajes o se procesan hechos que nunca quedaron persistidos.

Eventos iniciales de dominio: `WorkOrderDelivered`, `ProductionReceiptApplied`, `WorkOrderCancelled`.

En Container Apps puede haber **varias réplicas** del publisher; sin reserva de filas, dos instancias podrían enviar el mismo `MessageId`. Los fallos transitorios del bus requieren reintentos con backoff, no un bucle agresivo.

## Decisión

Usar el **patrón transactional outbox**:

1. En la **misma transacción** de EF Core que muta el agregado, se inserta una fila en `OutboxMessages` (payload serializado, tipo de evento, `OccurredAt`, estado `Pending`, id estable = `MessageId` de Service Bus).
2. Un **publisher** (`BackgroundService`) **reserva** un lote de pendientes con SQL Server `UPDLOCK, READPAST, ROWLOCK` + `OUTPUT` (`LockedUntil` / `LockedBy`), publica a **Azure Service Bus** con `MessageId` = id de outbox, y marca `Published`.
3. Ante error de publicación: incrementa `AttemptCount`, calcula `NextAttemptAt` con **backoff exponencial** (`min(maxBackoff, 2^attempt)` segundos), libera el lock; al superar `MaxAttempts` (configurable, default **5**) pasa a `Failed` y se registra error estructurado.
4. El **worker** (`Pymen.Worker`) consume la cola con idempotencia (registro de `MessageId` procesados), dead-letter y logs estructurados.

No se publica al bus desde el handler HTTP antes del commit.

### Multi-réplica (reserva)

- No alcanza con “leer Pending y luego actualizar”: dos réplicas pueden leer la misma fila.
- Decisión: claim atómico en SQL (`UPDLOCK` + `READPAST`) antes de `Send`. Filas con lock vigente se saltan; locks expirados se pueden reclamar (crash recovery).

### Reintentos

- `MaxAttempts` y tope de backoff configurables (`Outbox` options).
- `NextAttemptAt` evita reintentar en cada poll hasta que venza el backoff.
- Estado terminal `Failed` (no se reintenta más); operación/alerta vía logs (y más adelante DLQ operativa si hace falta).

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| **Publicar directo desde el request** | Dual-write: commit OK + publish fail (evento perdido) o publish OK + commit fail (evento fantasma). |
| **Solo domain events in-process** | No sobrevive a reinicios ni desacopla el worker; no hay cola durable. |
| **CDC / Change Feed sobre tablas de negocio** | Más infra y acoplamiento al esquema; outbox es explícito y testeable. |
| **Saga / orquestador completo** | Sobreingeniería para “generar PDF tras entrega”. |
| **Hangfire / Quartz solo** | Útil para jobs, pero no reemplaza bus + worker portable a Azure; outbox + Service Bus es el camino cloud. |
| **Estado `Processing` sin `LockedUntil`** | Tras crash de la réplica el mensaje queda huérfano; el lock con TTL permite reclaim. |
| **Duplicate detection del broker (Standard)** | Service Bus **Basic** no lo ofrece; la idempotencia del envío multi-réplica la resolvemos en SQL + la del consumo en el worker. Standard solo si más adelante se necesitan topics o duplicate detection en el broker. |

## Consecuencias

- Consistencia at-least-once hacia el bus; consumidores deben ser **idempotentes**.
- Con **Service Bus Basic** el broker **no** ofrece duplicate detection: la idempotencia del **consumidor** es registro de `MessageId` ya procesados.
- Varias réplicas del publisher no envían el mismo mensaje (test de dos publishers en paralelo).
- Latency eventual: el PDF no está listo en la respuesta de `MarkDelivered`; la UI descarga cuando exista (o reintenta).
- Misma unidad de persistencia (SQL) para negocio + outbox → no hace falta 2PC.
- Tests: outbox en la misma transacción; no doble publish secuencial; no doble publish en paralelo; Failed tras max intentos + backoff.
- Emulador de Service Bus + Azurite en docker-compose para desarrollo local.
