$inspectRoundTripRequest = @{
    inputAssetPath = "G:/Development/ARIS(CS)/ARIS/temp/serialize-test.uasset"
    fields = @("exports", "imports", "names")
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/uasset/inspect" -Method Post -Body $inspectRoundTripRequest -ContentType "application/json"

$response | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "=== Metadata Comparison ==="
Write-Host "Original Export Count: 2"
Write-Host "Round-Trip Export Count: $($response.result.summary.exportCount)"
Write-Host ""
Write-Host "Original Import Count: 25"
Write-Host "Round-Trip Import Count: $($response.result.summary.importCount)"
Write-Host ""
Write-Host "Original Name Count: 187"
Write-Host "Round-Trip Name Count: $($response.result.summary.nameCount)"
Write-Host ""
Write-Host "Original UE Version: 4.13"
Write-Host "Round-Trip UE Version: $($response.result.summary.ueVersion)"
Write-Host ""

$exportMatch = ($response.result.summary.exportCount -eq 2)
$importMatch = ($response.result.summary.importCount -eq 25)
$nameMatch = ($response.result.summary.nameCount -eq 187)
$versionMatch = ($response.result.summary.ueVersion -eq "4.13")

Write-Host "Export Count Match: $exportMatch"
Write-Host "Import Count Match: $importMatch"
Write-Host "Name Count Match: $nameMatch"
Write-Host "UE Version Match: $versionMatch"
Write-Host ""

if ($exportMatch -and $importMatch -and $nameMatch -and $versionMatch) {
    Write-Host "ROUND-TRIP VALIDATION: PASSED (All metadata matches)" -ForegroundColor Green
} else {
    Write-Host "ROUND-TRIP VALIDATION: FAILED (Metadata mismatch detected)" -ForegroundColor Red
}
