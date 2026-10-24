@description('Azure region.')
param location string

@description('Key Vault name (globally unique).')
param name string

@description('Principal ID of the API managed identity (JWT secret reader).')
param apiPrincipalId string

@description('Optional deployer object ID for initial secret management (Entra user/SP).')
param deployerObjectId string = ''

@description('JWT signing key for the API (only residual secret — no SQL/SB/Storage connection strings).')
@secure()
param jwtSigningKey string

@description('Resource tags.')
param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' = {
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

resource apiSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiPrincipalId, secretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', secretsUserRoleId)
    principalId: apiPrincipalId
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

resource jwtSecret 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: keyVault
  name: 'JwtSigningKey'
  properties: {
    value: jwtSigningKey
  }
}

output id string = keyVault.id
output name string = keyVault.name
output uri string = keyVault.properties.vaultUri
output jwtSecretUri string = jwtSecret.properties.secretUri
