<#
.SYNOPSIS
  Provision LIVE Application Insights + email alerts for the production API.

  Creates (idempotent-ish):
  - Application Insights component (Log Analytics based) when Azure CLI extension works
  - Links App Service to Insights (when available)
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

function Invoke-AzQuiet {
    param([Parameter(Mandatory = $true)][scriptblock] $Command)
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        & $Command
        return $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Test-AzApplicationInsightsExtension {
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        az extension show --name application-insights --query name -o tsv 2>$null | Out-Null
        return ($LASTEXITCODE -eq 0)
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Install-AzApplicationInsightsExtension {
    if (Test-AzApplicationInsightsExtension) {
        Write-Host "Azure CLI application-insights extension already installed." -ForegroundColor Green
        return $true
    }

    Write-Host "Installing Azure CLI application-insights extension if needed..." -ForegroundColor Cyan
    $exitCode = Invoke-AzQuiet {
        az extension add --name application-insights --allow-preview true 2>&1 | ForEach-Object {
            if ($_ -match '^(WARNING|ERROR):') {
                Write-Host $_ -ForegroundColor Yellow
            }
        }
    }

    if (Test-AzApplicationInsightsExtension) {
        Write-Host "Application Insights extension installed." -ForegroundColor Green
        return $true
    }

    Write-Host "Could not install application-insights extension (exit $exitCode)." -ForegroundColor Yellow
    Write-Host "Continuing with Http5xx metric alert + action group only." -ForegroundColor Yellow
    Write-Host "You can create Application Insights in Portal: Monitoring → Application Insights." -ForegroundColor Yellow
    return $false
}

if (-not (az account show 2>$null)) { throw "Run: az login" }

$hasInsightsExt = Install-AzApplicationInsightsExtension

if ($hasInsightsExt) {
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
    if ($conn) {
        Write-Host "Linking App Service '$ApiAppName' to Application Insights (setting connection string)..." -ForegroundColor Cyan
        az webapp config appsettings set `
            --resource-group $ResourceGroup `
            --name $ApiAppName `
            --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=$conn" `
            --output none | Out-Null
    }
    else {
        Write-Host "Could not read Application Insights connection string; skipping App Service link." -ForegroundColor Yellow
    }
}

Write-Host "Ensuring action group '$ActionGroupName' → $AlertEmail ..." -ForegroundColor Cyan
$agExit = Invoke-AzQuiet {
    az monitor action-group create `
        --resource-group $ResourceGroup `
        --name $ActionGroupName `
        --short-name usLive `
        --action email liveops $AlertEmail `
        --output none 2>&1 | Out-Null
}
if ($agExit -ne 0) {
    Write-Host "Action group may already exist; updating email action..." -ForegroundColor Yellow
    Invoke-AzQuiet {
        az monitor action-group update `
            --resource-group $ResourceGroup `
            --name $ActionGroupName `
            --add-action email liveops $AlertEmail `
            --output none 2>&1 | Out-Null
    } | Out-Null
}

$appId = az webapp show -g $ResourceGroup -n $ApiAppName --query id -o tsv
$agId = az monitor action-group show -g $ResourceGroup -n $ActionGroupName --query id -o tsv
if (-not $appId) { throw "Could not resolve App Service id for $ApiAppName." }
if (-not $agId) { throw "Could not resolve action group id for $ActionGroupName." }

Write-Host "Creating/updating Http5xx metric alert..." -ForegroundColor Cyan
$alertExit = Invoke-AzQuiet {
    az monitor metrics alert create `
        --name "uletismenu-live-http5xx" `
        --resource-group $ResourceGroup `
        --scopes $appId `
        --condition "total Http5xx > 5" `
        --window-size 5m `
        --evaluation-frequency 1m `
        --action $agId `
        --description "LIVE API Http5xx spike" `
        --severity 2 `
        --output none 2>&1 | Out-Null
}

if ($alertExit -ne 0) {
    Write-Host "Http5xx alert may already exist; verifying..." -ForegroundColor Yellow
    $existingAlert = $null
    Invoke-AzQuiet {
        $script:existingAlert = az monitor metrics alert show `
            --name "uletismenu-live-http5xx" `
            --resource-group $ResourceGroup `
            --query name -o tsv 2>$null
    } | Out-Null
    if ($existingAlert) {
        Write-Host "Http5xx alert already present: $existingAlert" -ForegroundColor Green
    }
    else {
        throw "Failed to create Http5xx metric alert. Re-run with -Verbose or check: az monitor metrics alert create --help"
    }
}
else {
    Write-Host "Http5xx alert created." -ForegroundColor Green
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "Confirm Azure Monitor welcome / test alert email at $AlertEmail" -ForegroundColor Yellow
Write-Host "Portal: $ApiAppName → Monitoring → Alerts → Alert rules (look for uletismenu-live-http5xx)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Recommended Portal log alert (SMTP):" -ForegroundColor Cyan
Write-Host "  Application Insights → Alerts → New alert rule → Custom log search"
Write-Host "  Query contains: Failed to send email OR SMTP network connectivity failed"
Write-Host "  Threshold: >= 3 results in 30 minutes → action group $ActionGroupName"
Write-Host ""
Write-Host "Also configure UptimeRobot (or equivalent) on https://api.uletismenu.com/health" -ForegroundColor Cyan
Write-Host "See docs/PRODUCTION_SMOKE.md"
