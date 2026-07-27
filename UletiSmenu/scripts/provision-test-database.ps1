<#
.SYNOPSIS
  Create UletiSmenuDb_Test on an existing Azure SQL server (TEST environment database).

.DESCRIPTION
  Reuses the same SQL server as LIVE to avoid extra server cost.
  TEST API runs on Render free tier (render-test.yaml) and points here.

.PARAMETER ResourceGroup
.PARAMETER SqlServerName
  Server name without .database.windows.net (e.g. uletismenu-staging-sql).
.PARAMETER DatabaseName
  Default: UletiSmenuDb_Test

.EXAMPLE
  .\scripts\provision-test-database.ps1 -SqlServerName uletismenu-staging-sql
#>
[CmdletBinding()]
param(
    [string] $ResourceGroup = "rg-uletismenu-staging",
    [Parameter(Mandatory = $true)]
    [string] $SqlServerName,
    [string] $DatabaseName = "UletiSmenuDb_Test"
)

$ErrorActionPreference = "Stop"

if (-not (az account show 2>$null)) { throw "Run: az login" }

Write-Host "Creating TEST database $DatabaseName on $SqlServerName ..." -ForegroundColor Cyan

az sql db create `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --name $DatabaseName `
    --edition GeneralPurpose `
    --compute-model Serverless `
    --family Gen5 `
    --capacity 1 `
    --auto-pause-delay 60 `
    --backup-storage-redundancy Local `
    --output none 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Serverless create failed; trying Basic tier ..." -ForegroundColor Yellow
    az sql db create `
        --resource-group $ResourceGroup `
        --server $SqlServerName `
        --name $DatabaseName `
        --edition Basic `
        --capacity 5 `
        --output none
}

Write-Host ""
Write-Host "TEST database ready: $DatabaseName" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Build connection string for Render TEST API:"
Write-Host "     Server=tcp:$SqlServerName.database.windows.net,1433;Initial Catalog=$DatabaseName;..."
Write-Host "  2. Deploy render-test.yaml in Render dashboard (Blueprint)."
Write-Host "  3. Set ConnectionStrings__UletiSmenu in Render TEST service."
Write-Host "  4. Cloudflare Pages: connect develop branch -> test.app.uletismenu.com"
Write-Host "  5. DNS: CNAME api-test -> Render TEST hostname (grey cloud during setup)"
