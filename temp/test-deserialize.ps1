$deserializeRequest = @{
    inputAssetPath = "G:/Development/ARIS(CS)/ARIS/tests/Aris.Core.Tests/TestAssets/Assault_M1A1Thompson_WW2_DrumSuppressor.uasset"
    outputJsonPath = "G:/Development/ARIS(CS)/ARIS/temp/deserialize-test.json"
    timeoutSeconds = 60
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/uasset/deserialize" -Method Post -Body $deserializeRequest -ContentType "application/json"

$response | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "=== File Verification ==="
$fileExists = Test-Path "G:/Development/ARIS(CS)/ARIS/temp/deserialize-test.json"
Write-Host "File Exists: $fileExists"

if ($fileExists) {
    $fileInfo = Get-Item "G:/Development/ARIS(CS)/ARIS/temp/deserialize-test.json"
    Write-Host "File Size: $($fileInfo.Length) bytes"
}
