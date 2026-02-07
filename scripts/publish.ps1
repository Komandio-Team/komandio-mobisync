# Komandio Mobisync - Build & Publish Script
# Generates a self-contained, single-file executable

$ProjectName = "MobiSync"
$ProjectPath = "$PSScriptRoot/../Komandio.Tools.Mobisync/Komandio.Tools.Mobisync.csproj"
$OutPath = "$PSScriptRoot/../publish"

Write-Host "--- Starting Publish Process: $ProjectName ---" -ForegroundColor Cyan

# 1. Stop existing process if running
Write-Host "Checking for running instances..." -ForegroundColor Gray
Get-Process $ProjectName -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Clean old publish folder
if (Test-Path $OutPath) {
    Write-Host "Cleaning output directory..." -ForegroundColor Gray
    Remove-Item $OutPath -Recurse -Force
}

# 3. Build and Publish
Write-Host "Building single-file binary (win-x64)..." -ForegroundColor Yellow
dotnet publish $ProjectPath -c Release -o $OutPath --nologo

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSuccessfully published to: $OutPath\$ProjectName.exe" -ForegroundColor Green
    $size = (Get-Item "$OutPath\$ProjectName.exe").Length / 1MB
    Write-Host "Binary size: $('{0:N2}' -f $size) MB" -ForegroundColor Gray
} else {
    Write-Host "`nBuild failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}
