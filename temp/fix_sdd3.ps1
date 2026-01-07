$content = Get-Content "G:\Development\ARIS(GSD)\ARIS\docs\SOFTWARE_DESIGN_DOCUMENT.md" -Raw

$old = @'
**Protocol:**
1. Client connects via WebSocket
2. Client sends JSON message with `RetocStreamRequest`
3. Server sends NDJSON stream of `RetocStreamEvent` (one JSON object per line)
4. Client can send `{"action": "cancel"}` to abort execution
5. Connection closes after `exited` or `error` event
'@

$new = @'
**Protocol:**
1. Client connects via WebSocket
2. Client sends JSON message with `RetocStreamRequest`
3. Server sends NDJSON events via WebSocket TEXT frames (one JSON object + newline per frame)
4. Client can send `{"action": "cancel"}` to abort execution
5. Connection closes after `exited` or `error` event

> **Framing:** Each WebSocket message is a complete TEXT frame containing one JSON object followed by `\n`. The client splits by newline defensively, though in practice each frame contains exactly one event.
'@

$content = $content.Replace($old, $new)

$content | Set-Content "G:\Development\ARIS(GSD)\ARIS\docs\SOFTWARE_DESIGN_DOCUMENT.md" -NoNewline
