$ErrorActionPreference = "Stop"

$Root = Split-Path $PSScriptRoot -Parent
$ApiProject = Join-Path $Root "API\API.csproj"
$OutputDir = Join-Path $Root "publish\api"

Write-Host "Publishing API (Release) to $OutputDir ..." -ForegroundColor Cyan

if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}

dotnet publish $ApiProject -c Release -o $OutputDir

$ZipPath = Join-Path $Root "publish\api.zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path "$OutputDir\*" -DestinationPath $ZipPath

Write-Host "Done." -ForegroundColor Green
Write-Host "  Folder: $OutputDir"
Write-Host "  Zip:    $ZipPath"
