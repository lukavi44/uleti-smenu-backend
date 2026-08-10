<#
.SYNOPSIS
  LIVE Azure Blob / uploads smoke helpers (never prints secret values).

.DESCRIPTION
  1) Optionally checks that App Service settings for Blob exist (names only).
  2) Prints the manual smoke checklist from docs/PRODUCTION_SMOKE.md section F.

  Full end-to-end upload still requires a logged-in browser session on LIVE.

.EXAMPLE
  .\verify-live-blob-smoke.ps1
  .\verify-live-blob-smoke.ps1 -CheckAppSettings
  .\verify-live-blob-smoke.ps1 -CheckAppSettings -ResourceGroup rg-uletismenu-staging -ApiAppName api-staging-uletismenu
#>
[CmdletBinding()]
param(
    [switch] $CheckAppSettings,
    [string] $ResourceGroup = "rg-uletismenu-staging",
    [string] $ApiAppName = "api-staging-uletismenu",
    [string] $BaseUrl = "https://api.uletismenu.com"
)

$ErrorActionPreference = "Continue"
$failed = 0

function Write-Check([string] $Name, [bool] $Ok, [string] $Detail) {
    if ($Ok) {
        Write-Host "PASS  $Name - $Detail" -ForegroundColor Green
    } else {
        Write-Host "FAIL  $Name - $Detail" -ForegroundColor Red
        $script:failed++
    }
}

Write-Host "LIVE Blob smoke helpers ($BaseUrl)" -ForegroundColor Cyan
Write-Host "Secrets are never printed. Record pass/fail in docs/PRODUCTION_SMOKE.md section F." -ForegroundColor DarkGray
Write-Host ""

if ($CheckAppSettings) {
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Check "az CLI" $false "Install Azure CLI and run az login"
    } elseif (-not (az account show 2>$null)) {
        Write-Check "az login" $false "Run: az login"
    } else {
        $names = az webapp config appsettings list `
            --resource-group $ResourceGroup `
            --name $ApiAppName `
            --query "[].name" `
            -o tsv 2>$null

        if (-not $names) {
            Write-Check "App settings list" $false "Could not list settings for $ApiAppName"
        } else {
            $nameSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$names)
            Write-Check "FileSettings__Provider present" ($nameSet.Contains("FileSettings__Provider")) "name exists (value not shown)"
            Write-Check "FileSettings__BlobConnectionString present" ($nameSet.Contains("FileSettings__BlobConnectionString")) "name exists (value not shown)"
            Write-Check "FileSettings__BlobContainerName present" ($nameSet.Contains("FileSettings__BlobContainerName")) "name exists (value not shown)"

            # Confirm Provider value only (not the connection string)
            $provider = az webapp config appsettings list `
                --resource-group $ResourceGroup `
                --name $ApiAppName `
                --query "[?name=='FileSettings__Provider'].value | [0]" `
                -o tsv 2>$null
            Write-Check "FileSettings__Provider=AzureBlob" ($provider -eq "AzureBlob") "value=$provider"
        }
    }
    Write-Host ""
}

Write-Host "Manual checklist (browser on LIVE):" -ForegroundColor Cyan
Write-Host "  F1  Confirm FileSettings__Provider=AzureBlob + BlobConnectionString set (Portal or -CheckAppSettings)"
Write-Host "  F2  Log in → upload profile photo → note public URL (GET /uploads/... or Blob-backed URL)"
Write-Host "  F3  Restart App Service (api-staging-uletismenu) → reload profile → photo still loads"
Write-Host "  F4  Optional: replace photo; optional: delete account / clear photo → old blob removed (best-effort)"
Write-Host ""
Write-Host "Record outcome in PRODUCTION_SMOKE.md section F (date / pass-fail)." -ForegroundColor DarkGray

if ($CheckAppSettings -and $failed -gt 0) {
    Write-Host "$failed check(s) failed." -ForegroundColor Red
    exit 1
}

exit 0
