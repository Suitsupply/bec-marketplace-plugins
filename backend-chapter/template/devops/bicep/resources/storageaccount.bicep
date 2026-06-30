@description('Storage Account name.')
param name string

@description('Resource location.')
param location string = resourceGroup().location

@description('Tag value for team ownership.')
param teamTag string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  tags: { team: teamTag }
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    defaultToOAuthAuthentication: true
    allowCrossTenantReplication: false
    accessTier: 'Hot'
    allowSharedKeyAccess: true
    publicNetworkAccess: 'Enabled'
  }
}

output id string = storageAccount.id
output name string = storageAccount.name
#disable-next-line outputs-should-not-contain-secrets
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value}'
