<#
.SYNOPSIS
  Publish API zip and deploy to Azure App Service staging.

.PARAMETER ResourceGroup
  Azure resource group containing the API web app.

.PARAMETER ApiAppName
  App Service name (default: api-staging-uletismenu).

.PARAMETER SkipPublish
  Skip dotnet publish; deploy existing publish\api.zip.

.EXAMPLE
  .\scripts\deploy-azure-staging.ps1

.EXAMPLE
  .\scripts\deploy-azure-staging.ps1 -SkipPublish
#>
[CmdletBinding()]
param(
    [string] $ResourceGroup = "rg-uletismenu-staging",
    [string] $ApiAppName = "api-staging-uletismenu",
    [switch] $SkipPublish
)

$ErrorActionPreference = "Stop"

$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) { throw "Run: az login" }

$Root = Split-Path $PSScriptRoot -Parent
$ZipPath = Join-Path $Root "publish\api.zip"

if (-not $SkipPublish) {
    & (Join-Path $PSScriptRoot "publish-api.ps1")
}

if (-not (Test-Path $ZipPath)) {
    throw "Missing $ZipPath - run publish-api.ps1 first."
}

$appState = az webapp show --resource-group $ResourceGroup --name $ApiAppName --query state -o tsv
if ($appState -eq "Stopped") {
    Write-Host "Web app is stopped - starting before deploy ..." -ForegroundColor Yellow
    az webapp start --resource-group $ResourceGroup --name $ApiAppName --output none
    Start-Sleep -Seconds 15
}

Write-Host "Deploying to $ApiAppName ..." -ForegroundColor Cyan
az webapp deploy `
    --resource-group $ResourceGroup `
    --name $ApiAppName `
    --src-path $ZipPath `
    --type zip `
    --async false

if ($LASTEXITCODE -ne 0) {
    throw "Azure deploy failed (exit $LASTEXITCODE). Check deployment logs in the Azure portal for $ApiAppName."
}

$hostName = az webapp show --resource-group $ResourceGroup --name $ApiAppName --query defaultHostName -o tsv
Write-Host "Deployed. API URL: https://$hostName" -ForegroundColor Green
Write-Host "Swagger: https://$hostName/swagger" -ForegroundColor DarkGray
