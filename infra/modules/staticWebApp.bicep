@description('Static Web Apps region (limited set: e.g. eastus2, westus2, centralus, westeurope).')
param location string

@description('Static Web App name.')
param name string

@description('Resource tags.')
param tags object = {}

resource swa 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    allowConfigFileUpdates: true
    stagingEnvironmentPolicy: 'Enabled'
  }
}

output id string = swa.id
output name string = swa.name
output defaultHostname string = swa.properties.defaultHostname
