@description('Azure region for the user-assigned managed identities.')
param location string

@description('Name of the API managed identity.')
param apiName string

@description('Name of the worker managed identity.')
param workerName string

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: apiName
  location: location
}

resource workerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: workerName
  location: location
}

output apiId string = apiIdentity.id
output apiName string = apiIdentity.name
output apiPrincipalId string = apiIdentity.properties.principalId
output apiClientId string = apiIdentity.properties.clientId

output workerId string = workerIdentity.id
output workerName string = workerIdentity.name
output workerPrincipalId string = workerIdentity.properties.principalId
output workerClientId string = workerIdentity.properties.clientId
