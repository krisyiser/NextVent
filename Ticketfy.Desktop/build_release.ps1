param (
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$ProjectPath = ".\Ticketfy.Desktop.csproj"
$ReleaseDir = ".\Output\Releases"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Compilando Ticketfy v$Version Multi-Arquitectura (x64 / x86)..." -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Publicar versión 64-bit (win-x64)
Write-Host "[1/2] Publicando versión 64-bit (win-x64)..." -ForegroundColor Yellow
$PublishDir64 = ".\bin\Release\net9.0\win-x64\publish"
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained -p:Version=$Version -p:AssemblyVersion=$Version.0 -p:FileVersion=$Version.0 /p:EnableSourceLink=false /p:EnableSourceControlManagerQueries=false /p:PublishRepositoryUrl=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error en la compilación x64." -ForegroundColor Red
    exit $LASTEXITCODE
}

# 2. Publicar versión 32-bit (win-x86)
Write-Host "[2/2] Publicando versión 32-bit (win-x86)..." -ForegroundColor Yellow
$PublishDir86 = ".\bin\Release\net9.0\win-x86\publish"
dotnet publish $ProjectPath -c Release -r win-x86 --self-contained -p:Version=$Version -p:AssemblyVersion=$Version.0 -p:FileVersion=$Version.0 /p:EnableSourceLink=false /p:EnableSourceControlManagerQueries=false /p:PublishRepositoryUrl=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error en la compilación x86." -ForegroundColor Red
    exit $LASTEXITCODE
}

# 3. Empaquetado Velopack
Write-Host "Empaquetando con Velopack (x64 y x86)..." -ForegroundColor Cyan
vpk pack -u Ticketfy.Desktop -v $Version -p $PublishDir64 -o "$ReleaseDir\x64" -e Ticketfy.Desktop.exe --packTitle "Ticketfy! (64-bit)"
vpk pack -u Ticketfy.Desktop -v $Version -p $PublishDir86 -o "$ReleaseDir\x86" -e Ticketfy.Desktop.exe --packTitle "Ticketfy! (32-bit)"

if (Test-Path "$ReleaseDir\x64\Ticketfy.Desktop-win-Setup.exe") {
    Copy-Item -Path "$ReleaseDir\x64\Ticketfy.Desktop-win-Setup.exe" -Destination "$ReleaseDir\Ticketfy-Setup-v$Version-x64.exe" -Force
    Copy-Item -Path "$ReleaseDir\x64\Ticketfy.Desktop-win-Setup.exe" -Destination "$ReleaseDir\Ticketfy-Setup-v$Version.exe" -Force
}
if (Test-Path "$ReleaseDir\x86\Ticketfy.Desktop-win-Setup.exe") {
    Copy-Item -Path "$ReleaseDir\x86\Ticketfy.Desktop-win-Setup.exe" -Destination "$ReleaseDir\Ticketfy-Setup-v$Version-x86.exe" -Force
}

# 4. Generar Manifiesto de Descargas para valcore.cloud
Write-Host "Generando manifiesto web de descargas releases.json para valcore.cloud..." -ForegroundColor Cyan
$ReleasesJson = @"
{
  "version": "$Version",
  "updated_at": "$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')",
  "downloads": {
    "x64": "Ticketfy-Setup-v$Version-x64.exe",
    "x86": "Ticketfy-Setup-v$Version-x86.exe",
    "default": "Ticketfy-Setup-v$Version.exe"
  }
}
"@
Set-Content -Path "$ReleaseDir\releases.json" -Value $ReleasesJson -Encoding UTF8

# Copiar y actualizar scripts detectores web
if (Test-Path "..\web") {
    Copy-Item -Path "..\web\*" -Destination "$ReleaseDir\" -Force
    if (Test-Path "$ReleaseDir\valcore-download.js") {
        $jsContent = Get-Content "$ReleaseDir\valcore-download.js" -Raw
        $jsContent = $jsContent -replace "const DEFAULT_VERSION = '[^']+'", "const DEFAULT_VERSION = '$Version'"
        Set-Content -Path "$ReleaseDir\valcore-download.js" -Value $jsContent -Encoding UTF8
    }
}

Write-Host "Publicando releases en Forgejo (https://git.valcore/yersi/ticketfy-releases.git)..." -ForegroundColor Cyan
$CurrentLocation = Get-Location
Set-Location -Path $ReleaseDir
if (!(Test-Path ".git")) {
    git init
    git branch -M main
    git remote add origin https://yersi:valcore1712-@git.valcore/yersi/ticketfy-releases.git
}

git config http.sslVerify false
git add .
git commit -m "Release v$Version"
git push -u origin main --force

Set-Location -Path $CurrentLocation

Write-Host "¡Construcción y publicación de actualización OTA completadas con éxito!" -ForegroundColor Green
Write-Host "Instalador 64-bit: $ReleaseDir\Ticketfy-Setup-v$Version-x64.exe" -ForegroundColor Green
Write-Host "Instalador 32-bit: $ReleaseDir\Ticketfy-Setup-v$Version-x86.exe" -ForegroundColor Green
Write-Host "Manifiesto Web:    $ReleaseDir\releases.json" -ForegroundColor Green

# 5. Limpieza de temporales
Write-Host "Limpiando artefactos temporales..." -ForegroundColor Cyan
Remove-Item -Recurse -Force ".\bin" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ".\obj" -ErrorAction SilentlyContinue
Write-Host "Proceso finalizado con éxito." -ForegroundColor Green
