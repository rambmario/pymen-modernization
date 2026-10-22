# ADR 0009 — Arquitectura cloud en Azure (mínimo costo)

**Estado:** Aceptado  
**Fecha:** 2026-10-22  
**Decisores:** Mario

---

## Contexto

Fase 2 (Cloud & Async): hace falta una URL pública de demo, procesamiento asíncrono (PDF de remito), contenedores y observabilidad, manteniendo el **costo mensual mínimo** (tiers gratuitos o de consumo). El portfolio no justifica un AKS ni SQL provisionado 24/7.

Diagrama objetivo: `docs/architecture-cloud.md`.

## Decisión

Desplegar Pymen en Azure con estos recursos:

| Recurso | Rol | Tier / configuración |
|---------|-----|----------------------|
| **Azure Container Apps** | API (`Pymen.Api`) y worker (`Pymen.Worker`) | Consumption; scale-to-zero cuando sea posible |
| **Azure Static Web Apps** | SPA Angular | Free |
| **Azure SQL Database** | Persistencia EF Core | Serverless (GP serverless) o free offer si aplica; **auto-pause** |
| **Azure Service Bus** | Cola de eventos de dominio | **Basic** (cola única MVP; sin duplicate detection del broker — ver ADR 0010). Standard solo si hace falta topics / duplicate detection |
| **Azure Blob Storage** | PDFs de remito por tenant | StorageV2 / Hot; acceso vía Managed Identity + SAS cortas |
| **Azure Key Vault** | Secretos residuales (si hace falta) | Standard; preferir Managed Identity sobre secretos |
| **Application Insights** + Log Analytics | Trazas, métricas, logs | Workspace-based; retención corta |
| **Managed Identity** | API y worker → SQL, Bus, Blob, Key Vault | System-assigned (o user-assigned compartida) |
| **Budget alert** | Tope de gasto en la suscripción | Alerta (no hard-cap) documentada en setup |

Infraestructura como código: Bicep en `/infra` (Paso 4). CI/CD con OIDC (Paso 5). Sin connection strings con contraseña en producción.

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| **AKS** | Overhead operativo y costo fijo; excesivo para demo portfolio. |
| **App Service (Windows/Linux)** | Más simple que ACA para una sola app, pero peor fit para API + worker + scale-to-zero; Container Apps alinea mejor con contenedores. |
| **SQL provisionado (DTU/vCore fijo)** | Costo fijo aunque no haya tráfico; serverless + auto-pause encaja con demo esporádica. |
| **Una DB SQL por tenant** | Rechazado en ADR 0002 (shared DB + `TenantId`). |
| **Functions only (sin Container Apps)** | La API Minimal + Identity + EF y el worker con QuestPDF encajan mejor como contenedores .NET largos. |
| **RabbitMQ self-hosted** | Más ops; Service Bus managed + emulador local cubre portfolio. |
| **AWS / GCP** | El stack .NET + Identity tipifica mejor en Azure para este portfolio. |

## Costo estimado mensual (orden de magnitud, USD, tráfico de demo bajo)

Precios públicos varían por región y oferta; cifras orientativas para **poca actividad** (pocas visitas, auto-pause de SQL, scale-to-zero de ACA). Revisar calculadora Azure antes de provisionar.

| Recurso | Estimación demo | Notas |
|---------|-----------------|-------|
| Container Apps (API + worker) | **~$0–5** | Grant mensual gratuito (vCPU-s / GiB-s / requests); idle ≈ 0 si scale-to-zero |
| Static Web Apps | **$0** | Plan Free (límites de ancho de banda/storage) |
| Azure SQL serverless | **~$0–15** | Free offer (si disponible en la suscripción) o compute solo al despertar + storage mínimo; auto-pause crítico |
| Service Bus | **~$0–1** | Basic/cola con poco volumen; Standard si se necesitan topics más adelante |
| Blob Storage | **< $1** | Pocos PDFs; LRS |
| Key Vault | **< $1** | Operaciones esporádicas |
| Application Insights / Log Analytics | **~$0–5** | Retención corta; cuidado con sampling si sube el volumen |
| **Total orientativo** | **~$0–25 / mes** | Con presupuesto alerta (p. ej. $20–30) para cortar sorpresas |

**Riesgos de costo:** SQL que no pausa (conexiones abiertas), réplicas mínimas > 0 en ACA, retención larga de logs, exceso de free tier.

## Consecuencias

- Demo pública viable con gasto controlado; Mario debe crear suscripción, login y permisos (ver `docs/azure-setup.md` en Paso 4) — la IA no provisiona ni paga.
- Cold starts: SQL serverless y ACA scale-to-zero implican latencia al primer hit tras idle.
- Managed Identity simplifica secretos pero exige roles RBAC correctos en Bicep.
- El front en Static Web Apps se desacopla del ciclo de deploy de la API/worker.
