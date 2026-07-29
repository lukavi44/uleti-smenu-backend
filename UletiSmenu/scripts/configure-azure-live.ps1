<#
.SYNOPSIS
  Apply required App Service settings for the LIVE API (Production environment).

.DESCRIPTION
  Targets the existing Azure App Service that serves api.uletismenu.com.
  Azure resource name may still be api-staging-uletismenu from initial provisioning.

.PARAMETER ResourceGroup
.PARAMETER ApiAppName
.PARAMETER ConnectionString
.PARAMETER BlobConnectionString
  Azure Storage connection string for durable uploads.
.PARAMETER SmtpPassword
  Zoho Mail app password for support@uletismenu.com (never commit this value).
.PARAMETER CorsOrigin
  Frontend origin (default https://app.uletismenu.com).

.EXAMPLE
  .\scripts\configure-azure-live.ps1 `
    -BlobConnectionString "DefaultEndpointsProtocol=https;..." `
    -SmtpPassword "your-zoho-app-password"
#>
[CmdletBinding()]
param(
    [string] $ResourceGroup = "rg-uletismenu-staging",
    [string] $ApiAppName = "api-staging-uletismenu",
    [string] $ConnectionString,
    [string] $BlobConnectionString,
    [string] $SmtpPassword,
    [string] $CorsOrigin = "https://app.uletismenu.com"
)

$ErrorActionPreference = "Stop"

if (-not (az account show 2>$null)) { throw "Run: az login" }

if (-not $ConnectionString) {
    $ConnectionString = az webapp config appsettings list `
        --resource-group $ResourceGroup `
        --name $ApiAppName `
        --query "[?name=='ConnectionStrings__UletiSmenu'].value | [0]" `
        -o tsv
}

if (-not $ConnectionString) {
    throw "No SQL connection string. Pass -ConnectionString or configure Azure first."
}

$settings = @{
    ASPNETCORE_ENVIRONMENT                     = "Production"
    Proxy__Provider                            = "None"
    "ConnectionStrings__UletiSmenu"             = $ConnectionString
    Cors__AllowedOrigins__0                    = $CorsOrigin
    FileSettings__Provider                     = "AzureBlob"
    FileSettings__BlobContainerName            = "uploads"
    Backend__BaseUrl                           = "https://api.uletismenu.com"
    Backend__FrontendBaseUrl                   = "https://app.uletismenu.com/"
    Stripe__Enabled                            = "false"
    AdminSeed__Enabled                         = "false"
    SmtpSettings__Host                         = "smtppro.zoho.eu"
    SmtpSettings__Port                         = "587"
    SmtpSettings__EnableSsl                    = "true"
    SmtpSettings__Username                     = "support@uletismenu.com"
    SmtpSettings__FromEmail                    = "noreply@uletismenu.com"
    SmtpSettings__FromName                     = "UletiSmenu"
    SmtpSettings__ReplyToEmail                 = "support@uletismenu.com"
    SmtpSettings__ContactInbox                 = "support@uletismenu.com"
}

if ($BlobConnectionString) {
    $settings["FileSettings__BlobConnectionString"] = $BlobConnectionString
}

if ($SmtpPassword) {
    $settings["SmtpSettings__Password"] = $SmtpPassword
}

$settingArgs = @()
foreach ($entry in $settings.GetEnumerator()) {
    $settingArgs += "$($entry.Key)=$($entry.Value)"
}

Write-Host "Configuring LIVE API $ApiAppName ..." -ForegroundColor Cyan
az webapp config appsettings set `
    --resource-group $ResourceGroup `
    --name $ApiAppName `
    --settings $settingArgs `
    --output none

az webapp config set `
    --resource-group $ResourceGroup `
    --name $ApiAppName `
    --https-only true `
    --output none

az webapp restart --resource-group $ResourceGroup --name $ApiAppName --output none

Write-Host "Done. Verify:" -ForegroundColor Green
Write-Host "  curl https://api.uletismenu.com/health"
Write-Host "  curl https://api.uletismenu.com/health/ready"
Write-Host ""
Write-Host "SMTP: set SmtpSettings__Password in Azure Portal if not passed via -SmtpPassword." -ForegroundColor Yellow
Write-Host "See docs/email-setup.md for Zoho app password steps." -ForegroundColor Yellow
Write-Host "Run scripts\verify-live-database.sql against the LIVE database." -ForegroundColor Yellow
