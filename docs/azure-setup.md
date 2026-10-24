# Azure setup (manual — Mario)

La IA **no** crea la suscripción, **no** ejecuta `az login` ni provisiona recursos de pago. Este documento es la checklist para que vos lo hagas cuando quieras demo pública (Fase 2 / Paso 4–5).

Plantillas: [`infra/`](../infra/) (Bicep). Arquitectura: [ADR 0009](adr/0009-arquitectura-cloud-azure.md), [architecture-cloud.md](architecture-cloud.md).

---

## Qué provisiona Bicep

| Recurso | Nombre tipico | Notas |
|---------|---------------|--------|
| Resource Group | `rg-pymen-demo` | Suscripción scope |
| User-assigned MI | `id-pymen-demo` | Compartida API + worker |
| Log Analytics + App Insights | `log-…` / `appi-…` | Retención 30 días |
| Storage + container | `stpymendemo…` / `delivery-notes` | LRS; RBAC Blob Contributor → MI |
| Service Bus Basic | `sb-…` + cola `domain-events` | RBAC Data Owner → MI |
| Azure SQL serverless | `sql-…` / DB `Pymen` | Auto-pause 60 min; Entra admin = MI |
| Key Vault | `kv…` | Secretos SQL / SB / Storage / JWT; RBAC |
| Container Apps | `ca-api-…`, `ca-worker-…` | Consumption; scale-to-zero; secretos desde KV |
| Static Web Apps | `swa-…` | Plan **Free** |
| Budget | `budget-pymen-demo` | Alerta (no hard-cap), p. ej. USD 30 |

Imágenes de contenedor por defecto son **placeholders** hasta el CI/OIDC (Paso 5).

---

## Pasos manuales (en orden)

### 1. Suscripción de Azure

1. Creá una suscripción de pago (Pay-As-You-Go u oferta con crédito) en el [portal de Azure](https://portal.azure.com/).
2. Anotá el **Subscription ID**.
3. Asegurate de tener rol **Owner** o **Contributor + User Access Administrator** (hace falta para role assignments RBAC).

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

### 4. Valores secretos (no los subas al repo)

Generá en local (ejemplo):

```powershell
$sqlPassword = -join ((48..57 + 65..90 + 97..122) | Get-Random -Count 24 | ForEach-Object { [char]$_ }) + "Aa1!"
$jwtKey = -join ((48..57 + 65..90 + 97..122) | Get-Random -Count 48 | ForEach-Object { [char]$_ })
$myObjectId = az ad signed-in-user show --query id -o tsv
$budgetEmail = "tu-email@ejemplo.com"
```

### 5. Validar y desplegar Bicep

Desde la raíz del repo:

```powershell
az bicep install
az deployment sub what-if `
  --location eastus `
  --template-file infra/main.bicep `
  --parameters infra/main.bicepparam `
  --parameters sqlAdministratorPassword=$sqlPassword `
  --parameters jwtSigningKey=$jwtKey `
  --parameters budgetContactEmail=$budgetEmail `
  --parameters deployerObjectId=$myObjectId

az deployment sub create `
  --name pymen-demo `
  --location eastus `
  --template-file infra/main.bicep `
  --parameters infra/main.bicepparam `
  --parameters sqlAdministratorPassword=$sqlPassword `
  --parameters jwtSigningKey=$jwtKey `
  --parameters budgetContactEmail=$budgetEmail `
  --parameters deployerObjectId=$myObjectId
```

Outputs útiles:

```powershell
az deployment sub show -n pymen-demo --query properties.outputs -o json
```

Vas a necesitar sobre todo `apiUrl`, `staticWebAppUrl`, `keyVaultName`, `sqlFullyQualifiedDomainName`.

### 6. Post-deploy (app real)

1. **Imágenes:** en Paso 5 el CI publica API/worker; actualizá parámetros `apiImage` / `workerImage` (o un redeploy) con esas referencias.
2. **Migraciones EF:** contra Azure SQL (cadena en Key Vault `SqlConnectionString`), con firewall: o IP temporal en el server SQL, o ejecutar desde Cloud Shell / pipeline.
3. **Usuario SQL para MI (recomendado):** mapear la managed identity a un usuario en la DB (`CREATE USER … FROM EXTERNAL PROVIDER`) y roles `db_datareader` / `db_datawriter` / etc.; luego rotar la cadena a `Authentication=Active Directory Managed Identity`.
4. **Static Web App:** token de deploy (`az staticwebapp secrets list …`) para el workflow de Paso 5; configurar `apiUrl` en el front.
5. **CORS:** Bicep setea `Cors__Origins` al hostname de SWA; si la API aún no lee esa config, alinearla en código en un paso posterior.
6. **Budget:** confirmá en Cost Management que la alerta llegó al mail; no es un tope duro — si el gasto se dispara, apagá/borrá el RG.

### 7. Apagar / borrar (control de costo)

```powershell
az group delete --name rg-pymen-demo --yes --no-wait
# El budget queda a nivel suscripción; borrarlo si ya no aplica:
az consumption budget delete --budget-name budget-pymen-demo
```

---

## Qué no hace este paso

- No hay `az login` ni deploy ejecutado por la IA.
- No hay OIDC de GitHub Actions (Paso 5).
- No hay URL de demo estable hasta que existan imágenes reales + migraciones + SWA deploy.

Cuando la suscripción esté lista y hayas hecho login, avisá y seguimos con el deploy o con el Paso 5 (CI/CD).
