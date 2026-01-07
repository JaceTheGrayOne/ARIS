$content = Get-Content "G:\Development\ARIS(GSD)\ARIS\docs\SOFTWARE_DESIGN_DOCUMENT.md" -Raw

# Pattern to remove
$old = @'


```json
{
  "commandType": "ToZen",
  "inputPath": "C:\\mods\\input",
  "outputPath": "C:\\mods\\output.utoc",
  "engineVersion": "UE5_6",
  "aesKey": null,
  "containerHeaderVersion": null,
  "tocVersion": null,
  "chunkId": null,
  "verbose": false,
  "timeoutSeconds": null,
  "interactive": true
}
```

```json
{
  "sessionId": "a1b2c3d4e5f6",
  "executionMode": "pty",
  "interactiveAllowed": true,
  "warnings": []
}
```



'@

$content = $content.Replace($old, "`n`n")

$content | Set-Content "G:\Development\ARIS(GSD)\ARIS\docs\SOFTWARE_DESIGN_DOCUMENT.md" -NoNewline
