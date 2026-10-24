@description('Azure region.')
param location string

@description('Log Analytics workspace name.')
param logAnalyticsName string

@description('Application Insights component name.')
param appInsightsName string

@description('Resource tags.')
param tags object = {}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output logAnalyticsId string = logAnalytics.id
output logAnalyticsCustomerId string = logAnalytics.properties.customerId
@secure()
output logAnalyticsSharedKey string = logAnalytics.listKeys().primarySharedKey
output appInsightsId string = appInsights.id
@secure()
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
