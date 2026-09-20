$ErrorActionPreference = "Stop"

$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$Project = Join-Path $RepoRoot "src/PortCore/PortCore.csproj"
$Source = Join-Path $RepoRoot "src/PortCore/bin/Release/netstandard2.1/TimeClickers.PortCore.dll"
$DestinationDir = Join-Path $RepoRoot "unity/TimeCli/Assets/Plugins/TimeCli"
$Destination = Join-Path $DestinationDir "TimeClickers.PortCore.dll"

Write-Host "Building PortCore for Unity (netstandard2.1)..."
dotnet build $Project -f netstandard2.1 -c Release

if ($LASTEXITCODE -ne 0) {
    throw "PortCore build failed."
}

if (-not (Test-Path $Source)) {
    throw "Expected output not found: $Source"
}

New-Item -ItemType Directory -Force -Path $DestinationDir | Out-Null
Copy-Item -Force $Source $Destination

Write-Host "Synced:"
Write-Host "  $Destination"
