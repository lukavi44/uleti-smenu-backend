<#
.SYNOPSIS
  Creates Azure resources for UletiSmenu staging (one-time setup).

.DESCRIPTION
  - Resource group
  - Azure SQL Server + database (UletiSmenuDb_Staging)
  - Linux App Service (.NET 8) for API
  - Static Web App for React frontend

  Run `az login` first. Custom domains (staging.uletismenu.com, api-staging.uletismenu.com)
  are configured separately - see docs/STAGING_DEPLOY.md.

.PARAMETER ResourceGroup
  Azure resource group name.

.PARAMETER Location
  Azure region (default: westeurope).

.PARAMETER NamePrefix
  Globally unique prefix for SQL server and Static Web App (letters/numbers/hyphens).

.PARAMETER ApiAppName
  App Service name for the API (default: api-staging-uletismenu).

.PARAMETER SqlAdminUser
  SQL admin login name.

.PARAMETER SqlAdminPassword
  SQL admin password (min 8 chars, complexity required by Azure). Prompted if omitted.

.EXAMPLE
  .\scripts\provision-azure-staging.ps1 -NamePrefix uletismenu-stg
#>
[CmdletBinding()]
param(
    [string] $ResourceGroup = "rg-uletismenu-staging",
    [string] $Location = "westeurope",
    [Parameter(Mandatory = $true)]
    [string] $NamePrefix,
    [string] $ApiAppName = "api-staging-uletismenu",
    [string] $SqlAdminUser = "uletismenu_admin",
    [SecureString] $SqlAdminPassword
)

$ErrorActionPreference = "Stop"

function Require-AzLogin {
    $account = az account show 2>$null | ConvertFrom-Json
    if (-not $account) {
        throw "Not logged in. Run: az login"
    }
    Write-Host "Subscription: $($account.name) ($($account.id))" -ForegroundColor DarkGray
}

function New-RandomPassword {
    param([int] $Length = 24)
    $chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#%*"
    -join ((1..$Length) | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
}

Require-AzLogin

if (-not $SqlAdminPassword) {
    $plain = New-RandomPassword
    Write-Host "Generated SQL admin password (save this - shown once):" -ForegroundColor Yellow
    Write-Host $plain
    $SqlAdminPassword = ConvertTo-SecureString $plain -AsPlainText -Force
}

$sqlPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword))

# Azure resource names (must be globally unique where noted)
$sqlServer = "$NamePrefix-sql"           # globally unique
$apiAppName = $ApiAppName                # globally unique
$swaName = "$NamePrefix-web"             # globally unique
$planName = "$NamePrefix-plan"
$dbName = "UletiSmenuDb_Staging"
$skuSql = "Basic"                        # staging; bump to S0 for heavier load

Write-Host "`n=== Creating resource group ===" -ForegroundColor Cyan
az group create --name $ResourceGroup --location $Location --output none

Write-Host "=== Creating Azure SQL server ===" -ForegroundColor Cyan
az sql server create `
    --resource-group $ResourceGroup `
    --name $sqlServer `
    --location $Location `
    --admin-user $SqlAdminUser `
    --admin-password $sqlPasswordPlain `
    --output none

Write-Host "=== Allow Azure services to reach SQL ===" -ForegroundColor Cyan
az sql server firewall-rule create `
    --resource-group $ResourceGroup `
    --server $sqlServer `
    --name AllowAzureServices `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0 `
    --output none

Write-Host "=== Creating database $dbName ===" -ForegroundColor Cyan
az sql db create `
    --resource-group $ResourceGroup `
    --server $sqlServer `
    --name $dbName `
    --edition $skuSql `
    --capacity 5 `
    --output none

Write-Host "=== Creating App Service plan (Linux B1) ===" -ForegroundColor Cyan
az appservice plan create `
    --resource-group $ResourceGroup `
    --name $planName `
    --location $Location `
    --sku B1 `
    --is-linux `
    --output none

Write-Host "=== Creating API web app (.NET 8) ===" -ForegroundColor Cyan
az webapp create `
    --resource-group $ResourceGroup `
    --plan $planName `
    --name $apiAppName `
    --runtime "DOTNETCORE:8.0" `
    --output none

$connString = "Server=tcp:$sqlServer.database.windows.net,1433;Initial Catalog=$dbName;Persist Security Info=False;User ID=$SqlAdminUser;Password=$sqlPasswordPlain;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "=== Configuring API app settings ===" -ForegroundColor Cyan
az webapp config appsettings set `
    --resource-group $ResourceGroup `
    --name $apiAppName `
    --settings `
        ASPNETCORE_ENVIRONMENT=Staging `
        "ConnectionStrings__UletiSmenu=$connString" `
        Cors__AllowedOrigins__0=https://staging.uletismenu.com `
        Cors__AllowedOrigins__1=http://localhost:5173 `
        FileSettings__UploadPath=/home/uploads `
        Stripe__Enabled=false `
    --output none

az webapp config set `
    --resource-group $ResourceGroup `
    --name $apiAppName `
    --https-only true `
    --output none

Write-Host "=== Creating Static Web App (frontend) ===" -ForegroundColor Cyan
az staticwebapp create `
    --resource-group $ResourceGroup `
    --name $swaName `
    --location $Location `
    --sku Free `
    --output none

$apiDefaultHost = az webapp show --resource-group $ResourceGroup --name $apiAppName --query defaultHostName -o tsv
$swaDefaultHost = az staticwebapp show --resource-group $ResourceGroup --name $swaName --query defaultHostname -o tsv

$outPath = Join-Path $PSScriptRoot "azure-staging-output.json"
@{
    resourceGroup   = $ResourceGroup
    location        = $Location
    sqlServer       = "$sqlServer.database.windows.net"
    database        = $dbName
    sqlAdminUser    = $SqlAdminUser
    apiAppName      = $apiAppName
    apiDefaultUrl   = "https://$apiDefaultHost"
    swaName         = $swaName
    swaDefaultUrl   = "https://$swaDefaultHost"
    customApiHost   = "api-staging.uletismenu.com"
    customWebHost   = "staging.uletismenu.com"
} | ConvertTo-Json | Set-Content -Path $outPath -Encoding UTF8

Write-Host "`nDone. Resource summary saved to:" -ForegroundColor Green
Write-Host "  $outPath"
Write-Host ""
Write-Host "Default URLs (before custom DNS):" -ForegroundColor Yellow
Write-Host "  API:      https://$apiDefaultHost"
Write-Host "  Frontend: https://$swaDefaultHost"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. DNS: CNAME api-staging -> $apiDefaultHost"
Write-Host "  2. DNS: CNAME staging -> $swaDefaultHost (or SWA custom domain wizard)"
Write-Host "  3. .\scripts\deploy-azure-staging.ps1"
Write-Host "  4. Build frontend with VITE_API_BASE_URL=https://api-staging.uletismenu.com and deploy to SWA"
Write-Host ""
Write-Host "SQL password was set during this run. Store it in a password manager." -ForegroundColor Yellow
