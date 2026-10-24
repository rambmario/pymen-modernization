@description('Azure region.')
param location string

@description('Storage account name (3–24 lowercase alphanumeric).')
param name string

@description('Blob container for delivery-note PDFs.')
param deliveryNotesContainerName string = 'delivery-notes'

@description('Principal ID of the API managed identity (Blob Data Reader).')
param apiPrincipalId string

@description('Principal ID of the worker managed identity (Blob Data Contributor).')
param workerPrincipalId string

@description('Resource tags.')
param tags object = {}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowSharedKeyAccess: false
  }
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource deliveryNotes 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: deliveryNotesContainerName
  properties: {
    publicAccess: 'None'
  }
}

// Storage Blob Data Reader (API downloads PDFs)
var blobReaderRoleId = '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'
// Storage Blob Data Contributor (worker uploads PDFs)
var blobContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource apiBlobReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, apiPrincipalId, blobReaderRoleId)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', blobReaderRoleId)
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource workerBlobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, workerPrincipalId, blobContributorRoleId)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', blobContributorRoleId)
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output id string = storage.id
output name string = storage.name
output blobEndpoint string = storage.properties.primaryEndpoints.blob
output deliveryNotesContainerName string = deliveryNotes.name
