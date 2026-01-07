#Requires -Version 5.1
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent

Write-Host "=== ARIS Release Build ===" -ForegroundColor Cyan

# Step 1: Clean artifacts
Write-Host "`n[1/6] Cleaning artifacts..." -ForegroundColor Yellow
Remove-Item -Recurse -Force "$RepoRoot/$OutputDir" -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path "$RepoRoot/$OutputDir/payload" -Force | Out-Null

# Step 2: Build frontend
Write-Host "`n[2/6] Building frontend..." -ForegroundColor Yellow
Push-Location "$RepoRoot/frontend"
try {
    npm ci
    if ($LASTEXITCODE -ne 0) { throw "npm ci failed with exit code $LASTEXITCODE" }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed with exit code $LASTEXITCODE" }
} finally {
    Pop-Location
}

# Step 3: Verify no hardcoded URLs in frontend build
Write-Host "`n[3/6] Verifying frontend uses relative URLs..." -ForegroundColor Yellow
$jsFiles = Get-ChildItem -Path "$RepoRoot/frontend/dist/assets/*.js" -File -ErrorAction SilentlyContinue
foreach ($file in $jsFiles) {
    $content = Get-Content $file -Raw
    if ($content -match 'localhost:5000') {
        throw "FAIL: $($file.Name) contains hardcoded localhost:5000"
    }
}
Write-Host "  PASS: No hardcoded backend URLs in production JS"

# Step 4: Publish Aris.Hosting
Write-Host "`n[4/6] Publishing Aris.Hosting..." -ForegroundColor Yellow
dotnet publish "$RepoRoot/src/Aris.Hosting/Aris.Hosting.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -o "$RepoRoot/$OutputDir/payload" `
    /p:PublishSingleFile=true
if ($LASTEXITCODE -ne 0) { throw "dotnet publish Aris.Hosting failed" }

# Copy frontend to wwwroot
New-Item -ItemType Directory -Path "$RepoRoot/$OutputDir/payload/wwwroot" -Force | Out-Null
Copy-Item -Recurse "$RepoRoot/frontend/dist/*" "$RepoRoot/$OutputDir/payload/wwwroot/"

# Step 5: Create payload.zip
Write-Host "`n[5/6] Creating payload.zip..." -ForegroundColor Yellow
Compress-Archive -Path "$RepoRoot/$OutputDir/payload/*" `
    -DestinationPath "$RepoRoot/$OutputDir/payload.zip" -Force

# Step 6: Publish ARIS.UI
Write-Host "`n[6/6] Publishing ARIS.UI..." -ForegroundColor Yellow
dotnet publish "$RepoRoot/src/ARIS.UI/ARIS.UI.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -o "$RepoRoot/$OutputDir/release" `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true
if ($LASTEXITCODE -ne 0) { throw "dotnet publish ARIS.UI failed" }

# Verify output
$exe = Get-Item "$RepoRoot/$OutputDir/release/ARIS.exe" -ErrorAction Stop
Write-Host "`n=== Build Complete ===" -ForegroundColor Green
Write-Host "Output: $($exe.FullName)"
Write-Host "Size: $([math]::Round($exe.Length / 1MB, 2)) MB"
