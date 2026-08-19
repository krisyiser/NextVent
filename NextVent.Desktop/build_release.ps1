param (
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$ProjectPath = ".\NextVent.Desktop.csproj"
$PublishDir = ".\bin\Release\net9.0\win-x64\publish"
$ReleaseDir = ".\Output\Releases"

Write-Host "Compilando Ticketfy v$Version..." -ForegroundColor Cyan

# 1. Publish the .NET Application
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained -p:Version=$Version -p:AssemblyVersion=$Version.0 -p:FileVersion=$Version.0

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error en la compilacion (dotnet publish)." -ForegroundColor Red
    exit $LASTEXITCODE
}

# 2. Package with Velopack
Write-Host "Empaquetando con Velopack..." -ForegroundColor Cyan
vpk pack -u NextVent.Desktop -v $Version -p $PublishDir -o $ReleaseDir -e NextVent.Desktop.exe --packTitle "Ticketfy!"

if (Test-Path "$ReleaseDir\NextVent.Desktop-win-Setup.exe") {
    Rename-Item -Path "$ReleaseDir\NextVent.Desktop-win-Setup.exe" -NewName "Ticketfy-Setup-v$Version.exe" -Force
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al empaquetar con Velopack." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "¡Construcción completada con éxito! Los archivos OTA están en: $ReleaseDir" -ForegroundColor Green
Write-Host "Publicando releases en Forgejo (https://git.valcore/yersi/ticketfy-releases.git)..." -ForegroundColor Cyan

Set-Location -Path $ReleaseDir
if (!(Test-Path ".git")) {
    git init
    git branch -M main
    git remote add origin https://yersi:valcore1712-@git.valcore/yersi/ticketfy-releases.git
}

# Deshabilitar verificación SSL por si el contenedor Docker usa certificados autofirmados
git config http.sslVerify false

git add .
git commit -m "Release v$Version"
git push -u origin main --force

Set-Location -Path ..\..\
Write-Host "Actualización OTA publicada correctamente y disponible para descarga." -ForegroundColor Green
