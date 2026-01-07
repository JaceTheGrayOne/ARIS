$serializeRequest = @{
    inputJsonPath = "G:/Development/ARIS(CS)/ARIS/temp/deserialize-test.json"
    outputAssetPath = "G:/Development/ARIS(CS)/ARIS/temp/serialize-test.uasset"
    timeoutSeconds = 60
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/uasset/serialize" -Method Post -Body $serializeRequest -ContentType "application/json"

$response | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "=== File Verification ==="
$fileExists = Test-Path "G:/Development/ARIS(CS)/ARIS/temp/serialize-test.uasset"
Write-Host "File Exists: $fileExists"

if ($fileExists) {
    $fileInfo = Get-Item "G:/Development/ARIS(CS)/ARIS/temp/serialize-test.uasset"
    Write-Host "File Size: $($fileInfo.Length) bytes"
    
    $bytes = [System.IO.File]::ReadAllBytes("G:/Development/ARIS(CS)/ARIS/temp/serialize-test.uasset")
    Write-Host "First 4 bytes (hex): $([BitConverter]::ToString($bytes[0..3]))"
}
