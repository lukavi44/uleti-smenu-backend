<#
.SYNOPSIS
  LIVE Production SQL review helpers (Azure posture via CLI; schema via Azure Data Studio).

.DESCRIPTION
  Legacy Azure names map to LIVE (see docs/ROADMAP.md):
    rg-uletismenu-staging, uletismenu-staging-sql, UletiSmenuDb_Staging

  Automated: tier, auto-pause, backup retention, firewall rules, connection string name on App Service.
  Manual: run verify-live-database.sql in Azure Data Studio when DB is awake.

.EXAMPLE
  .\verify-live-sql-review.ps1
  .\verify-live-sql-review.ps1 -ResourceGroup rg-uletismenu-staging -Server uletismenu-staging-sql -Database UletiSmenuDb_Staging
#>
[CmdletBinding()]
param(
    [string] $ResourceGroup = "rg-uletismenu-staging",
    [string] $Server = "uletismenu-staging-sql",
    [string] $Database = "UletiSmenuDb_Staging",
    [string] $ApiAppName = "api-staging-uletismenu",
    [string] $ApiBaseUrl = "https://api.uletismenu.com"
)

$ErrorActionPreference = "Continue"
$failed = 0
$warn = 0

function Write-Check([string] $Name, [string] $Level, [string] $Detail) {
    switch ($Level) {
        "PASS" { Write-Host "PASS  $Name - $Detail" -ForegroundColor Green }
        "WARN" { Write-Host "WARN  $Name - $Detail" -ForegroundColor Yellow; $script:warn++ }
        "FAIL" { Write-Host "FAIL  $Name - $Detail" -ForegroundColor Red; $script:failed++ }
        default { Write-Host "INFO  $Name - $Detail" -ForegroundColor Cyan }
    }
}

Write-Host "LIVE SQL review ($Database on $Server) — legacy RG name = LIVE" -ForegroundColor Cyan
Write-Host ""

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Check "az CLI" "FAIL" "Install Azure CLI and run az login"
    exit 1
}
if (-not (az account show 2>$null)) {
    Write-Check "az login" "FAIL" "Run: az login"
    exit 1
}

try {
    $ready = Invoke-WebRequest -Uri "$ApiBaseUrl/health/ready" -UseBasicParsing -TimeoutSec 90
    $healthy = $ready.StatusCode -eq 200 -and $ready.Content -match "Healthy"
    Write-Check "API /health/ready" $(if ($healthy) { "PASS" } else { "WARN" }) "HTTP $($ready.StatusCode) $($ready.Content)"
} catch {
    $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    Write-Check "API /health/ready" "WARN" "HTTP $code — DB may be paused; wake with a real API call, wait, re-run"
}

$db = az sql db show --resource-group $ResourceGroup --server $Server --name $Database -o json 2>$null | ConvertFrom-Json
if (-not $db) {
    Write-Check "Database" "FAIL" "Could not read $Database"
} else {
    $sku = "$($db.currentSku.tier) $($db.currentSku.name) (capacity $($db.currentSku.capacity))"
    Write-Check "S1 Tier" "INFO" "$sku; status=$($db.status); maxSize=$([math]::Round($db.maxSizeBytes / 1GB, 1)) GB"
    if ($null -ne $db.autoPauseDelay -and $db.autoPauseDelay -gt 0) {
        Write-Check "S2 Auto-pause" "WARN" "autoPauseDelay=$($db.autoPauseDelay) min; minCapacity=$($db.minCapacity) — cold start 30–60s after idle; OK for pilot, revisit before marketing"
    } else {
        Write-Check "S2 Auto-pause" "PASS" "Auto-pause disabled or not serverless"
    }
}

$str = az sql db str-policy show --resource-group $ResourceGroup --server $Server --name $Database -o json 2>$null | ConvertFrom-Json
if ($str) {
    Write-Check "S3 Backup (STR)" "INFO" "retentionDays=$($str.retentionDays); diffBackupIntervalInHours=$($str.diffBackupIntervalInHours) — PITR within STR window"
} else {
    Write-Check "S3 Backup (STR)" "WARN" "Could not read short-term retention policy"
}

$rules = az sql server firewall-rule list --resource-group $ResourceGroup --server $Server -o json 2>$null | ConvertFrom-Json
if ($rules) {
    $wideOpen = $rules | Where-Object { $_.startIpAddress -eq "0.0.0.0" -and $_.endIpAddress -eq "255.255.255.255" }
    if ($wideOpen) {
        Write-Check "S4 Firewall" "WARN" "Rule '$($wideOpen.name)' allows 0.0.0.0–255.255.255.255 — restrict to admin IPs before marketing"
    } else {
        Write-Check "S4 Firewall" "PASS" "No internet-wide rule detected"
    }
    foreach ($r in $rules) {
        Write-Host "      rule $($r.name): $($r.startIpAddress) – $($r.endIpAddress)" -ForegroundColor DarkGray
    }
}

$connName = az webapp config appsettings list --resource-group $ResourceGroup --name $ApiAppName --query "[?name=='ConnectionStrings__UletiSmenu'].name | [0]" -o tsv 2>$null
Write-Check "S5 Connection string" $(if ($connName) { "PASS" } else { "FAIL" }) $(if ($connName) { "App setting name present (value not shown)" } else { "ConnectionStrings__UletiSmenu missing on $ApiAppName" })

Write-Host ""
Write-Host "S6 Schema checks (manual):" -ForegroundColor Cyan
Write-Host "  1. Azure Data Studio → connect to $Database on $Server"
Write-Host "  2. Run: UletiSmenu\scripts\verify-live-database.sql"
Write-Host "  3. Expect: OK on Phase 3 index; DeletedAtUtc WARN until account-deletion ships to LIVE"
Write-Host ""
Write-Host "Record outcome in docs/PRODUCTION_HARDENING.md section 4 SQL review template." -ForegroundColor DarkGray

if ($failed -gt 0) { exit 1 }
exit 0
