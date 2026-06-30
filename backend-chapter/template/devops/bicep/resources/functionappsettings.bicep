@description('Existing Function App name.')
param functionAppName string

@description('Environment to deploy.')
@allowed([
  'tst'
  'prd'
])
param env string

@secure()
@description('Storage account connection string for AzureWebJobsStorage.')
param storageAccountConnectionString string

@description('Application Insights connection string.')
param appInsightsConnectionString string

@description('Service Bus fully qualified namespace.')
param serviceBusFullyQualifiedNamespace string

@description('SWAPI base URL.')
param swapiBaseUrl string

@description('Client ID of the user-assigned managed identity attached to the Function App.')
param userAssignedIdentityClientId string

var aspNetCoreEnvironment = {
  tst: 'Staging'
  prd: 'Production'
}

var appSettingsProperties = {
  ASPNETCORE_ENVIRONMENT: aspNetCoreEnvironment[env]
  WEBSITE_RUN_FROM_PACKAGE: '1'
  FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
  FUNCTIONS_EXTENSION_VERSION: '~4'
  APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
  AzureWebJobsStorage: storageAccountConnectionString
  WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: storageAccountConnectionString
  WEBSITE_CONTENTSHARE: toLower(functionAppName)

  ServiceSettings__ServiceName: 'Template.Api'
  AZURE_CLIENT_ID: userAssignedIdentityClientId
  SwapiClientSettings__BaseUrl: swapiBaseUrl
  ServiceBusOptions__StoreServiceBus__UpdatePersonQueueName: 'update-person'
  ServiceBusOptions__StoreServiceBus__FullyQualifiedNamespace: serviceBusFullyQualifiedNamespace
  ServiceBusOptions__StoreServiceBus__credential: 'managedidentity'
  ServiceBusOptions__StoreServiceBus__clientId: userAssignedIdentityClientId

  MessageRetryOptions__MaxDeliveryCount: '3'
  MessageRetryOptions__RetryDelay: '00:00:30'
  MessageRetryOptions__BackoffMultiplier: '1'
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' existing = {
  name: functionAppName
}

resource stagingSlot 'Microsoft.Web/sites/slots@2024-04-01' existing = {
  name: 'staging'
  parent: functionApp
}

resource appSettings 'Microsoft.Web/sites/config@2024-04-01' = {
  name: 'appsettings'
  parent: functionApp
  properties: appSettingsProperties
}

resource slotConfigNames 'Microsoft.Web/sites/config@2024-04-01' = {
  name: 'slotConfigNames'
  parent: functionApp
  properties: {
    appSettingNames: [
      'ServiceBusOptions__StoreServiceBus__FullyQualifiedNamespace'
    ]
  }
}

resource stagingSlotAppSettings 'Microsoft.Web/sites/slots/config@2024-04-01' = {
  name: 'appsettings'
  parent: stagingSlot
  properties: union(appSettingsProperties, {
    ServiceBusOptions__StoreServiceBus__FullyQualifiedNamespace: ''
  })
}
