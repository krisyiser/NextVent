import os
import subprocess
import sys

sys.stdout.reconfigure(encoding='utf-8')

# Check if oscdimg.exe or IMAPI2 is available
ps_script = """
param($SourceDir, $IsoPath)

# Use IMAPI2 via PowerShell COM Object to build ISO
$image = New-Object -ComObject IMAPI2FS.MsftFileSystemImage
$image.ChooseImageDefaultsForMediaType(1) # 1 = CD, 2 = DVD
$image.FileSystemsToCreate = 3 # ISO9660 + Joliet

$dir = $image.Root
$files = Get-ChildItem -Path $SourceDir
foreach ($f in $files) {
    $dir.AddTree($f.FullName, $false)
}

$result = $image.CreateResultImage()
$stream = $result.ImageStream

$fileStream = [System.IO.File]::Create($IsoPath)
$buffer = New-Object byte[] 8192

while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
    $fileStream.Write($buffer, 0, $read)
}

$fileStream.Close()
Write-Host "ISO Creado exitosamente en: $IsoPath"
"""

with open("create_iso.ps1", "w", encoding="utf-8") as f:
    f.write(ps_script)

print("Script create_iso.ps1 listo.")
