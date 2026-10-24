targetScope = 'subscription'

@description('Azure region for most resources (SQL, ACA, Storage, KV, monitoring).')
param location string = 'eastus'

@description('Static Web Apps location (limited regions).')
param staticWebAppLocation string = 'eastus2'

@description('Short environment name used in resource names (e.g. demo).')
@minLength(2)
@maxLength(12)
param environmentName string = 'demo'

@description('Unique suffix for globally unique names (leave empty to derive from subscription).')
param uniqueSuffix string = ''

@description('Entra admin login for Azure SQL (your UPN or display name). Required — SQL is Entra-only.')
param sqlEntraAdminLogin string

@description('Entra object ID of the SQL admin (deployer user). Required — no SQL password.')
param sqlEntraAdminObjectId string

@description('JWT signing key for the API (min ~32 chars). Stored in Key Vault only.')
@secure()
param jwtSigningKey string

@description('API container image (set to your GHCR/ACR image in Paso 5+).')
param apiImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Worker container image (set to your GHCR/ACR image in Paso 5+).')
param workerImage string = 'mcr.microsoft.com/dotnet/runtime:8.0'

@description('Monthly cost budget amount (alert only; not a hard cap).')
param budgetAmount int = 30

@description('Email for budget alerts.')
param budgetContactEmail string

@description('Budget start date (first day of month), e.g. 2026-11-01.')
param budgetStartDate string = '2026-11-01'

@description('Optional Entra object ID of the deployer (Key Vault Secrets Officer). Defaults to sqlEntraAdminObjectId when empty.')
param deployerObjectId string = ''

@description('Resource tags applied to tagged resources.')
param tags object = {
  project: 'pymen'
  env: environmentName
  managedBy: 'bicep'
}

var suffix = empty(uniqueSuffix) ? take(uniqueString(subscription().subscriptionId, environmentName), 6) : uniqueSuffix
var namePrefix = 'pymen-${environmentName}'
var rgName = 'rg-${namePrefix}'
var kvDeployerObjectId = empty(deployerObjectId) ? sqlEntraAdminObjectId : deployerObjectId

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: rgName
  location: location
  tags: tags
}

module identity 'modules/identity.bicep' = {
  name: 'identity'
  scope: rg
  params: {
    location: location
    apiName: 'id-${namePrefix}-api'
    workerName: 'id-${namePrefix}-worker'
  }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    logAnalyticsName: 'log-${namePrefix}-${suffix}'
    appInsightsName: 'appi-${namePrefix}-${suffix}'
    tags: tags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    location: location
    name: take(replace('stpymen${environmentName}${suffix}', '-', ''), 24)
    deliveryNotesContainerName: 'delivery-notes'
    apiPrincipalId: identity.outputs.apiPrincipalId
    workerPrincipalId: identity.outputs.workerPrincipalId
    tags: tags
  }
}

module serviceBus 'modules/serviceBus.bicep' = {
  name: 'serviceBus'
  scope: rg
  params: {
    location: location
    namespaceName: 'sb-${namePrefix}-${suffix}'
    queueName: 'domain-events'
    apiPrincipalId: identity.outputs.apiPrincipalId
    workerPrincipalId: identity.outputs.workerPrincipalId
    tags: tags
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  scope: rg
  params: {
    location: location
    serverName: 'sql-${namePrefix}-${suffix}'
    databaseName: 'Pymen'
    entraAdminLogin: sqlEntraAdminLogin
    entraAdminObjectId: sqlEntraAdminObjectId
    entraAdminPrincipalType: 'User'
    autoPauseDelayMinutes: 60
    tags: tags
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  scope: rg
  params: {
    location: location
    name: take('kv${replace(namePrefix, '-', '')}${suffix}', 24)
    apiPrincipalId: identity.outputs.apiPrincipalId
    deployerObjectId: kvDeployerObjectId
    jwtSigningKey: jwtSigningKey
    tags: tags
  }
}

module staticWebApp 'modules/staticWebApp.bicep' = {
  name: 'staticWebApp'
  scope: rg
  params: {
    location: staticWebAppLocation
    name: 'swa-${namePrefix}-${suffix}'
    tags: tags
  }
}

module containerApps 'modules/containerApps.bicep' = {
  name: 'containerApps'
  scope: rg
  params: {
    location: location
    environmentName: 'cae-${namePrefix}-${suffix}'
    apiAppName: 'ca-api-${namePrefix}'
    workerAppName: 'ca-worker-${namePrefix}'
    logAnalyticsCustomerId: monitoring.outputs.logAnalyticsCustomerId
    logAnalyticsSharedKey: monitoring.outputs.logAnalyticsSharedKey
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    apiManagedIdentityId: identity.outputs.apiId
    apiManagedIdentityClientId: identity.outputs.apiClientId
    workerManagedIdentityId: identity.outputs.workerId
    workerManagedIdentityClientId: identity.outputs.workerClientId
    keyVaultUri: keyVault.outputs.uri
    sqlFullyQualifiedDomainName: sql.outputs.fullyQualifiedDomainName
    sqlDatabaseName: sql.outputs.databaseName
    serviceBusFullyQualifiedNamespace: serviceBus.outputs.fullyQualifiedNamespace
    serviceBusNamespaceName: serviceBus.outputs.name
    serviceBusQueueName: serviceBus.outputs.queueName
    blobServiceUri: storage.outputs.blobEndpoint
    deliveryNotesContainerName: storage.outputs.deliveryNotesContainerName
    apiImage: apiImage
    workerImage: workerImage
    corsOrigin: 'https://${staticWebApp.outputs.defaultHostname}'
    tags: tags
  }
}

module budget 'modules/budget.bicep' = {
  name: 'budget'
  scope: subscription()
  params: {
    name: 'budget-${namePrefix}'
    amount: budgetAmount
    contactEmail: budgetContactEmail
    startDate: budgetStartDate
  }
}

output resourceGroupName string = rg.name
output apiManagedIdentityName string = identity.outputs.apiName
output apiManagedIdentityClientId string = identity.outputs.apiClientId
output apiManagedIdentityPrincipalId string = identity.outputs.apiPrincipalId
output workerManagedIdentityName string = identity.outputs.workerName
output workerManagedIdentityClientId string = identity.outputs.workerClientId
output workerManagedIdentityPrincipalId string = identity.outputs.workerPrincipalId
output sqlFullyQualifiedDomainName string = sql.outputs.fullyQualifiedDomainName
output sqlDatabaseName string = sql.outputs.databaseName
output serviceBusNamespace string = serviceBus.outputs.name
output serviceBusFullyQualifiedNamespace string = serviceBus.outputs.fullyQualifiedNamespace
output serviceBusQueueName string = serviceBus.outputs.queueName
output storageAccountName string = storage.outputs.name
output blobServiceUri string = storage.outputs.blobEndpoint
output keyVaultName string = keyVault.outputs.name
output keyVaultUri string = keyVault.outputs.uri
output apiFqdn string = containerApps.outputs.apiFqdn
output apiUrl string = 'https://${containerApps.outputs.apiFqdn}'
output staticWebAppHostname string = staticWebApp.outputs.defaultHostname
output staticWebAppUrl string = 'https://${staticWebApp.outputs.defaultHostname}'
output budgetName string = budget.outputs.name
