@description('Function App name.')
param name string

@description('Resource location.')
param location string

@description('App Service plan resource id.')
param serverFarmId string

@description('Resource ID of the user-assigned managed identity.')
param userAssignedIdentityId string

@description('Tag value for team ownership.')
param teamTag string

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: name
  location: location
  kind: 'functionapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  tags: {
    team: teamTag
  }
  properties: {
    httpsOnly: true
    keyVaultReferenceIdentity: userAssignedIdentityId
    serverFarmId: serverFarmId
    siteConfig: {
      minTlsVersion: '1.2'
    }
  }
}

resource stagingSlot 'Microsoft.Web/sites/slots@2024-04-01' = {
  name: 'staging'
  parent: functionApp
  location: location
  kind: 'functionapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  tags: {
    team: teamTag
  }
  properties: {
    httpsOnly: true
    keyVaultReferenceIdentity: userAssignedIdentityId
    serverFarmId: serverFarmId
    siteConfig: {
      minTlsVersion: '1.2'
    }
  }
}

output name string = functionApp.name
output defaultHostName string = functionApp.properties.defaultHostName
