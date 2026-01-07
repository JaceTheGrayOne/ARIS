# scripts/build-release.ps1
# Run from anywhere (it will auto-cd to repo root).
#
# Examples:
#   powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1
#   powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1 -Runtime win-x64 -Clean

param(
  [string]$Runtime = "win-x64",
  [switch]$Clean,
  [switch]$RunApp
)

$ErrorActionPreference = "Stop"

function Run([string]$cmd) {
  Write-Host "==> $cmd"
  & powershell -NoProfile -Command $cmd
  if ($LASTEXITCODE -ne 0) { throw "Command failed: $cmd" }
}

function RunDotnet([string[]]$dotnetArgs) {
  $argLine = $dotnetArgs -join ' '
  Write-Host "==> dotnet $argLine"
  & dotnet @dotnetArgs
  if ($LASTEXITCODE -ne 0) { throw "Command failed: dotnet $argLine" }
}

# Resolve repo root (script is in repoRoot\scripts\)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Resolve-Path (Join-Path $scriptDir "..")
$repoRootPath = $repoRoot.Path
$artifactsDir = Join-Path $repoRootPath "artifacts"

Push-Location $repoRoot
try {
  # Avoid locked obj/bin files by shutting down build servers
  Run "dotnet build-server shutdown"

  $buildProps = @(
    "/p:SelfContained=false",
    "/nodeReuse:false",
    "/p:UseSharedCompilation=false",
    "/m:1"
  )

  if ($Clean) {
    RunDotnet @("clean", ".\ARIS.sln", "-c", "Release")
    if (Test-Path .\frontend\node_modules) { Remove-Item .\frontend\node_modules -Recurse -Force }
    if (Test-Path .\frontend\dist) { Remove-Item .\frontend\dist -Recurse -Force }
  }

  # 1) Build + test (force framework-dependent here to avoid NETSDK1150)
  $buildArgs = @("build", ".\ARIS.sln", "-c", "Release") + $buildProps
  RunDotnet $buildArgs
  $testArgs = @("test", ".\ARIS.sln", "-c", "Release", "--no-build") + $buildProps
  RunDotnet $testArgs

  # 2) Frontend production bundle
  Push-Location .\frontend
  try {
    Run "npm install"
    Run "npm run build"
  }
  finally {
    Pop-Location
  }

  # 3) Publish Aris.Hosting and rebuild payload.zip
  $payloadDir = Join-Path $artifactsDir "payload"
  $payloadZip = Join-Path $artifactsDir "payload.zip"

  if (Test-Path $payloadDir) { Remove-Item $payloadDir -Recurse -Force }
  New-Item -ItemType Directory -Path $payloadDir -Force | Out-Null

  RunDotnet @(
    "publish",
    ".\src\Aris.Hosting\Aris.Hosting.csproj",
    "-c", "Release",
    "-r", $Runtime,
    "--self-contained", "true",
    "-o", $payloadDir,
    "/p:PublishSingleFile=true"
  )

  # Copy frontend bundle into payload wwwroot
  $wwwrootDir = Join-Path $payloadDir "wwwroot"
  if (Test-Path $wwwrootDir) { Remove-Item $wwwrootDir -Recurse -Force }
  New-Item -ItemType Directory -Path $wwwrootDir -Force | Out-Null
  Copy-Item -Recurse -Force ".\frontend\dist\*" $wwwrootDir

  if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }
  Compress-Archive -Path "$payloadDir\*" -DestinationPath $payloadZip -Force

  # 4) Publish single-exe distributable (UI bootstrapper)
  RunDotnet @(
    "publish",
    ".\src\ARIS.UI\ARIS.UI.csproj",
    "-c", "Release",
    "-r", $Runtime,
    "/p:SelfContained=true",
    "/p:PublishSingleFile=true"
  )

  $publishDir = Join-Path $repoRootPath ("src\ARIS.UI\bin\Release\net8.0-windows\{0}\publish" -f $Runtime)
  Write-Host ""
  if (Test-Path $publishDir) {
    Write-Host "Publish output:"
    Write-Host "  $publishDir"
    $exe = Get-ChildItem -Path $publishDir -Filter *.exe | Sort-Object Length -Descending | Select-Object -First 1
    Write-Host "Run:"
    if ($exe) {
    Write-Host "  `"$($exe.FullName)`""
    } else {
    Write-Host "  (no .exe found in publish output)"
    }
  } else {
    Write-Host "Publish directory not found. Search for publish output under:"
    Write-Host "  .\src\ARIS.UI\bin\Release\"
  }
  if ($RunApp -and $exe) { & $exe.FullName }

  # Copy ARIS.exe to repo root for convenience
  if ($exe) {
    $rootExe = Join-Path $repoRootPath "ARIS.exe"
    Copy-Item -Path $exe.FullName -Destination $rootExe -Force
    Write-Host "Root EXE:"
    Write-Host "  $rootExe"
  }
}
finally {
  Pop-Location
}
