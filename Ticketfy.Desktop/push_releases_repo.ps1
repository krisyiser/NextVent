param (
    [string]$Version = "3.1.55"
)

$ErrorActionPreference = "Stop"
$ReleasesDir = "c:\Users\YERSI\.gemini\antigravity-ide\scratch\NextVent\Ticketfy.Desktop\Output\Releases"
$GitDir = "c:\Users\YERSI\.gemini\antigravity-ide\scratch\NextVent\TicketfyReleasesRepo"

if (Test-Path $GitDir) {
    Remove-Item -Recurse -Force $GitDir
}
New-Item -ItemType Directory -Path $GitDir | Out-Null

Write-Host "Copiando binarios e instalador v$Version..."
if (Test-Path "$ReleasesDir\Ticketfy-Setup-v$Version-x64.exe") {
    Copy-Item "$ReleasesDir\Ticketfy-Setup-v$Version-x64.exe" "$GitDir\" -Force
}
if (Test-Path "$ReleasesDir\Ticketfy-Setup-v$Version-x86.exe") {
    Copy-Item "$ReleasesDir\Ticketfy-Setup-v$Version-x86.exe" "$GitDir\" -Force
}
if (Test-Path "$ReleasesDir\Ticketfy-Instalador-v$Version-x64.zip") {
    Copy-Item "$ReleasesDir\Ticketfy-Instalador-v$Version-x64.zip" "$GitDir\" -Force
}
if (Test-Path "$ReleasesDir\Ticketfy-Portable-v$Version-x64.zip") {
    Copy-Item "$ReleasesDir\Ticketfy-Portable-v$Version-x64.zip" "$GitDir\" -Force
}
if (Test-Path "$ReleasesDir\releases.json") {
    Copy-Item "$ReleasesDir\releases.json" "$GitDir\" -Force
}

Set-Location $GitDir
git init
git -c http.sslVerify=false remote add origin https://git.valcore/yersi/ticketfy-releases.git
git add .
git -c http.sslVerify=false commit -m "release: v$Version"
git -c http.sslVerify=false push -f origin HEAD:refs/heads/main
Write-Host "¡Repositorio git.valcore/yersi/ticketfy-releases actualizado exitosamente a v$Version!" -ForegroundColor Green

