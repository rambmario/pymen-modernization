@description('Azure region.')
param location string

@description('Logical SQL server name (globally unique).')
param serverName string

@description('Database name.')
param databaseName string = 'Pymen'

@description('Entra admin display name / UPN (deployer). Required for Entra-only auth.')
param entraAdminLogin string

@description('Entra object ID of the admin (deployer user or group).')
param entraAdminObjectId string

@description('Principal type of the Entra admin (User or Group).')
@allowed([
  'User'
  'Group'
])
param entraAdminPrincipalType string = 'User'

@description('Auto-pause delay in minutes (serverless). -1 disables pause.')
@minValue(-1)
param autoPauseDelayMinutes int = 60

@description('Resource tags.')
param tags object = {}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  tags: tags
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: entraAdminLogin
      sid: entraAdminObjectId
      tenantId: tenant().tenantId
      principalType: entraAdminPrincipalType
    }
  }
}

resource firewallAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    autoPauseDelay: autoPauseDelayMinutes
    minCapacity: json('0.5')
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
  }
}

var fqdn = sqlServer.properties.fullyQualifiedDomainName

output serverId string = sqlServer.id
output serverName string = sqlServer.name
output fullyQualifiedDomainName string = fqdn
output databaseName string = database.name
output databaseId string = database.id

@description('Connection string template for Managed Identity (substitute Client Id per app).')
output sqlConnectionStringTemplate string = 'Server=tcp:${fqdn},1433;Initial Catalog=${database.name};Authentication=Active Directory Managed Identity;User Id={CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
