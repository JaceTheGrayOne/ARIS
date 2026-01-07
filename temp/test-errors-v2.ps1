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
    $response | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Status: Failed (as expected)" -ForegroundColor Green
    Write-Host "HTTP Status Code: $($_.Exception.Response.StatusCode.value__)"
    Write-Host "Exception Message: $($_.Exception.Message)"
    
    if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
        try {
            $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "Error Response:"
            $errorResponse | ConvertTo-Json -Depth 5
        } catch {
            Write-Host "Raw Error Details: $($_.ErrorDetails.Message)"
        }
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
    $response | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Status: Failed (as expected)" -ForegroundColor Green
    Write-Host "HTTP Status Code: $($_.Exception.Response.StatusCode.value__)"
    Write-Host "Exception Message: $($_.Exception.Message)"
    
    if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
        try {
            $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "Error Response:"
            $errorResponse | ConvertTo-Json -Depth 5
        } catch {
            Write-Host "Raw Error Details: $($_.ErrorDetails.Message)"
        }
    }
}
