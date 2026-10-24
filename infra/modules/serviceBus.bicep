@description('Azure region.')
param location string

@description('Service Bus namespace name (globally unique).')
param namespaceName string

@description('Queue name for domain events (ADR 0010).')
param queueName string = 'domain-events'

@description('Principal ID of the shared managed identity.')
param managedIdentityPrincipalId string

@description('Resource tags.')
param tags object = {}

resource namespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: namespace
  name: queueName
  properties: {
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
    requiresDuplicateDetection: false
    requiresSession: false
  }
}

resource rootRule 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2022-10-01-preview' = {
  parent: namespace
  name: 'RootManageSharedAccessKey'
  properties: {
    rights: [
      'Listen'
      'Manage'
      'Send'
    ]
  }
}

// Azure Service Bus Data Owner (send + listen for shared MI; tighten later if split identities)
var sbDataOwnerRoleId = '090c5cfd-751d-490a-894a-3ce6f1109419'

resource sbRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(namespace.id, managedIdentityPrincipalId, sbDataOwnerRoleId)
  scope: namespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', sbDataOwnerRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

var keys = rootRule.listKeys()
var connectionString = keys.primaryConnectionString

output id string = namespace.id
output name string = namespace.name
output endpoint string = namespace.properties.serviceBusEndpoint
output queueName string = queue.name
@secure()
output connectionString string = connectionString
