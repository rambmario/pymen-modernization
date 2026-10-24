@description('Azure region.')
param location string

@description('Container Apps Environment name.')
param environmentName string

@description('API Container App name.')
param apiAppName string

@description('Worker Container App name.')
param workerAppName string

@description('Log Analytics workspace customer ID.')
param logAnalyticsCustomerId string

@description('Log Analytics primary shared key.')
@secure()
param logAnalyticsSharedKey string

@description('Application Insights connection string.')
@secure()
param appInsightsConnectionString string

@description('User-assigned managed identity resource ID.')
param managedIdentityId string

@description('User-assigned managed identity client ID.')
param managedIdentityClientId string

@description('Key Vault URI (https://...vault.azure.net/).')
param keyVaultUri string

@description('Service Bus queue name.')
param serviceBusQueueName string = 'domain-events'

@description('Blob container for delivery notes.')
param deliveryNotesContainerName string = 'delivery-notes'

@description('API container image (GHCR/ACR; placeholder until Paso 5 CI).')
param apiImage string

@description('Worker container image (GHCR/ACR; placeholder until Paso 5 CI).')
param workerImage string

@description('CORS origin for the Angular SPA (Static Web App URL).')
param corsOrigin string = ''

@description('Resource tags.')
param tags object = {}

resource managedEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsSharedKey
      }
    }
  }
}

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      secrets: [
        {
          name: 'sql-connection'
          keyVaultUrl: '${keyVaultUri}secrets/SqlConnectionString'
          identity: managedIdentityId
        }
        {
          name: 'servicebus-connection'
          keyVaultUrl: '${keyVaultUri}secrets/ServiceBusConnectionString'
          identity: managedIdentityId
        }
        {
          name: 'storage-connection'
          keyVaultUrl: '${keyVaultUri}secrets/StorageConnectionString'
          identity: managedIdentityId
        }
        {
          name: 'jwt-signing-key'
          keyVaultUrl: '${keyVaultUri}secrets/JwtSigningKey'
          identity: managedIdentityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__Pymen'
              secretRef: 'sql-connection'
            }
            {
              name: 'ServiceBus__ConnectionString'
              secretRef: 'servicebus-connection'
            }
            {
              name: 'ServiceBus__QueueName'
              value: serviceBusQueueName
            }
            {
              name: 'AzureStorage__ConnectionString'
              secretRef: 'storage-connection'
            }
            {
              name: 'AzureStorage__DeliveryNotesContainer'
              value: deliveryNotesContainerName
            }
            {
              name: 'Jwt__SigningKey'
              secretRef: 'jwt-signing-key'
            }
            {
              name: 'Jwt__Issuer'
              value: 'pymen'
            }
            {
              name: 'Jwt__Audience'
              value: 'pymen-api'
            }
            {
              name: 'Outbox__PublisherEnabled'
              value: 'true'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentityClientId
            }
            {
              name: 'Cors__Origins'
              value: corsOrigin
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 2
        rules: [
          {
            name: 'http-scale'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

resource worker 'Microsoft.App/containerApps@2024-03-01' = {
  name: workerAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: [
        {
          name: 'sql-connection'
          keyVaultUrl: '${keyVaultUri}secrets/SqlConnectionString'
          identity: managedIdentityId
        }
        {
          name: 'servicebus-connection'
          keyVaultUrl: '${keyVaultUri}secrets/ServiceBusConnectionString'
          identity: managedIdentityId
        }
        {
          name: 'storage-connection'
          keyVaultUrl: '${keyVaultUri}secrets/StorageConnectionString'
          identity: managedIdentityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'worker'
          image: workerImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'DOTNET_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__Pymen'
              secretRef: 'sql-connection'
            }
            {
              name: 'ServiceBus__ConnectionString'
              secretRef: 'servicebus-connection'
            }
            {
              name: 'ServiceBus__QueueName'
              value: serviceBusQueueName
            }
            {
              name: 'AzureStorage__ConnectionString'
              secretRef: 'storage-connection'
            }
            {
              name: 'AzureStorage__DeliveryNotesContainer'
              value: deliveryNotesContainerName
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentityClientId
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
        rules: [
          {
            name: 'servicebus-scale'
            custom: {
              type: 'azure-servicebus'
              metadata: {
                queueName: serviceBusQueueName
                messageCount: '1'
              }
              auth: [
                {
                  secretRef: 'servicebus-connection'
                  triggerParameter: 'connection'
                }
              ]
            }
          }
        ]
      }
    }
  }
}

output environmentId string = managedEnv.id
output apiFqdn string = api.properties.configuration.ingress.fqdn
output apiId string = api.id
output workerId string = worker.id
