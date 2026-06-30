@description('User-assigned managed identity name.')
param name string

@description('Resource location.')
param location string

@description('Tag value for team ownership.')
param teamTag string

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
  tags: {
    team: teamTag
  }
}

output id string = userAssignedIdentity.id
output name string = userAssignedIdentity.name
output principalId string = userAssignedIdentity.properties.principalId
output clientId string = userAssignedIdentity.properties.clientId
