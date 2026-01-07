$content = Get-Content "G:\Development\ARIS(GSD)\ARIS\docs\SOFTWARE_DESIGN_DOCUMENT.md" -Raw
$old = "Client can send  to abort"
$new = 'Client can send `{"action": "cancel"}` to abort'
$content = $content.Replace($old, $new)
$content | Set-Content "G:\Development\ARIS(GSD)\ARIS\docs\SOFTWARE_DESIGN_DOCUMENT.md" -NoNewline
