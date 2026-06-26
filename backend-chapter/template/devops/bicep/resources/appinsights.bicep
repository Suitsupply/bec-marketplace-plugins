@description('Application Insights name.')
param name string

@description('Resource location.')
param location string

@description('Log Analytics Workspace resource ID.')
param logAnalyticsWorkspaceId string

@description('Tag value for team ownership.')
param teamTag string

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  kind: 'web'
  tags: {
    team: teamTag
  }
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
  }
}

output connectionString string = appInsights.properties.ConnectionString
