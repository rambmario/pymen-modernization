@description('Azure region.')
param location string

@description('Logical SQL server name (globally unique).')
param serverName string

@description('Database name.')
param databaseName string = 'Pymen'

@description('SQL admin login (bootstrap only; prefer Managed Identity at runtime).')
param administratorLogin string

@description('SQL admin password (bootstrap only).')
@secure()
param administratorLoginPassword string

@description('Display name for the Entra admin (managed identity name).')
param aadAdminLogin string

@description('Object ID (principal ID) of the managed identity used as Entra admin.')
param aadAdminObjectId string

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
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: false
      login: aadAdminLogin
      sid: aadAdminObjectId
      tenantId: tenant().tenantId
      principalType: 'Application'
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
var sqlConnectionString = 'Server=tcp:${fqdn},1433;Initial Catalog=${database.name};User ID=${administratorLogin};Password=${administratorLoginPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

output serverId string = sqlServer.id
output serverName string = sqlServer.name
output fullyQualifiedDomainName string = fqdn
output databaseName string = database.name
output databaseId string = database.id
@secure()
output sqlConnectionString string = sqlConnectionString
