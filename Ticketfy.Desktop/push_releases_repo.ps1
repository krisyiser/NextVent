$ErrorActionPreference = "Stop"
$ReleasesDir = "c:\Users\YERSI\.gemini\antigravity-ide\scratch\NextVent\Ticketfy.Desktop\Output\Releases"
$GitDir = "c:\Users\YERSI\.gemini\antigravity-ide\scratch\NextVent\TicketfyReleasesRepo"

if (Test-Path $GitDir) {
    Remove-Item -Recurse -Force $GitDir
}
New-Item -ItemType Directory -Path $GitDir | Out-Null

Write-Host "Copiando binarios e instalador v3.1.48..."
Copy-Item "$ReleasesDir\Ticketfy-Setup-v3.1.48-x64.exe" "$GitDir\" -Force
Copy-Item "$ReleasesDir\Ticketfy-Setup-v3.1.48-x86.exe" "$GitDir\" -Force
Copy-Item "$ReleasesDir\Ticketfy-Instalador-v3.1.48-x64.zip" "$GitDir\" -Force
Copy-Item "$ReleasesDir\Ticketfy-Portable-v3.1.48-x64.zip" "$GitDir\" -Force
Copy-Item "$ReleasesDir\releases.json" "$GitDir\" -Force

Set-Location $GitDir
git init
git -c http.sslVerify=false remote add origin https://git.valcore/yersi/ticketfy-releases.git
git add .
git -c http.sslVerify=false commit -m "release: v3.1.48"
git -c http.sslVerify=false push -f origin HEAD:refs/heads/main
Write-Host "¡Repositorio git.valcore/yersi/ticketfy-releases actualizado exitosamente a v3.1.48!" -ForegroundColor Green
