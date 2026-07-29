<#
.SYNOPSIS
  DEPRECATED — use configure-azure-live.ps1 for LIVE / Production App Service settings.

  Azure resource names may still contain "staging" (legacy); they host LIVE today.
  Prefer: .\scripts\configure-azure-live.ps1

.PARAMETER ResourceGroup
.PARAMETER ApiAppName
.PARAMETER ConnectionString
  Azure SQL connection string. If omitted, copies from ConnectionStrings__DefaultConnection when present.

.EXAMPLE
  .\scripts\configure-azure-live.ps1 -SmtpPassword "..."
#>
[CmdletBinding()]
param(
    [string] $ResourceGroup = "rg-uletismenu-staging",
    [string] $ApiAppName = "api-staging-uletismenu",
    [string] $ConnectionString
)

$ErrorActionPreference = "Stop"

Write-Host "DEPRECATED: prefer configure-azure-live.ps1 for LIVE configuration." -ForegroundColor Yellow

if (-not (az account show 2>$null)) { throw "Run: az login" }

if (-not $ConnectionString) {
    $ConnectionString = az webapp config appsettings list `
        --resource-group $ResourceGroup `
        --name $ApiAppName `
        --query "[?name=='ConnectionStrings__UletiSmenu' || name=='ConnectionStrings__DefaultConnection'].value | [0]" `
        -o tsv
}

if (-not $ConnectionString) {
    throw "No connection string. Pass -ConnectionString or set ConnectionStrings__DefaultConnection in Azure."
}

Write-Host "Configuring $ApiAppName ..." -ForegroundColor Cyan
az webapp config appsettings set `
    --resource-group $ResourceGroup `
    --name $ApiAppName `
    --settings `
        ASPNETCORE_ENVIRONMENT=Staging `
        "ConnectionStrings__UletiSmenu=$ConnectionString" `
        Cors__AllowedOrigins__0=https://staging.uletismenu.com `
        Cors__AllowedOrigins__1=http://localhost:5173 `
        FileSettings__UploadPath=/home/uploads `
        Stripe__Enabled=false `
    --output none

$legacy = az webapp config appsettings list `
    --resource-group $ResourceGroup `
    --name $ApiAppName `
    --query "[?name=='ConnectionStrings__DefaultConnection'].name" -o tsv
if ($legacy) {
    az webapp config appsettings delete `
        --resource-group $ResourceGroup `
        --name $ApiAppName `
        --setting-names ConnectionStrings__DefaultConnection `
        --output none
}

az webapp start --resource-group $ResourceGroup --name $ApiAppName --output none
Write-Host "Done. Start deploy with .\scripts\deploy-azure-staging.ps1" -ForegroundColor Green
