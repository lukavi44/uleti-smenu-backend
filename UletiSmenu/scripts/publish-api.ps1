$ErrorActionPreference = "Stop"

$Root = Split-Path $PSScriptRoot -Parent
$ApiProject = Join-Path $Root "API\API.csproj"
$OutputDir = Join-Path $Root "publish\api"
$ZipPath = Join-Path $Root "publish\api.zip"

function New-LinuxCompatibleZip {
    param(
        [Parameter(Mandatory = $true)][string] $SourceDirectory,
        [Parameter(Mandatory = $true)][string] $ZipFile
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $sourceFull = (Resolve-Path $SourceDirectory).Path.TrimEnd('\')
    if (Test-Path $ZipFile) { Remove-Item $ZipFile -Force }

    $zip = [System.IO.Compression.ZipFile]::Open($ZipFile, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem -Path $sourceFull -Recurse -File | ForEach-Object {
            $relative = $_.FullName.Substring($sourceFull.Length).TrimStart('\', '/')
            $entryName = $relative -replace '\\', '/'
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $entryName)
        }
    }
    finally {
        $zip.Dispose()
    }
}

Write-Host "Publishing API (Release) to $OutputDir ..." -ForegroundColor Cyan

if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}

dotnet publish $ApiProject -c Release -o $OutputDir

Write-Host "Creating Linux-compatible zip ..." -ForegroundColor Cyan
New-LinuxCompatibleZip -SourceDirectory $OutputDir -ZipFile $ZipPath

Write-Host "Done." -ForegroundColor Green
Write-Host "  Folder: $OutputDir"
Write-Host "  Zip:    $ZipPath"
