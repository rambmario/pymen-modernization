@description('Azure region.')
param location string

@description('Key Vault name (globally unique).')
param name string

@description('Principal ID of the shared managed identity (secrets user).')
param managedIdentityPrincipalId string

@description('Optional deployer object ID for initial secret management (Entra user/SP).')
param deployerObjectId string = ''

@description('SQL connection string stored as a secret (password or MI form).')
@secure()
param sqlConnectionString string

@description('Service Bus connection string (RootManageSharedAccessKey or send/listen).')
@secure()
param serviceBusConnectionString string

@description('Azure Storage connection string for delivery-note blobs.')
@secure()
param storageConnectionString string

@description('JWT signing key for the API.')
@secure()
param jwtSigningKey string

@description('Resource tags.')
param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: false
    publicNetworkAccess: 'Enabled'
  }
}

// Built-in: Key Vault Secrets User
var secretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
// Built-in: Key Vault Secrets Officer (deployer bootstrap)
var secretsOfficerRoleId = 'b86a8fe4-44ce-4948-aee5-eccb2c3fc7cd'

resource miSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, managedIdentityPrincipalId, secretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', secretsUserRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource deployerSecretsOfficer 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(deployerObjectId)) {
  name: guid(keyVault.id, deployerObjectId, secretsOfficerRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', secretsOfficerRoleId)
    principalId: deployerObjectId
    principalType: 'User'
  }
}

resource sqlSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'SqlConnectionString'
  properties: {
    value: sqlConnectionString
  }
}

resource serviceBusSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ServiceBusConnectionString'
  properties: {
    value: serviceBusConnectionString
  }
}

resource storageSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'StorageConnectionString'
  properties: {
    value: storageConnectionString
  }
}

resource jwtSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'JwtSigningKey'
  properties: {
    value: jwtSigningKey
  }
}

output id string = keyVault.id
output name string = keyVault.name
output uri string = keyVault.properties.vaultUri
output sqlSecretUri string = sqlSecret.properties.secretUri
output serviceBusSecretUri string = serviceBusSecret.properties.secretUri
output storageSecretUri string = storageSecret.properties.secretUri
output jwtSecretUri string = jwtSecret.properties.secretUri
