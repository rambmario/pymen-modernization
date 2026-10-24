# Azure setup (manual — Mario)

La IA **no** crea la suscripción, **no** ejecuta `az login` ni provisiona recursos de pago. Este documento es la checklist para demo pública (Fase 2 / Paso 4–5).

Plantillas: [`infra/`](../infra/) (Bicep). Arquitectura: [ADR 0009](adr/0009-arquitectura-cloud-azure.md), [architecture-cloud.md](architecture-cloud.md).

**Auth en Azure (sin secretos de conexión):**

| Recurso | Cómo autentica la app |
|---------|------------------------|
| Azure SQL | Entra-only (`azureADOnlyAuthentication`); MI como usuario de DB |
| Service Bus | Endpoint + `DefaultAzureCredential` (Data Sender API / Data Receiver worker) |
| Blob Storage | Blob URI + MI (`allowSharedKeyAccess=false`; Reader API / Contributor worker) |
| Key Vault | Solo **JWT** residual; Secrets User → MI de la API |

Local sigue usando connection strings (SQL docker, emulador Service Bus, Azurite).

---

## Qué provisiona Bicep

| Recurso | Nombre tipico | Notas |
|---------|---------------|--------|
| Resource Group | `rg-pymen-demo` | Suscripción scope |
| User-assigned MI (×2) | `id-…-api`, `id-…-worker` | Identidades separadas (mínimo privilegio) |
| Log Analytics + App Insights | `log-…` / `appi-…` | Retención 30 días |
| Storage + container | `stpymendemo…` / `delivery-notes` | Sin shared key; RBAC Reader/Contributor |
| Service Bus Basic | `sb-…` + cola `domain-events` | `disableLocalAuth`; Sender/Receiver RBAC |
| Azure SQL serverless | `sql-…` / DB `Pymen` | **Solo Entra**; admin = tu usuario; auto-pause 60 min |
| Key Vault | `kv…` | Secret **JwtSigningKey** únicamente |
| Container Apps | `ca-api-…`, `ca-worker-…` | Consumption; scale-to-zero; env con endpoints + `AZURE_CLIENT_ID` |
| Static Web Apps | `swa-…` | Plan **Free** |
| Budget | `budget-pymen-demo` | Alerta (no hard-cap), p. ej. USD 30 |

Imágenes de contenedor por defecto son **placeholders** hasta el CI/OIDC (Paso 5).

---

## Pasos manuales (en orden)

### 1. Suscripción de Azure

1. Creá una suscripción de pago (Pay-As-You-Go u oferta con crédito) en el [portal de Azure](https://portal.azure.com/).
2. Anotá el **Subscription ID**.
3. Rol **Owner** o **Contributor + User Access Administrator** (hace falta para RBAC).

> Sin suscripción activa no se puede desplegar. Frená aquí si todavía no la tenés.

### 2. CLI y login

```powershell
winget install Microsoft.AzureCLI   # si no está
az login
az account set --subscription "<SUBSCRIPTION_ID>"
az account show
```

### 3. Providers (una vez por suscripción)

```powershell
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights
az provider register --namespace Microsoft.Insights
az provider register --namespace Microsoft.Sql
az provider register --namespace Microsoft.ServiceBus
az provider register --namespace Microsoft.Storage
az provider register --namespace Microsoft.KeyVault
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.ManagedIdentity
az provider register --namespace Microsoft.Consumption
```

### 4. Valores para el deploy (no los subas al repo)

```powershell
$jwtKey = -join ((48..57 + 65..90 + 97..122) | Get-Random -Count 48 | ForEach-Object { [char]$_ })
$myObjectId = az ad signed-in-user show --query id -o tsv
$myUpn = az ad signed-in-user show --query userPrincipalName -o tsv
$budgetEmail = "tu-email@ejemplo.com"
```

No hay `sqlAdministratorPassword`: SQL es **solo Microsoft Entra**.

### 5. Validar y desplegar Bicep

Desde la raíz del repo:

```powershell
az bicep install
az deployment sub what-if `
  --location eastus `
  --template-file infra/main.bicep `
  --parameters infra/main.bicepparam `
  --parameters sqlEntraAdminLogin=$myUpn `
  --parameters sqlEntraAdminObjectId=$myObjectId `
  --parameters jwtSigningKey=$jwtKey `
  --parameters budgetContactEmail=$budgetEmail

az deployment sub create `
  --name pymen-demo `
  --location eastus `
  --template-file infra/main.bicep `
  --parameters infra/main.bicepparam `
  --parameters sqlEntraAdminLogin=$myUpn `
  --parameters sqlEntraAdminObjectId=$myObjectId `
  --parameters jwtSigningKey=$jwtKey `
  --parameters budgetContactEmail=$budgetEmail
```

Outputs útiles:

```powershell
az deployment sub show -n pymen-demo --query properties.outputs -o json
```

Vas a necesitar: `apiUrl`, `staticWebAppUrl`, `sqlFullyQualifiedDomainName`, `apiManagedIdentityName`, `workerManagedIdentityName`.

### 6. Post-deploy — usuario SQL para cada Managed Identity (obligatorio)

SQL no tiene login/password. Hay que crear los usuarios de DB con los nombres de las MIs (outputs):

1. Conectate al server SQL con **Entra auth** (vos sos el admin de Entra).
2. Agregá tu IP al firewall del server si hace falta (`az sql server firewall-rule create …`).
3. Ejecutá el script (ajustá los nombres `[…]` a los outputs):

Plantilla: [`infra/scripts/create-sql-mi-users.sql`](../infra/scripts/create-sql-mi-users.sql).

```sql
-- Ejemplo (reemplazar nombres por outputs del deploy)
CREATE USER [id-pymen-demo-api] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [id-pymen-demo-api];
ALTER ROLE db_datawriter ADD MEMBER [id-pymen-demo-api];
ALTER ROLE db_ddladmin ADD MEMBER [id-pymen-demo-api];

CREATE USER [id-pymen-demo-worker] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [id-pymen-demo-worker];
ALTER ROLE db_datawriter ADD MEMBER [id-pymen-demo-worker];
```

Con `sqlcmd` (Entra):

```powershell
sqlcmd -S "<sqlFullyQualifiedDomainName>" -d Pymen -G -i infra/scripts/create-sql-mi-users.sql
```

### 7. Post-deploy — app real

1. **Imágenes:** en Paso 5 el CI publica API/worker; actualizá `apiImage` / `workerImage`.
2. **Migraciones EF:** como Entra admin (o con la MI de API si tiene `db_ddladmin`), desde Cloud Shell / máquina con `az login` y firewall abierto.
3. **Static Web App:** token de deploy (`az staticwebapp secrets list …`) para el workflow de Paso 5; configurar `apiUrl` en el front.
4. **CORS:** Bicep setea `Cors__Origins` al hostname de SWA.
5. **Budget:** confirmá la alerta en Cost Management; no es un tope duro — si el gasto se dispara, apagá/borrá el RG.

### 8. Apagar / borrar (control de costo)

```powershell
az group delete --name rg-pymen-demo --yes --no-wait
az consumption budget delete --budget-name budget-pymen-demo
```

---

## Modos de configuración (código)

| Setting | Local (docker compose) | Azure (ACA) |
|---------|------------------------|-------------|
| SQL | `ConnectionStrings:Pymen` con password SA | `Authentication=Active Directory Managed Identity;User Id=<clientId>` |
| Service Bus | `ServiceBus:ConnectionString` (emulator) | `ServiceBus:FullyQualifiedNamespace` + `AZURE_CLIENT_ID` |
| Blob | `AzureStorage:ConnectionString` (Azurite) | `AzureStorage:BlobServiceUri` + `AZURE_CLIENT_ID` |
| JWT | `.env` / env | Key Vault secret → ACA secretRef |

`DefaultAzureCredential` / `ManagedIdentityCredential` se usan cuando no hay connection string.

---

## Estado actual (despliegue diferido)

Por costo, **no hay suscripción Azure activa** en el portfolio por ahora (ADR 0009). El IaC y el workflow están **deploy-ready**:

- CI job **Infrastructure (Bicep)** valida `infra/main.bicep` en cada push/PR (sin cuenta Azure).
- Workflow [`.github/workflows/deploy-azure.yml`](../.github/workflows/deploy-azure.yml): solo `workflow_dispatch`; si faltan variables OIDC, el job **gate** publica un notice y **no despliega**.

---

## Activar deploy OIDC cuando exista suscripción

### 1. App registration + federated credential

1. En Entra ID: App registration (ej. `pymen-github-deploy`).
2. **Certificates & secrets → Federated credentials → GitHub Actions**:
   - Organization: `rambmario` (o tu org/user)
   - Repository: `pymen-modernization`
   - Entity: `Branch` → `main` (o Environment `azure-demo` si preferís)
3. Anotá **Application (client) ID** y **Directory (tenant) ID**.
4. En la suscripción: rol **Contributor** (+ **User Access Administrator** si el Bicep crea role assignments) sobre el SP de esa app.

### 2. Variables y secretos del repo GitHub

**Settings → Secrets and variables → Actions**

| Tipo | Nombre | Valor |
|------|--------|--------|
| Variable | `AZURE_CLIENT_ID` | Client ID de la app |
| Variable | `AZURE_TENANT_ID` | Tenant ID |
| Variable | `AZURE_SUBSCRIPTION_ID` | Subscription ID |
| Variable | `AZURE_SQL_ENTRA_ADMIN_LOGIN` | Tu UPN (admin SQL Entra) |
| Variable | `AZURE_SQL_ENTRA_ADMIN_OBJECT_ID` | Tu object id |
| Variable | `AZURE_BUDGET_CONTACT_EMAIL` | Email de alertas de budget |
| Secret | `AZURE_JWT_SIGNING_KEY` | Clave JWT (≥32 chars) |

Sin las tres primeras variables, Actions → **Deploy Azure** se puede disparar pero **sale en skip** (esperado hasta configurar).

### 3. Disparar el deploy

Actions → **Deploy Azure** → Run workflow (`environmentName=demo`). El workflow:

1. Login OIDC (`azure/login`)
2. Build/push imágenes API y worker a GHCR
3. `az deployment sub create` con `infra/main.bicep`

Después: pasos 6–7 de este doc (usuarios SQL MI, migraciones, SWA).

### 4. Observabilidad en Azure

ACA ya recibe `APPLICATIONINSIGHTS_CONNECTION_STRING` desde Bicep. La API/worker exportan OpenTelemetry a Application Insights cuando esa variable está presente (localmente usan OTLP → collector en compose, o consola).

---

## Qué no hace la IA

- No hay `az login` ni deploy ejecutado por la IA.
- No hay URL pública mientras no haya suscripción + workflow configurado.

Cuando la suscripción esté lista, configurá las variables de la tabla y corré **Deploy Azure** a mano.
