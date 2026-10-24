@description('Azure region for the user-assigned managed identity.')
param location string

@description('Name of the user-assigned managed identity shared by API and worker.')
param name string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
}

output id string = identity.id
output name string = identity.name
output principalId string = identity.properties.principalId
output clientId string = identity.properties.clientId
