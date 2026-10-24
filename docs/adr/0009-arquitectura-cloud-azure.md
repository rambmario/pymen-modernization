# ADR 0009 — Arquitectura cloud en Azure (mínimo costo)

**Estado:** Aceptado (actualizado 2026-10-24 — despliegue diferido)  
**Fecha:** 2026-10-22  
**Decisores:** Mario

---

## Contexto

Fase 2 (Cloud & Async): hace falta una URL pública de demo, procesamiento asíncrono (PDF de remito), contenedores y observabilidad, manteniendo el **costo mensual mínimo** (tiers gratuitos o de consumo). El portfolio no justifica un AKS ni SQL provisionado 24/7.

Diagrama objetivo: `docs/architecture-cloud.md`.

### Despliegue diferido (2026-10-24)

Por **costo de suscripción Azure**, el provisionamiento real queda **aplazado**. La Fase 2 entrega el stack **deploy-ready**:

- Bicep en `/infra` + checklist en `docs/azure-setup.md` (Paso 4).
- CI valida Bicep (`az bicep build` + linter) sin cuenta Azure.
- Workflow de deploy OIDC solo `workflow_dispatch`, y se **salta** si no hay `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID`.
- Demo del flujo async con stack **local** (compose + GIF Playwright); sin URL pública por ahora.

Cuando exista suscripción, activar según `docs/azure-setup.md` (OIDC + deploy manual).

## Decisión

Diseño objetivo (cuando se active la suscripción): desplegar Pymen en Azure con estos recursos:

| Recurso | Rol | Tier / configuración |
|---------|-----|----------------------|
| **Azure Container Apps** | API (`Pymen.Api`) y worker (`Pymen.Worker`) | Consumption; scale-to-zero cuando sea posible |
| **Azure Static Web Apps** | SPA Angular | Free |
| **Azure SQL Database** | Persistencia EF Core | Serverless (GP serverless); **auto-pause**; **solo Microsoft Entra** (`azureADOnlyAuthentication = true`); **sin** `administratorLoginPassword` |
| **Azure Service Bus** | Cola de eventos de dominio | **Basic**; auth solo Entra (`disableLocalAuth`); **sin** connection strings / `listKeys` en IaC |
| **Azure Blob Storage** | PDFs de remito por tenant | StorageV2 / Hot; **`allowSharedKeyAccess = false`**; acceso vía Managed Identity |
| **Azure Key Vault** | Secreto residual **JWT** únicamente | Standard; no se guardan cadenas SQL/SB/Storage |
| **Application Insights** + Log Analytics | Trazas, métricas, logs | Workspace-based; retención corta |
| **Managed Identity** | Identidades **separadas** API y worker | User-assigned; RBAC de mínimo privilegio (ver abajo) |
| **Budget alert** | Tope de gasto en la suscripción | Alerta (no hard-cap) documentada en setup |

### Autenticación runtime (Azure)

| Dependencia | API | Worker |
|-------------|-----|--------|
| SQL | Cadena con `Authentication=Active Directory Managed Identity` + `User Id=<api clientId>` | Igual con clientId del worker |
| Service Bus | `FullyQualifiedNamespace` + `DefaultAzureCredential` → rol **Data Sender** | Mismo endpoint → rol **Data Receiver** |
| Blob | `BlobServiceUri` + MI → rol **Storage Blob Data Reader** | Mismo URI → rol **Storage Blob Data Contributor** |
| Key Vault | Secrets User (JWT) | No necesita |

Los usuarios SQL de cada MI se crean post-deploy (`CREATE USER … FROM EXTERNAL PROVIDER`); script en `infra/scripts/create-sql-mi-users.sql` y checklist en `docs/azure-setup.md`.

### Local vs Azure (código)

- **Local:** connection strings (SQL docker, emulador Service Bus, Azurite).
- **Azure:** endpoints + `DefaultAzureCredential` / Managed Identity (`AZURE_CLIENT_ID` en ACA).

Infraestructura como código: Bicep en `/infra` (Paso 4). CI/CD con OIDC (Paso 5). **Sin connection strings con clave compartida en producción.**

## Alternativas consideradas

| Alternativa | Por qué no |
|-------------|------------|
| **AKS** | Overhead operativo y costo fijo; excesivo para demo portfolio. |
| **App Service (Windows/Linux)** | Más simple que ACA para una sola app, pero peor fit para API + worker + scale-to-zero; Container Apps alinea mejor con contenedores. |
| **SQL provisionado (DTU/vCore fijo)** | Costo fijo aunque no haya tráfico; serverless + auto-pause encaja con demo esporádica. |
| **SQL con password + Entra (híbrido)** | Rechazado: secretos de conexión innecesarios; Entra-only + MI como usuario de DB. |
| **MI compartida API+worker + Data Owner** | Rechazado: mínimo privilegio pide Sender/Receiver y Reader/Contributor separados. |
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
| Key Vault | **< $1** | Solo JWT; operaciones esporádicas |
| Application Insights / Log Analytics | **~$0–5** | Retención corta; cuidado con sampling si sube el volumen |
| **Total orientativo** | **~$0–25 / mes** | Con presupuesto alerta (p. ej. $20–30) para cortar sorpresas |

**Riesgos de costo:** SQL que no pausa (conexiones abiertas), réplicas mínimas > 0 en ACA, retención larga de logs, exceso de free tier.

## Consecuencias

- **Hoy:** portfolio cloud-ready sin gasto Azure; demo local + GIF; IaC y workflow listos pero inactivos hasta configurar variables OIDC.
- **Cuando haya suscripción:** demo pública viable con gasto controlado; Mario crea suscripción, login, federated credential y variables (ver `docs/azure-setup.md`) — la IA no provisiona ni paga.
- Cold starts: SQL serverless y ACA scale-to-zero implican latencia al primer hit tras idle.
- Entra-only SQL exige un paso humano post-deploy (usuarios MI + firewall + migraciones con tu identidad Entra).
- Managed Identity + RBAC fino (Sender/Receiver, Reader/Contributor) evita secretos de plataforma en Key Vault.
- El front en Static Web Apps se desacopla del ciclo de deploy de la API/worker.
