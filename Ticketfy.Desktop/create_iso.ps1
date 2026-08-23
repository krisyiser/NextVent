
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
