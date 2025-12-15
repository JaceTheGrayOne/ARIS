param(
    [Parameter(Mandatory=$true)][string]$ToolsDir,
    [Parameter(Mandatory=$true)][string]$ManifestPath,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$retocDir = Join-Path $ToolsDir 'retoc'
$retocExe = Join-Path $retocDir 'retoc.exe'

# 1) Short-circuit if retoc.exe exists and Force is not set
if (-not $Force -and (Test-Path $retocExe)) {
    Write-Host "Retoc executable already present at $retocExe. Skipping download."
    return
}

Write-Host "Downloading latest Retoc release from GitHub..."

# Ensure retoc directory exists
New-Item -ItemType Directory -Force -Path $retocDir | Out-Null

# 2) Query GitHub latest release for trumank/retoc
$repo = 'trumank/retoc'
$apiUrl = "https://api.github.com/repos/$repo/releases/latest"

$headers = @{
    'User-Agent' = 'ARIS-BuildScript'
    'Accept' = 'application/vnd.github.v3+json'
}

try {
    $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers -UseBasicParsing
}
catch {
    Write-Error "Failed to query GitHub API for $repo releases. Error: $($_.Exception.Message)"
    Write-Error "This may be due to rate limiting or network issues. Try again later or download manually."
    throw
}

# 3) Find the Windows x64 asset (retoc-x86_64-pc-windows-msvc.zip)
$asset = $release.assets | Where-Object { $_.name -like 'retoc-x86_64-pc-windows-msvc.zip' } | Select-Object -First 1
if (-not $asset) {
    throw "Could not find retoc-x86_64-pc-windows-msvc.zip in latest release assets. Available assets: $($release.assets.name -join ', ')"
}

$downloadUrl = $asset.browser_download_url
$releaseTag = $release.tag_name
Write-Host "Found Retoc $releaseTag at $downloadUrl"

# 4) Download zip to temp, extract, copy retoc.exe
$tmpDir = New-Item -ItemType Directory -Force -Path (Join-Path $env:TEMP 'ARIS-RetocDownload')
$zipPath = Join-Path $tmpDir 'retoc.zip'

Write-Host "Downloading to $zipPath..."
try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath -UseBasicParsing
}
catch {
    Write-Error "Failed to download Retoc from $downloadUrl. Error: $($_.Exception.Message)"
    throw
}

$extractDir = Join-Path $tmpDir 'retoc-extracted'
if (Test-Path $extractDir) {
    Remove-Item $extractDir -Recurse -Force
}

Write-Host "Extracting archive..."
Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

# Expect retoc.exe somewhere in extracted folder; usually root
$extractedExe = Get-ChildItem -Path $extractDir -Recurse -Filter 'retoc.exe' | Select-Object -First 1
if (-not $extractedExe) {
    throw "Downloaded retoc archive does not contain retoc.exe. Archive contents: $(Get-ChildItem $extractDir -Recurse | Select-Object -ExpandProperty Name)"
}

Write-Host "Copying retoc.exe to $retocExe..."
Copy-Item -Path $extractedExe.FullName -Destination $retocExe -Force

# 5) Compute SHA-256 and file size
Write-Host "Computing SHA-256 hash and file size..."
$fileInfo = Get-Item $retocExe
$sizeBytes = $fileInfo.Length
$hashObj = Get-FileHash -Algorithm SHA256 -Path $retocExe
$sha256 = $hashObj.Hash.ToLowerInvariant()

Write-Host "Retoc SHA-256: $sha256"
Write-Host "Retoc size: $sizeBytes bytes"

# 6) Update tools.manifest.json entry for id == 'retoc'
Write-Host "Updating $ManifestPath..."
$jsonText = Get-Content $ManifestPath -Raw -Encoding UTF8
$manifest = $jsonText | ConvertFrom-Json

$retocEntry = $manifest.tools | Where-Object { $_.id -eq 'retoc' } | Select-Object -First 1
if (-not $retocEntry) {
    throw "Could not find 'retoc' entry in tools.manifest.json."
}

$retocEntry.sha256 = $sha256
$retocEntry.size = $sizeBytes

# Update version field to release tag
if ($releaseTag) {
    $retocEntry.version = $releaseTag
}

# Save manifest with proper formatting
$manifestJson = $manifest | ConvertTo-Json -Depth 10
Set-Content -Path $ManifestPath -Value $manifestJson -Encoding UTF8

Write-Host "Successfully updated retoc tool:"
Write-Host "  Version: $releaseTag"
Write-Host "  SHA-256: $sha256"
Write-Host "  Size: $sizeBytes bytes"
Write-Host "  Path: $retocExe"

# Cleanup temp directory
Write-Host "Cleaning up temporary files..."
Remove-Item $tmpDir -Recurse -Force -ErrorAction SilentlyContinue
