param(
    [string]$UnityPath = "C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.24f1\\Editor\\Unity.exe",
    [string]$PrivateVoxelJson = "",
    [switch]$OpenAfterSetup
)

$ErrorActionPreference = "Stop"

$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$ProjectPath = Join-Path $RepoRoot "unity/TimeCli"
$LogDir = Join-Path $RepoRoot "out/unity"
$LogPath = Join-Path $LogDir "arena-prototype-batch.log"

if (-not (Test-Path $UnityPath)) {
    throw "Unity executable not found: $UnityPath"
}

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

Write-Host "1/3 Building PortCore for Unity..."
& (Join-Path $PSScriptRoot "sync-portcore.ps1")

if ($LASTEXITCODE -ne 0) {
    throw "PortCore sync failed."
}

if ($PrivateVoxelJson) {
    if (-not (Test-Path $PrivateVoxelJson)) {
        throw "Private voxel JSON not found: $PrivateVoxelJson"
    }

    $OutputCatalog = Join-Path $ProjectPath "Assets/TimeCli/PrivateGenerated/Resources/TimeCliVoxelCatalog.bytes"
    $Converter = Join-Path $PSScriptRoot "build-private-voxel-catalog.py"

    Write-Host "2/3 Building private canonical voxel catalog..."
    python $Converter $PrivateVoxelJson $OutputCatalog

    if ($LASTEXITCODE -ne 0) {
        throw "Private voxel catalog conversion failed."
    }
}
else {
    Write-Host "2/3 No private voxel JSON supplied; Unity will use the development catalog."
}

Write-Host "3/3 Running Unity batch compile + Arena scene generation..."
$UnityArgs = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath,
    "-executeMethod", "TimeCli.UnityRuntime.Editor.TimeCliProjectSetup.CreateArenaPrototypeScene",
    "-logFile", $LogPath
)
& $UnityPath @UnityArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Unity batch setup failed. Log:"
    Write-Host "  $LogPath"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Arena prototype generated successfully."
Write-Host "Scene:"
Write-Host "  $ProjectPath\\Assets\\TimeCli\\Scenes\\ArenaPrototype.unity"
Write-Host "Log:"
Write-Host "  $LogPath"

if ($OpenAfterSetup) {
    Write-Host "Opening Unity Editor..."
    Start-Process -FilePath $UnityPath -ArgumentList "-projectPath", $ProjectPath
}
