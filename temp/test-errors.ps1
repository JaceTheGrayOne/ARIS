Write-Host "=== Error Test 1: Missing Input File ==="
$invalidRequest = @{
    inputAssetPath = "G:/Development/ARIS(CS)/ARIS/nonexistent.uasset"
    fields = @()
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/uasset/inspect" `
        -Method Post `
        -Body $invalidRequest `
        -ContentType "application/json"
    Write-Host "ERROR: Request should have failed but succeeded" -ForegroundColor Red
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "Status: Failed (as expected)" -ForegroundColor Green
    Write-Host "Error Code: $($errorResponse.error.code)"
    Write-Host "Error Message: $($errorResponse.error.message)"
    
    if ($errorResponse.error.message -notlike "*G:/*" -and $errorResponse.error.message -notlike "*C:/*") {
        Write-Host "Path sanitization: PASSED (no full paths in error)" -ForegroundColor Green
    } else {
        Write-Host "Path sanitization: FAILED (full path leaked)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Error Test 2: Invalid File Format ==="
$invalidFormatRequest = @{
    inputAssetPath = "G:/Development/ARIS(CS)/ARIS/temp/test-inspect.ps1"
    fields = @()
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/uasset/inspect" `
        -Method Post `
        -Body $invalidFormatRequest `
        -ContentType "application/json"
    Write-Host "ERROR: Request should have failed but succeeded" -ForegroundColor Red
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "Status: Failed (as expected)" -ForegroundColor Green
    Write-Host "Error Code: $($errorResponse.error.code)"
    Write-Host "Error Message: $($errorResponse.error.message)"
}
