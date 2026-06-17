<#
.SYNOPSIS
  Create Render staging services (API web service + frontend static site).

.DESCRIPTION
  Requires RENDER_API_KEY and pushed render.yaml in both GitHub repos.
  Creates services in workspace UletiSmenu (tea-d8p5ef8js32c738ds2eg).

  After run: set ConnectionStrings__UletiSmenu in Render Dashboard for the API service.

.EXAMPLE
  $env:RENDER_API_KEY = "rnd_..."
  .\scripts\provision-render-staging.ps1
#>
[CmdletBinding()]
param(
    [string] $OwnerId = "tea-d8p5ef8js32c738ds2eg",
    [string] $ApiRepo = "https://github.com/lukavi44/uleti-smenu-backend",
    [string] $WebRepo = "https://github.com/lukavi44/uleti-smenu_react",
    [string] $Branch = "main"
)

$ErrorActionPreference = "Stop"

if (-not $env:RENDER_API_KEY) { throw "Set RENDER_API_KEY first." }

$headers = @{
    Authorization = "Bearer $env:RENDER_API_KEY"
    "Content-Type" = "application/json"
    Accept = "application/json"
}

function Invoke-RenderApi {
    param([string] $Method, [string] $Uri, [object] $Body)
    $json = if ($Body) { $Body | ConvertTo-Json -Depth 10 } else { $null }
    try {
        return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers -Body $json
    }
    catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $detail = $reader.ReadToEnd()
            throw "Render API error: $detail"
        }
        throw
    }
}

function Get-RenderServiceByName {
    param([string] $Name)
    $all = Invoke-RenderApi -Method Get -Uri "https://api.render.com/v1/services?ownerId=$OwnerId&limit=100"
    foreach ($item in $all) {
        if ($item.service.name -eq $Name) { return $item.service }
    }
    return $null
}

Write-Host "Workspace: UletiSmenu ($OwnerId)" -ForegroundColor Cyan

$apiName = "uletismenu-api-staging"
$webName = "uletismenu-web-staging"

$existingApi = Get-RenderServiceByName -Name $apiName
if ($existingApi) {
    Write-Host "API service already exists: $($existingApi.id)" -ForegroundColor Yellow
} else {
    Write-Host "Creating API web service: $apiName ..." -ForegroundColor Cyan
    $apiBody = @{
        type = "web_service"
        name = $apiName
        ownerId = $OwnerId
        repo = $ApiRepo
        branch = $Branch
        autoDeploy = "yes"
        rootDir = "UletiSmenu"
        serviceDetails = @{
            env = "docker"
            dockerfilePath = "Dockerfile"
            plan = "free"
            region = "frankfurt"
            healthCheckPath = "/health"
        }
        envVars = @(
            @{ key = "ASPNETCORE_ENVIRONMENT"; value = "Staging" }
            @{ key = "ConnectionStrings__UletiSmenu"; value = "REPLACE_IN_DASHBOARD" }
            @{ key = "Cors__AllowedOrigins__0"; value = "https://uletismenu-web-staging.onrender.com" }
            @{ key = "Cors__AllowedOrigins__1"; value = "http://localhost:5173" }
            @{ key = "FileSettings__UploadPath"; value = "/app/uploads" }
            @{ key = "DataProtection__KeysPath"; value = "/app/data-protection-keys" }
            @{ key = "Stripe__Enabled"; value = "false" }
        )
    }
    $created = Invoke-RenderApi -Method Post -Uri "https://api.render.com/v1/services" -Body $apiBody
    Write-Host "Created API: $($created.service.id)" -ForegroundColor Green
}

$existingWeb = Get-RenderServiceByName -Name $webName
if ($existingWeb) {
    Write-Host "Static site already exists: $($existingWeb.id)" -ForegroundColor Yellow
} else {
    Write-Host "Creating static site: $webName ..." -ForegroundColor Cyan
    $webBody = @{
        type = "static_site"
        name = $webName
        ownerId = $OwnerId
        repo = $WebRepo
        branch = $Branch
        autoDeploy = "yes"
        rootDir = "uleti-smenu"
        serviceDetails = @{
            buildCommand = "npm ci && npm run build"
            publishPath = "dist"
        }
        envVars = @(
            @{ key = "VITE_API_BASE_URL"; value = "https://uletismenu-api-staging.onrender.com" }
        )
    }
    $created = Invoke-RenderApi -Method Post -Uri "https://api.render.com/v1/services" -Body $webBody
    Write-Host "Created static site: $($created.service.id)" -ForegroundColor Green
}

Write-Host ""
Write-Host "URLs (after first deploy):" -ForegroundColor Yellow
Write-Host "  API:  https://$apiName.onrender.com"
Write-Host "  Web:  https://$webName.onrender.com"
Write-Host ""
Write-Host "IMPORTANT: Push render.yaml + Dockerfile to GitHub, then set ConnectionStrings__UletiSmenu on the API service in Render Dashboard." -ForegroundColor Yellow
Write-Host "Allow Render outbound IPs on Azure SQL firewall (or enable Allow Azure services)." -ForegroundColor Yellow
