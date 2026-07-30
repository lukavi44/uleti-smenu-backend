<#
.SYNOPSIS
  Provision LIVE Application Insights + email alerts for the production API.

  Creates (idempotent-ish):
  - Application Insights component (Log Analytics based)
  - Links App Service to Insights
  - Action group emailing support@uletismenu.com
  - Metric alert: Http5xx
  - Activity-style guidance for SMTP log alerts (printed; create in Portal if CLI unavailable)

  Does NOT print secret values.

.EXAMPLE
  .\configure-azure-monitoring.ps1
  .\configure-azure-monitoring.ps1 -AlertEmail "support@uletismenu.com"
#>
[CmdletBinding()]
param(
    [string] $ResourceGroup = "rg-uletismenu-staging",
    [string] $ApiAppName = "api-staging-uletismenu",
    [string] $InsightsName = "appi-uletismenu-live",
    [string] $ActionGroupName = "ag-uletismenu-live",
    [string] $AlertEmail = "support@uletismenu.com",
    [string] $Location = "westeurope"
)

$ErrorActionPreference = "Stop"

function Install-AzApplicationInsightsExtension {
    Write-Host "Installing Azure CLI application-insights extension if needed..." -ForegroundColor Cyan
    $previousErrorAction = $ErrorActionPreference
    try {
        # az writes preview notices to stderr; do not treat them as terminating errors in PowerShell.
        $ErrorActionPreference = "Continue"
        $output = az extension add --name application-insights --upgrade 2>&1
        foreach ($line in $output) {
            if ($line -match '^WARNING:') {
                Write-Host $line -ForegroundColor Yellow
            }
        }
        if ($LASTEXITCODE -ne 0) {
            throw "az extension add --name application-insights --upgrade failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
    }
}

if (-not (az account show 2>$null)) { throw "Run: az login" }

Install-AzApplicationInsightsExtension

Write-Host "Ensuring Application Insights '$InsightsName'..." -ForegroundColor Cyan
$existing = az monitor app-insights component show -a $InsightsName -g $ResourceGroup 2>$null
if (-not $existing) {
    az monitor app-insights component create `
        --app $InsightsName `
        --location $Location `
        --resource-group $ResourceGroup `
        --application-type web `
        --kind web `
        --output none
}

$conn = az monitor app-insights component show -a $InsightsName -g $ResourceGroup --query connectionString -o tsv
if (-not $conn) { throw "Failed to read Application Insights connection string." }

Write-Host "Linking App Service '$ApiAppName' to Application Insights (setting connection string)..." -ForegroundColor Cyan
az webapp config appsettings set `
    --resource-group $ResourceGroup `
    --name $ApiAppName `
    --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=$conn" `
    --output none | Out-Null

Write-Host "Ensuring action group '$ActionGroupName' → $AlertEmail ..." -ForegroundColor Cyan
az monitor action-group create `
    --resource-group $ResourceGroup `
    --name $ActionGroupName `
    --short-name usLive `
    --action email liveops $AlertEmail `
    --output none 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Action group may already exist; updating email action..." -ForegroundColor Yellow
    az monitor action-group update `
        --resource-group $ResourceGroup `
        --name $ActionGroupName `
        --add-action email liveops $AlertEmail `
        --output none 2>$null
}

$appId = az webapp show -g $ResourceGroup -n $ApiAppName --query id -o tsv
$agId = az monitor action-group show -g $ResourceGroup -n $ActionGroupName --query id -o tsv

Write-Host "Creating/updating Http5xx metric alert..." -ForegroundColor Cyan
az monitor metrics alert create `
    --name "uletismenu-live-http5xx" `
    --resource-group $ResourceGroup `
    --scopes $appId `
    --condition "total Http5xx > 5" `
    --window-size 5m `
    --evaluation-frequency 1m `
    --action $agId `
    --description "LIVE API Http5xx spike" `
    --severity Sev2 `
    --output none 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Http5xx alert may already exist (OK)." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "Confirm Azure Monitor welcome / test alert email at $AlertEmail" -ForegroundColor Yellow
Write-Host ""
Write-Host "Recommended Portal log alert (SMTP):" -ForegroundColor Cyan
Write-Host "  Application Insights → Alerts → New alert rule → Custom log search"
Write-Host "  Query contains: Failed to send email OR SMTP network connectivity failed"
Write-Host "  Threshold: >= 3 results in 30 minutes → action group $ActionGroupName"
Write-Host ""
Write-Host "Also configure UptimeRobot (or equivalent) on https://api.uletismenu.com/health" -ForegroundColor Cyan
Write-Host "See docs/PRODUCTION_SMOKE.md"
