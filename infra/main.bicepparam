using 'main.bicep'

param location = 'eastus'
param staticWebAppLocation = 'eastus2'
param environmentName = 'demo'
param uniqueSuffix = ''
param budgetAmount = 30
param budgetStartDate = '2026-11-01'
param apiImage = 'mcr.microsoft.com/dotnet/samples:aspnetapp'
param workerImage = 'mcr.microsoft.com/dotnet/runtime:8.0'
param deployerObjectId = ''

// Pass at deploy time (never commit real values):
//   sqlEntraAdminLogin, sqlEntraAdminObjectId, jwtSigningKey, budgetContactEmail
