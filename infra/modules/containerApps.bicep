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

@description('API user-assigned managed identity resource ID.')
param apiManagedIdentityId string

@description('API user-assigned managed identity client ID.')
param apiManagedIdentityClientId string

@description('Worker user-assigned managed identity resource ID.')
param workerManagedIdentityId string

@description('Worker user-assigned managed identity client ID.')
param workerManagedIdentityClientId string

@description('Key Vault URI (https://...vault.azure.net/).')
param keyVaultUri string

@description('SQL FQDN (server.database.windows.net).')
param sqlFullyQualifiedDomainName string

@description('SQL database name.')
param sqlDatabaseName string = 'Pymen'

@description('Service Bus fully qualified namespace (name.servicebus.windows.net).')
param serviceBusFullyQualifiedNamespace string

@description('Service Bus namespace name (for KEDA scaler metadata).')
param serviceBusNamespaceName string

@description('Service Bus queue name.')
param serviceBusQueueName string = 'domain-events'

@description('Blob service URI (https://account.blob.core.windows.net/).')
param blobServiceUri string

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

var apiSqlConnectionString = 'Server=tcp:${sqlFullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Managed Identity;User Id=${apiManagedIdentityClientId};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var workerSqlConnectionString = 'Server=tcp:${sqlFullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Managed Identity;User Id=${workerManagedIdentityClientId};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource managedEnv 'Microsoft.App/managedEnvironments@2025-07-01' = {
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

resource api 'Microsoft.App/containerApps@2025-07-01' = {
  name: apiAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiManagedIdentityId}': {}
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
          name: 'jwt-signing-key'
          keyVaultUrl: '${keyVaultUri}secrets/JwtSigningKey'
          identity: apiManagedIdentityId
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
              value: apiSqlConnectionString
            }
            {
              name: 'ServiceBus__FullyQualifiedNamespace'
              value: serviceBusFullyQualifiedNamespace
            }
            {
              name: 'ServiceBus__QueueName'
              value: serviceBusQueueName
            }
            {
              name: 'AzureStorage__BlobServiceUri'
              value: blobServiceUri
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
              value: apiManagedIdentityClientId
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

resource worker 'Microsoft.App/containerApps@2025-07-01' = {
  name: workerAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${workerManagedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
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
              value: workerSqlConnectionString
            }
            {
              name: 'ServiceBus__FullyQualifiedNamespace'
              value: serviceBusFullyQualifiedNamespace
            }
            {
              name: 'ServiceBus__QueueName'
              value: serviceBusQueueName
            }
            {
              name: 'AzureStorage__BlobServiceUri'
              value: blobServiceUri
            }
            {
              name: 'AzureStorage__DeliveryNotesContainer'
              value: deliveryNotesContainerName
            }
            {
              name: 'Outbox__PublisherEnabled'
              value: 'false'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: workerManagedIdentityClientId
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
            // identity is supported at runtime for MI auth; Bicep types lag behind (no connection string).
            custom: union({
              type: 'azure-servicebus'
              metadata: {
                queueName: serviceBusQueueName
                namespace: serviceBusNamespaceName
                messageCount: '1'
              }
            }, {
              identity: workerManagedIdentityId
            })
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
