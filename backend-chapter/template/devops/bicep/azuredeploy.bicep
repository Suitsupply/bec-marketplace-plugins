targetScope = 'resourceGroup'

@description('Environment to deploy.')
@allowed([
  'tst'
  'prd'
])
param env string

@description('Azure location for all resources.')
param location string = resourceGroup().location

@description('Name of the function app.')
param functionAppName string

@description('Name of the user-assigned managed identity for the Function App.')
param managedIdentityName string

@description('Name of the app service plan.')
param appServicePlanName string

@description('Name of the Application Insights instance.')
param appInsightsName string

@description('Log Analytics Workspace resource ID for Application Insights.')
param logAnalyticsWorkspaceId string

@description('Object IDs of Azure AD groups to assign the Contributor role on this resource group.')
param contributorPrincipalIds array = []

@description('Team Key Vault name. Used for Key Vault references in app settings.')
param teamKeyVault string

@description('Name of the storage account to create for Function App WebJobs storage. Must be globally unique, lowercase, alphanumeric, 3-24 chars. Example: "shopifyintegrationtstsa"')
param storageAccountName string

@description('Existing Service Bus namespace name (without .servicebus.windows.net).')
param serviceBusNamespaceName string

@description('Team name tag applied to all resources.')
param teamNameTag string

@description('SWAPI base URL.')
param swapiBaseUrl string = 'https://swapi.info/api/'

var contributorRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
var moduleName = 'azuredeploy'

module storageAccountModule './resources/storageaccount.bicep' = {
  name: '${moduleName}-storageaccount'
  params: {
    name: storageAccountName
    location: location
    teamTag: teamNameTag
  }
}

module hostingPlanModule './resources/hostingplan.bicep' = {
  name: '${moduleName}-hostingplan'
  params: {
    name: appServicePlanName
    location: location
    teamTag: teamNameTag
  }
}

module appInsightsModule './resources/appinsights.bicep' = {
  name: '${moduleName}-appinsights'
  params: {
    name: appInsightsName
    location: location
    logAnalyticsWorkspaceId: logAnalyticsWorkspaceId
    teamTag: teamNameTag
  }
}

module userAssignedIdentityModule './resources/userassignedidentity.bicep' = {
  name: '${moduleName}-userassignedidentity'
  params: {
    name: managedIdentityName
    location: location
    teamTag: teamNameTag
  }
}

module functionAppModule './resources/functionapp.bicep' = {
  name: '${moduleName}-functionapp'
  params: {
    name: functionAppName
    location: location
    serverFarmId: hostingPlanModule.outputs.planId
    userAssignedIdentityId: userAssignedIdentityModule.outputs.id
    teamTag: teamNameTag
  }
}

module functionAppSettingsModule './resources/functionappsettings.bicep' = {
  name: '${moduleName}-appsettings'
  params: {
    functionAppName: functionAppModule.outputs.name
    env: env
    storageAccountConnectionString: storageAccountModule.outputs.connectionString
    appInsightsConnectionString: appInsightsModule.outputs.connectionString
    serviceBusFullyQualifiedNamespace: '${serviceBusNamespaceName}.servicebus.windows.net'
    swapiBaseUrl: swapiBaseUrl
    userAssignedIdentityClientId: userAssignedIdentityModule.outputs.clientId
  }
}

resource contributorRoleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in contributorPrincipalIds: {
  name: guid(resourceGroup().id, principalId, contributorRoleDefinitionId)
  properties: {
    roleDefinitionId: contributorRoleDefinitionId
    principalId: principalId
    principalType: 'Group'
  }
}]

output functionAppHostname string = functionAppModule.outputs.defaultHostName
