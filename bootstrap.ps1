# bootstrap.ps1 – Preparación del entorno y npm install
# ---------------------------------------------------
# Añade la ruta de Node.js al PATH de la sesión actual
$nodePath = 'C:\Program Files\nodejs'
if (-Not ($env:PATH -split ';' | Where-Object { $_ -eq $nodePath })) {
  $env:PATH = "$env:PATH;$nodePath"
  Write-Host "Added Node.js to PATH: $nodePath"
} else {
  Write-Host "Node.js path already in PATH"
}

# Verificar que node y npm están accesibles
try {
  $nodeVersion = node -v
  Write-Host "Node version: $nodeVersion"
} catch {
  Write-Error "Node no se encontró después de actualizar PATH. Abortando."
  exit 1
}
try {
  $npmVersion = npm -v
  Write-Host "npm version: $npmVersion"
} catch {
  Write-Error "npm no se encontró. Abortando."
  exit 1
}

# Eliminar carpeta node_modules existente (si existe)
if (Test-Path .\node_modules) {
  Write-Host "Removing existing node_modules..."
  Remove-Item -Recurse -Force .\node_modules
}

# Instalar dependencias
Write-Host "Running npm install (legacy peer deps, force)..."
npm install --legacy-peer-deps --force
if ($LASTEXITCODE -ne 0) {
  Write-Error "npm install falló (código $LASTEXITCODE)."
  exit $LASTEXITCODE
}

Write-Host "✅  Instalación completada"
