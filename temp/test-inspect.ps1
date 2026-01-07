$inspectRequest = @{
    inputAssetPath = "G:/Development/ARIS(CS)/ARIS/tests/Aris.Core.Tests/TestAssets/Assault_M1A1Thompson_WW2_DrumSuppressor.uasset"
    fields = @("exports", "imports", "names")
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/uasset/inspect" -Method Post -Body $inspectRequest -ContentType "application/json"

$response | ConvertTo-Json -Depth 10
