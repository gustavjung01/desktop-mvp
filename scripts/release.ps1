param(
    [switch]$SkipRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($env:OS -ne "Windows_NT") {
    throw "CONGTY release packaging only runs on Windows."
}

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$releaseFile = Join-Path $repoRoot "release.json"
$projectFile = Join-Path $repoRoot "src\CongTy.Desktop\CongTy.Desktop.csproj"
$outputDirectory = Join-Path $repoRoot "dist\windows-release"
$stagingDirectory = Join-Path $repoRoot "dist\windows-release-staging"
$installerScript = Join-Path $repoRoot "installer\congty.nsi"
$iconScript = Join-Path $repoRoot "scripts\create-brand-icon.ps1"
$logoSource = Join-Path $repoRoot "logo.jpg"
$iconDestination = Join-Path $repoRoot "src\CongTy.Desktop\Assets\Brand\logo.ico"

if (-not (Test-Path -LiteralPath $releaseFile -PathType Leaf)) {
    throw "Missing release.json."
}

$release = Get-Content -LiteralPath $releaseFile -Raw | ConvertFrom-Json
$version = [string]$release.version
$semVerPattern = '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$'
if ([string]::IsNullOrWhiteSpace($version) -or $version -notmatch $semVerPattern) {
    throw "release.json.version must be valid SemVer."
}

if (-not [string]::IsNullOrWhiteSpace($env:KM_RELEASE_VERSION) -and $env:KM_RELEASE_VERSION -ne $version) {
    throw "KM_RELEASE_VERSION '$($env:KM_RELEASE_VERSION)' does not match release.json.version '$version'."
}

$numericVersion = ($version -split '[-+]')[0]
$assemblyVersion = "$numericVersion.0"
$releaseNotes = if ([string]::IsNullOrWhiteSpace($env:KM_RELEASE_NOTES)) {
    "Cập nhật CONGTY v$version."
} else {
    $env:KM_RELEASE_NOTES.Trim()
}

Remove-Item -LiteralPath $outputDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $stagingDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $stagingDirectory | Out-Null

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $iconScript -Source $logoSource -Destination $iconDestination
if ($LASTEXITCODE -ne 0) {
    throw "Could not generate CONGTY icon."
}

if (-not $SkipRestore) {
    & dotnet restore $projectFile --runtime win-x64
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
}

$publishArgs = @(
    "publish",
    $projectFile,
    "--configuration", "Release",
    "--runtime", "win-x64",
    "--self-contained", "true",
    "--output", $stagingDirectory,
    "--no-restore",
    "/p:Version=$version",
    "/p:AssemblyVersion=$assemblyVersion",
    "/p:FileVersion=$assemblyVersion",
    "/p:InformationalVersion=$version"
)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

$makeNsis = Get-Command makensis.exe -ErrorAction SilentlyContinue
$makeNsisPath = if ($makeNsis) {
    $makeNsis.Source
} else {
    @(
        (Join-Path ${env:ProgramFiles(x86)} "NSIS\makensis.exe"),
        (Join-Path $env:ProgramFiles "NSIS\makensis.exe")
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) } | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($makeNsisPath)) {
    throw "makensis.exe was not found. Install NSIS before building a release."
}

& $makeNsisPath "/DAPP_VERSION=$version" "/DSTAGING_DIR=$stagingDirectory" "/DOUTPUT_DIR=$outputDirectory" "/DICON_PATH=$iconDestination" $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "NSIS packaging failed."
}

$installerName = "CONGTY-Setup-$version.exe"
$installerPath = Join-Path $outputDirectory $installerName
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw "Expected installer was not produced: $installerName"
}

$installer = Get-Item -LiteralPath $installerPath
if ($installer.Length -le 0) {
    throw "Installer is empty."
}

$hash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
$manifestPath = Join-Path $outputDirectory "latest.json"
$manifest = [ordered]@{
    schemaVersion = 1
    latestVersion = $version
    releaseNotes = $releaseNotes
    downloadPath = $installerName
    sha256 = $hash
    size = $installer.Length
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding utf8

$validated = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($validated.schemaVersion -ne 1) { throw "latest.json schemaVersion is invalid." }
if ($validated.latestVersion -ne $version) { throw "latest.json version does not match release.json." }
if ($validated.downloadPath -ne $installerName) { throw "latest.json points to the wrong installer." }
if ($validated.sha256 -ne $hash) { throw "latest.json SHA-256 does not match installer." }
if ([Int64]$validated.size -ne $installer.Length) { throw "latest.json size does not match installer." }

Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
$unexpected = Get-ChildItem -LiteralPath $outputDirectory -File | Where-Object {
    $_.Name -notin @($installerName, "latest.json")
}
if ($unexpected) {
    throw "Release output contains unexpected artifacts: $($unexpected.Name -join ', ')"
}

Write-Host "Release package ready:"
Write-Host "  $installerPath"
Write-Host "  $manifestPath"
