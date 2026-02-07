$line = '<2026-02-06T20:40:27.917Z> [Notice] <SHUDEvent_OnNotification> Added notification "Contract Accepted:  Alliance Aid: Supply Thief: " [27] to queue. New queue size: 4, MissionId: [7e79fc91-e866-4b63-acf6-4e167f5f100c], ObjectiveId: [] [Team_CoreGameplayFeatures][Missions][Comms]'
$pattern = 'Added notification "(?<text>[^"]*?)"\s*\[\d+\]\s*to queue\..*?MissionId:\s*\[(?<missionId>.*?)\]'

if ($line -match $pattern) {
    Write-Host "MATCH SUCCESS!"
    Write-Host "Text: $($Matches['text'])"
    Write-Host "MissionId: $($Matches['missionId'])"
} else {
    Write-Host "MATCH FAILED!"
}
