param(
    [string]$UnityPath = "C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.24f1\\Editor\\Unity.exe",
    [string]$PrivateVoxelJson = "",
    [string]$OriginalDataSource = "",
    [switch]$RequireWeaponHierarchy,
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

Write-Host "1/5 Building PortCore for Unity..."
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

    Write-Host "2/5 Building private canonical voxel catalog..."
    python $Converter $PrivateVoxelJson $OutputCatalog

    if ($LASTEXITCODE -ne 0) {
        throw "Private voxel catalog conversion failed."
    }
}
else {
    Write-Host "2/5 No private voxel JSON supplied; Unity will use the development catalog."
}

if ($OriginalDataSource) {
    if (-not (Test-Path $OriginalDataSource)) {
        throw "Original Data/sharedassets source not found: $OriginalDataSource"
    }

    $PrivateResources = Join-Path $ProjectPath "Assets/TimeCli/PrivateGenerated/Resources"
    $TextureExtractor = Join-Path $PSScriptRoot "extract-private-special-textures.py"

    Write-Host "3/5 Extracting private TimeCube/WeaponCube textures..."
    python $TextureExtractor $OriginalDataSource $PrivateResources

    if ($LASTEXITCODE -ne 0) {
        throw "Special-cube texture extraction failed."
    }
}
else {
    Write-Host "3/5 No original Data source supplied; special cubes use procedural fallback visuals."
}

if ($OriginalDataSource) {
    $HierarchyOutput = Join-Path $ProjectPath "Assets/TimeCli/PrivateGenerated/Resources/TimeCliWeaponHierarchy.json"
    $HierarchyExtractor = Join-Path $PSScriptRoot "extract-private-weapon-hierarchy.py"
    $HierarchyValidator = Join-Path $PSScriptRoot "validate-private-weapon-hierarchy.py"

    python -c "import UnityPy" 2>$null
    $HasUnityPy = ($LASTEXITCODE -eq 0)

    if ($HasUnityPy) {
        Write-Host "4/5 Extracting and validating private click-weapon hierarchy..."
        python $HierarchyExtractor $OriginalDataSource $HierarchyOutput
        if ($LASTEXITCODE -ne 0) {
            throw "Click-weapon hierarchy extraction failed."
        }

        python $HierarchyValidator $HierarchyOutput
        if ($LASTEXITCODE -ne 0) {
            throw "Click-weapon hierarchy validation failed."
        }
    }
    elseif ($RequireWeaponHierarchy) {
        throw "UnityPy is required for exact click-weapon hierarchy recovery. Run: python -m pip install UnityPy"
    }
    else {
        Write-Warning "4/5 UnityPy is not installed; exact click-weapon hierarchy extraction was skipped. Install with: python -m pip install UnityPy"
    }
}
else {
    if ($RequireWeaponHierarchy) {
        throw "-RequireWeaponHierarchy needs -OriginalDataSource."
    }

    Write-Host "4/5 No original Data source supplied; click-weapon hierarchy extraction skipped."
}

Write-Host "5/5 Running Unity batch compile + Arena scene generation..."
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
