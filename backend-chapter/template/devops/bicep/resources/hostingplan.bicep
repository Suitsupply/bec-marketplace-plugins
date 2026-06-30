@description('App Service plan name.')
param name string

@description('Resource location.')
param location string

@description('Plan SKU name. Defaults to Y1 (Consumption).')
param skuName string = 'Y1'

@description('Plan SKU tier. Defaults to Dynamic (Consumption).')
param skuTier string = 'Dynamic'

@description('Tag value for team ownership.')
param teamTag string

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: name
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
  kind: 'functionapp'
  tags: { team: teamTag }
  properties: {
    reserved: false
  }
}

output planId string = appServicePlan.id
