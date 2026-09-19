param(
    [string]$InstallerPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($env:OS -ne "Windows_NT") {
    throw "Installed package smoke only runs on Windows."
}
if ($env:GITHUB_ACTIONS -ne "true") {
    throw "Installed package smoke is CI-only to avoid changing a developer's installed CONGTY registry markers."
}

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$releaseFile = Join-Path $repoRoot "release.json"
if (-not (Test-Path -LiteralPath $releaseFile -PathType Leaf)) {
    throw "Missing release.json."
}

$release = Get-Content -LiteralPath $releaseFile -Raw | ConvertFrom-Json
$version = [string]$release.version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "release.json.version is required."
}

if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
    $InstallerPath = Join-Path $repoRoot "dist\windows-release\CONGTY-Setup-$version.exe"
}
$InstallerPath = [IO.Path]::GetFullPath($InstallerPath)
if (-not (Test-Path -LiteralPath $InstallerPath -PathType Leaf)) {
    throw "Missing release installer: $InstallerPath"
}

$installDirectory = Join-Path $env:LOCALAPPDATA "Programs\CONGTY-CI"
$productRegistryPath = "HKCU:\Software\CongTy\Desktop"
$uninstallRegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\CONGTY"
$productExe = Join-Path $installDirectory "CongTy.Desktop.exe"
$uninstaller = Join-Path $installDirectory "Uninstall.exe"
$diagnostic = Join-Path $installDirectory "startup-smoke-error.txt"

function Normalize-Directory([string]$Path) {
    return [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($Path))
}

function Assert-Equal([string]$Expected, [string]$Actual, [string]$Message) {
    if (-not [string]::Equals($Expected, $Actual, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Message Expected='$Expected' Actual='$Actual'."
    }
}

function Invoke-AndCheck([string]$FilePath, [string[]]$Arguments, [string]$Label) {
    $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -PassThru -Wait
    if ($process.ExitCode -ne 0) {
        throw "$Label failed. ExitCode=$($process.ExitCode)"
    }
}

function Remove-SmokeState {
    if (Test-Path -LiteralPath $uninstaller -PathType Leaf) {
        try {
            Invoke-AndCheck $uninstaller @("/S") "Silent uninstall"
        } catch {
            Write-Warning $_
        }
    }

    Remove-Item -LiteralPath $installDirectory -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $productRegistryPath -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $uninstallRegistryPath -Recurse -Force -ErrorAction SilentlyContinue
}

if (Test-Path -LiteralPath $installDirectory) {
    throw "CI runner is not clean: test install directory already exists."
}
if (Test-Path -LiteralPath $productRegistryPath) {
    throw "CI runner is not clean: CONGTY product registry key already exists."
}
if (Test-Path -LiteralPath $uninstallRegistryPath) {
    throw "CI runner is not clean: CONGTY uninstall registry key already exists."
}

try {
    Invoke-AndCheck $InstallerPath @("/S", "/D=$installDirectory") "Silent install"

    if (-not (Test-Path -LiteralPath $productExe -PathType Leaf)) {
        throw "Installed executable is missing: $productExe"
    }
    if (-not (Test-Path -LiteralPath $uninstaller -PathType Leaf)) {
        throw "Installed uninstaller is missing: $uninstaller"
    }

    $product = Get-ItemProperty -LiteralPath $productRegistryPath -ErrorAction Stop
    Assert-Equal (Normalize-Directory $installDirectory) (Normalize-Directory ([string]$product.InstallDir)) "InstallDir registry mismatch."
    Assert-Equal $version ([string]$product.Version) "Product registry version mismatch."

    $uninstall = Get-ItemProperty -LiteralPath $uninstallRegistryPath -ErrorAction Stop
    Assert-Equal $version ([string]$uninstall.DisplayVersion) "Uninstall registry version mismatch."
    Assert-Equal (Normalize-Directory $installDirectory) (Normalize-Directory ([string]$uninstall.InstallLocation)) "Uninstall registry location mismatch."

    $productVersion = [string](Get-Item -LiteralPath $productExe).VersionInfo.ProductVersion
    $normalizedProductVersion = ($productVersion -split '\+', 2)[0]
    Assert-Equal $version $normalizedProductVersion "Installed executable product version mismatch."

    Remove-Item -LiteralPath $diagnostic -Force -ErrorAction SilentlyContinue
    $smoke = Start-Process -FilePath $productExe -ArgumentList @("--installed-package-smoke") -PassThru -Wait
    if ($smoke.ExitCode -ne 0) {
        if (Test-Path -LiteralPath $diagnostic -PathType Leaf) {
            Write-Host "----- startup-smoke-error.txt -----"
            Get-Content -LiteralPath $diagnostic | Write-Host
            Write-Host "-----------------------------------"
        }
        throw "Installed application smoke failed. ExitCode=$($smoke.ExitCode)"
    }

    Write-Host "Installed package smoke PASS: CONGTY v$version"
}
finally {
    Remove-SmokeState
}

if (Test-Path -LiteralPath $installDirectory) {
    throw "Installed package smoke cleanup failed: install directory still exists."
}
if (Test-Path -LiteralPath $productRegistryPath) {
    throw "Installed package smoke cleanup failed: product registry key still exists."
}
if (Test-Path -LiteralPath $uninstallRegistryPath) {
    throw "Installed package smoke cleanup failed: uninstall registry key still exists."
}
