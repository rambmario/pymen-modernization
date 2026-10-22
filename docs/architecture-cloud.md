# Arquitectura cloud objetivo (Fase 2)

Diagrama de la arquitectura acordada en [ADR 0009](adr/0009-arquitectura-cloud-azure.md) y mensajería [ADR 0010](adr/0010-mensajeria-async-outbox.md).

## Vista lógica

```mermaid
flowchart TB
  subgraph Clients
    Browser["Browser / recruiter"]
  end

  subgraph Azure["Azure (demo)"]
    SWA["Azure Static Web Apps<br/>Angular SPA"]
    ACA_Env["Container Apps Environment"]

    subgraph ACA["Azure Container Apps"]
      API["Pymen.Api<br/>Minimal API + Outbox publisher"]
      Worker["Pymen.Worker<br/>QuestPDF → Blob"]
    end

    SQL[("Azure SQL<br/>serverless + auto-pause")]
    SB["Azure Service Bus<br/>queue"]
    Blob["Blob Storage<br/>delivery-note PDFs"]
    KV["Key Vault"]
    AI["Application Insights<br/>+ Log Analytics"]
  end

  Browser --> SWA
  SWA -->|"HTTPS /api"| API
  API --> SQL
  API -->|"Outbox → publish"| SB
  API -.-> KV
  Worker --> SB
  Worker --> Blob
  Worker --> SQL
  Worker -.-> KV
  API --> Blob
  API -.-> AI
  Worker -.-> AI
```

## Flujo: entrega de OT → PDF de remito

```mermaid
sequenceDiagram
  participant UI as Angular SPA
  participant API as Pymen.Api
  participant DB as Azure SQL
  participant Pub as Outbox publisher
  participant Bus as Service Bus
  participant W as Pymen.Worker
  participant Blob as Blob Storage

  UI->>API: POST deliver work order
  API->>DB: Begin tx: MarkDelivered + DeliveryNote + Outbox row
  API->>DB: Commit
  API-->>UI: 200 (OT Delivered)

  Pub->>DB: Read pending Outbox
  Pub->>Bus: Send MessageId = Outbox.Id
  Pub->>DB: Mark Outbox published

  Bus->>W: WorkOrderDelivered
  W->>DB: Load note / tenant (idempotent)
  W->>W: QuestPDF generate
  W->>Blob: Upload PDF (path per tenant)

  UI->>API: GET delivery-note PDF
  API->>Blob: Stream or short-lived SAS
  API-->>UI: PDF (404 if other tenant)
```

## Principios

- **Sin secretos en repo:** Managed Identity + OIDC en CI; Key Vault solo si hace falta.
- **Multi-tenant:** filtros `TenantId` en API y rutas de blob por tenant.
- **Costo mínimo:** scale-to-zero, SQL auto-pause, Static Web Apps Free, grants de consumo.
- **Observabilidad:** OpenTelemetry → Application Insights con `TenantId` (sin PII).
