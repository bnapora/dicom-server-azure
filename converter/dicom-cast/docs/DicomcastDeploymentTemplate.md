```json
{
    "$schema": "http://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
        "serviceName": {
            "minLength": 3,
            "maxLength": 24,
            "type": "String",
            "metadata": {
                "description": "Name of the DICOM Cast service container group."
            }
        },
        "image": {
            "defaultValue": "dicomoss.azurecr.io/linux_dicom-cast",
            "type": "String",
            "metadata": {
                "description": "Container image to deploy. Should be of the form repoName/imagename:tag for images stored in public Docker Hub, or a fully qualified URI for other registries. Images from private registries require additional registry credentials."
            }
        },
        "storageAccountSku": {
            "defaultValue": "Standard_LRS",
            "allowedValues": [
                "Standard_LRS",
                "Standard_GRS",
                "Standard_RAGRS",
                "Standard_ZRS",
                "Premium_LRS",
                "Premium_ZRS",
                "Standard_GZRS",
                "Standard_RAGZRS"
            ],
            "type": "String"
        },
        "deployApplicationInsights": {
            "defaultValue": true,
            "type": "Bool",
            "metadata": {
                "description": "Deploy Application Insights for the DICOM server. Disabled for Microsoft Azure Government (MAG)"
            }
        },
        "applicationInsightsLocation": {
            "defaultValue": "[resourceGroup().location]",
            "allowedValues": [
                "southeastasia",
                "northeurope",
                "westeurope",
                "eastus",
                "southcentralus",
                "westus2"
            ],
            "type": "String"
        },
        "cpuCores": {
            "defaultValue": "1.0",
            "type": "String",
            "metadata": {
                "description": "The number of CPU cores to allocate to the container."
            }
        },
        "memoryInGb": {
            "defaultValue": "1.5",
            "type": "String",
            "metadata": {
                "description": "The amount of memory to allocate to the container in gigabytes."
            }
        },
        "location": {
            "defaultValue": "[resourceGroup().location]",
            "type": "String",
            "metadata": {
                "description": "Location for all resources."
            }
        },
        "restartPolicy": {
            "defaultValue": "always",
            "allowedValues": [
                "never",
                "always",
                "onfailure"
            ],
            "type": "String",
            "metadata": {
                "description": "The behavior of Azure runtime if container has stopped."
            }
        },
        "dicomWebEndpoint": {
            "type": "String",
            "metadata": {
                "description": "The endpoint of the DICOM Web server."
            }
        },
        "fhirEndpoint": {
            "type": "String",
            "metadata": {
                "description": "The endpoint of the FHIR server."
            }
        },
        "patientSystemId": {
            "type": "String",
            "metadata": {
                "description": "Patient SystemId configured by the user"
            }
        },
        "isIssuerIdUsed": {
            "defaultValue": false,
            "type": "Bool",
            "metadata": {
                "description": "Issuer id or patient system id used based on this boolean value"
            }
        },
        "enforceValidationOfTagValues": {
            "defaultValue": false,
            "type": "Bool",
            "metadata": {
                "description": "Enforce validation of all tag values and do not store to FHIR even if only non-required tags are invalid"
            }
        },
        "ignoreJsonParsingErrors": {
            "defaultValue": false,
            "type": "Bool",
            "metadata": {
                "description": "Ignore json parsing errors for DICOM instances with malformed DICOM json"
            }
        },
        "additionalEnvironmentVariables": {
            "defaultValue": [],
            "type": "Array",
            "metadata": {
                "description": "Array of additional enviornment variables with objects with properties 'name' and 'value'. ex: [{\"name\": \"testName\", \"value\": \"testValue\"}]"
            }
        },
        "virtualNetworkName": {
            "type": "String",
            "metadata": {
                "description": "Virtual network where ACi will be deployed to"
            }
        },
        "subnetName": {
            "type": "String",
            "metadata": {
                "description": "Subnet within a virtual network which is delegated to ACI"
            }
        }
    },
    "variables": {
        "networkProfileName": "aci-networkProfile",
        "interfaceConfigName": "eth0",
        "interfaceIpConfig": "ipconfigprofile1",
        "isMAG": "[or(contains(resourceGroup().location,'usgov'),contains(resourceGroup().location,'usdod'))]",
        "serviceName": "[toLower(parameters('serviceName'))]",
        "virtualNetworkName": "[parameters('virtualNetworkName')]",
        "subnetName": "[parameters('subnetName')]",
        "keyvaultName": "[concat(substring(replace(variables('serviceName'), '-', ''), 0, min(11, length(variables('serviceName')))), uniquestring(resourceGroup().id))]",
        "containerGroupResourceId": "[resourceId('Microsoft.ContainerInstance/containerGroups/', variables('serviceName'))]",
        "deployAppInsights": "[and(parameters('deployApplicationInsights'),not(variables('isMAG')))]",
        "appInsightsName": "[concat('AppInsights-', variables('serviceName'))]",
        "storageAccountName": "[concat(substring(replace(variables('serviceName'), '-', ''), 0, min(11, length(variables('serviceName')))), uniquestring(resourceGroup().id))]",
        "storageResourceId": "[resourceId('Microsoft.Storage/storageAccounts', variables('storageAccountName'))]",
        "keyVaultEndpoint": "[if(variables('isMAG'), concat('https://', variables('keyvaultName'), '.vault.usgovcloudapi.net/'), concat('https://', variables('keyvaultName'), '.vault.azure.net/'))]",
        "keyVaultResourceId": "[resourceId('Microsoft.KeyVault/vaults', variables('keyvaultName'))]",
        "networkProfileResourceId": "[resourceId('Microsoft.Network/virtualNetworks/subnets', variables('virtualNetworkName'), variables('subnetName'))]",
        "environmentVariables": [
            {
                "name": "Fhir__Endpoint",
                "value": "[parameters('fhirEndpoint')]"
            },
            {
                "name": "DicomWeb__Endpoint",
                "value": "[parameters('dicomWebEndpoint')]"
            },
            {
                "name": "KeyVault__Endpoint",
                "value": "[variables('keyVaultEndpoint')]"
            },
            {
                "name": "DicomCast__Features__EnforceValidationOfTagValues",
                "value": "[parameters('enforceValidationOfTagValues')]"
            },
            {
                "name": "DicomCast__Features__IgnoreJsonParsingErrors",
                "value": "[parameters('ignoreJsonParsingErrors')]"
            },
            {
                "name": "Patient__PatientSystemId",
                "value": "[parameters('patientSystemId')]"
            },
            {
                "name": "Patient__IsIssuerIdUsed",
                "value": "[parameters('isIssuerIdUsed')]"
            }
        ]
    },
    "resources": [
        {
            "type": "Microsoft.Network/networkProfiles",
            "apiVersion": "2020-11-01",
            "name": "[variables('serviceName')]",
            "location": "[parameters('location')]",
            "properties": {
                "containerNetworkInterfaceConfigurations": [
                    {
                        "name": "[variables('interfaceConfigName')]",
                        "properties": {
                            "ipConfigurations": [
                                {
                                    "name": "[variables('interfaceIpConfig')]",
                                    "properties": {
                                        "subnet": {
                                            "id": "[variables('networkProfileResourceId')]"
                                        }
                                    }
                                }
                            ]
                        }
                    }
                ]
            }
        },
        {
            "type": "Microsoft.ContainerInstance/containerGroups",
            "apiVersion": "2018-10-01",
            "name": "[variables('serviceName')]",
            "location": "[parameters('location')]",
            "dependsOn": [
                "[concat('Microsoft.Insights/components/', variables('appInsightsName'))]"
            ],
            "identity": {
                "type": "SystemAssigned"
            },
            "properties": {
                "containers": [
                    {
                        "name": "[variables('serviceName')]",
                        "properties": {
                            "image": "[parameters('image')]",
                            "resources": {
                                "requests": {
                                    "cpu": "[parameters('cpuCores')]",
                                    "memoryInGb": "[parameters('memoryInGb')]"
                                }
                            },
                            "environmentVariables": "[concat(variables('environmentVariables'), parameters('additionalEnvironmentVariables'), array(createObject('name', 'ApplicationInsights__InstrumentationKey', 'value', if(variables('deployAppInsights'), reference(concat('Microsoft.Insights/components/', variables('appInsightsName'))).InstrumentationKey, ''))))]"
                        }
                    }
                ],
                "osType": "Linux",
                "networkProfile": {
                    "id": "[resourceId('Microsoft.Network/networkProfiles', variables('serviceName'))]"
                },
                "restartPolicy": "[parameters('restartPolicy')]"
            }
        },
        {
            "type": "Microsoft.Insights/components",
            "apiVersion": "2015-05-01",
            "name": "[variables('appInsightsName')]",
            "location": "[parameters('applicationInsightsLocation')]",
            "tags": {
                "[concat('hidden-link:', variables('containerGroupResourceId'))]": "Resource",
                "displayName": "AppInsightsComponent"
            },
            "kind": "web",
            "properties": {
                "Application_Type": "web",
                "ApplicationId": "[variables('serviceName')]"
            },
            "condition": "[variables('deployAppInsights')]"
        },
        {
            "type": "Microsoft.Storage/storageAccounts",
            "apiVersion": "2019-04-01",
            "name": "[variables('storageAccountName')]",
            "location": "[resourceGroup().location]",
            "tags": {},
            "sku": {
                "name": "[parameters('storageAccountSku')]"
            },
            "kind": "StorageV2",
            "properties": {
                "accessTier": "Hot",
                "supportsHttpsTrafficOnly": "true"
            }
        },
        {
            "type": "Microsoft.KeyVault/vaults",
            "apiVersion": "2015-06-01",
            "name": "[variables('keyvaultName')]",
            "location": "[resourceGroup().location]",
            "dependsOn": [
                "[variables('containerGroupResourceId')]"
            ],
            "tags": {},
            "properties": {
                "sku": {
                    "family": "A",
                    "name": "Standard"
                },
                "tenantId": "[reference(variables('containerGroupResourceId'), '2018-10-01', 'Full').Identity.tenantId]",
                "accessPolicies": [
                    {
                        "tenantId": "[reference(variables('containerGroupResourceId'), '2018-10-01', 'Full').Identity.tenantId]",
                        "objectId": "[reference(variables('containerGroupResourceId'), '2018-10-01', 'Full').Identity.principalId]",
                        "permissions": {
                            "secrets": [
                                "get",
                                "list",
                                "set"
                            ]
                        }
                    }
                ],
                "enabledForDeployment": false
            }
        },
        {
            "type": "Microsoft.KeyVault/vaults/secrets",
            "apiVersion": "2015-06-01",
            "name": "[concat(variables('keyvaultName'), '/TableStore--ConnectionString')]",
            "dependsOn": [
                "[variables('keyVaultResourceId')]",
                "[variables('storageResourceId')]"
            ],
            "properties": {
                "contentType": "text/plain",
                "value": "[concat('DefaultEndpointsProtocol=https;AccountName=', variables('storageAccountName'), ';AccountKey=', listKeys(variables('storageResourceId'), providers('Microsoft.Storage', 'storageAccounts').apiVersions[0]).keys[0].value, ';')]"
            }
        }
    ],
    "outputs": {
        "containerTenantId": {
            "type": "String",
            "value": "[reference(variables('containerGroupResourceId'), '2018-10-01', 'Full').Identity.tenantId]"
        },
        "containerPrincipalId": {
            "type": "String",
            "value": "[reference(variables('containerGroupResourceId'), '2018-10-01', 'Full').Identity.principalId]"
        }
    }
}
