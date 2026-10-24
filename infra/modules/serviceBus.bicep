@description('Azure region.')
param location string

@description('Service Bus namespace name (globally unique).')
param namespaceName string

@description('Queue name for domain events (ADR 0010).')
param queueName string = 'domain-events'

@description('Principal ID of the API managed identity (Data Sender).')
param apiPrincipalId string

@description('Principal ID of the worker managed identity (Data Receiver).')
param workerPrincipalId string

@description('Resource tags.')
param tags object = {}

resource namespace 'Microsoft.ServiceBus/namespaces@2026-01-01' = {
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
    disableLocalAuth: true
  }
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2026-01-01' = {
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

// Azure Service Bus Data Sender
var sbSenderRoleId = '69a75ee4-2260-41d7-a5e3-49da8a390b4a'
// Azure Service Bus Data Receiver
var sbReceiverRoleId = '4f6d3b9b-027b-4f4c-9142-0e54f9ee9439'

resource apiSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(namespace.id, apiPrincipalId, sbSenderRoleId)
  scope: namespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', sbSenderRoleId)
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource workerReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(namespace.id, workerPrincipalId, sbReceiverRoleId)
  scope: namespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', sbReceiverRoleId)
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output id string = namespace.id
output name string = namespace.name
output endpoint string = namespace.properties.serviceBusEndpoint
output fullyQualifiedNamespace string = '${namespace.name}.servicebus.windows.net'
output queueName string = queue.name
