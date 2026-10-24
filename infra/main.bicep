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

@description('SQL administrator login (bootstrap; runtime prefers Managed Identity).')
param sqlAdministratorLogin string = 'pymenadmin'

@description('SQL administrator password (secure; never commit real values).')
@secure()
param sqlAdministratorPassword string

@description('JWT signing key for the API (min ~32 chars).')
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

@description('Optional Entra object ID of the deployer (Key Vault Secrets Officer).')
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
    name: 'id-${namePrefix}'
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
    managedIdentityPrincipalId: identity.outputs.principalId
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
    managedIdentityPrincipalId: identity.outputs.principalId
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
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    aadAdminLogin: identity.outputs.name
    aadAdminObjectId: identity.outputs.principalId
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
    managedIdentityPrincipalId: identity.outputs.principalId
    deployerObjectId: deployerObjectId
    sqlConnectionString: sql.outputs.sqlConnectionString
    serviceBusConnectionString: serviceBus.outputs.connectionString
    storageConnectionString: storage.outputs.connectionString
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
    managedIdentityId: identity.outputs.id
    managedIdentityClientId: identity.outputs.clientId
    keyVaultUri: keyVault.outputs.uri
    serviceBusQueueName: serviceBus.outputs.queueName
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
output managedIdentityClientId string = identity.outputs.clientId
output managedIdentityPrincipalId string = identity.outputs.principalId
output sqlFullyQualifiedDomainName string = sql.outputs.fullyQualifiedDomainName
output sqlDatabaseName string = sql.outputs.databaseName
output serviceBusNamespace string = serviceBus.outputs.name
output serviceBusQueueName string = serviceBus.outputs.queueName
output storageAccountName string = storage.outputs.name
output keyVaultName string = keyVault.outputs.name
output keyVaultUri string = keyVault.outputs.uri
output apiFqdn string = containerApps.outputs.apiFqdn
output apiUrl string = 'https://${containerApps.outputs.apiFqdn}'
output staticWebAppHostname string = staticWebApp.outputs.defaultHostname
output staticWebAppUrl string = 'https://${staticWebApp.outputs.defaultHostname}'
output budgetName string = budget.outputs.name
