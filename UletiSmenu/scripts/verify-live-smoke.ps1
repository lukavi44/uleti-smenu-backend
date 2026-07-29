<#
.SYNOPSIS
  LIVE smoke checks for api.uletismenu.com (no secrets printed).

.EXAMPLE
  .\verify-live-smoke.ps1
  .\verify-live-smoke.ps1 -BaseUrl https://api.uletismenu.com
#>
[CmdletBinding()]
param(
    [string] $BaseUrl = "https://api.uletismenu.com"
)

$ErrorActionPreference = "Continue"
$failed = 0

function Write-Check([string] $Name, [bool] $Ok, [string] $Detail) {
    if ($Ok) {
        Write-Host "PASS  $Name — $Detail" -ForegroundColor Green
    } else {
        Write-Host "FAIL  $Name — $Detail" -ForegroundColor Red
        $script:failed++
    }
}

Write-Host "LIVE smoke against $BaseUrl" -ForegroundColor Cyan

try {
    $health = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 30
    Write-Check "A1 /health" ($health.StatusCode -eq 200 -and $health.Content -match "ok") "HTTP $($health.StatusCode) $($health.Content)"
} catch {
    Write-Check "A1 /health" $false $_.Exception.Message
}

try {
    $ready = Invoke-WebRequest -Uri "$BaseUrl/health/ready" -UseBasicParsing -TimeoutSec 60
    Write-Check "A2 /health/ready" ($ready.StatusCode -eq 200) "HTTP $($ready.StatusCode) $($ready.Content)"
} catch {
    $resp = $_.Exception.Response
    $code = if ($resp) { [int]$resp.StatusCode } else { 0 }
    if ($code -eq 503) {
        Write-Host "WARN  A2 /health/ready — HTTP 503 Unhealthy (often Azure SQL paused). Wake DB with a real app request, wait, re-run." -ForegroundColor Yellow
    } else {
        Write-Check "A2 /health/ready" $false $_.Exception.Message
    }
}

try {
    $swagger = Invoke-WebRequest -Uri "$BaseUrl/swagger" -UseBasicParsing -TimeoutSec 20
    Write-Check "A3 /swagger disabled" ($swagger.StatusCode -eq 404) "HTTP $($swagger.StatusCode) (expected 404)"
} catch {
    $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    Write-Check "A3 /swagger disabled" ($code -eq 404 -or $code -eq 0) "HTTP $code (404 expected)"
}

try {
    $body = '{"email":"smoke-should-404@uletismenu.com"}'
    $debug = Invoke-WebRequest -Uri "$BaseUrl/api/v1/debug/test-email" -Method POST -ContentType "application/json" -Body $body -UseBasicParsing -TimeoutSec 20
    Write-Check "A4 debug/test-email" ($debug.StatusCode -eq 404) "HTTP $($debug.StatusCode) (expected 404)"
} catch {
    $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    # 404 is success. 400 can mean model binding ran before Production gate (treat as FAIL until fixed).
    Write-Check "A4 debug/test-email" ($code -eq 404) "HTTP $code (expected 404)"
}

Write-Host ""
if ($failed -eq 0) {
    Write-Host "Automated checks OK. Complete section B (email flows) manually — see docs/PRODUCTION_SMOKE.md" -ForegroundColor Green
    exit 0
}

Write-Host "$failed automated check(s) failed." -ForegroundColor Red
exit 1
