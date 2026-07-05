Add-Type -AssemblyName System.Drawing
$sidebar = New-Object System.Drawing.Bitmap(164, 314)
$header = New-Object System.Drawing.Bitmap(150, 57)
$graphicsSidebar = [System.Drawing.Graphics]::FromImage($sidebar)
$graphicsHeader = [System.Drawing.Graphics]::FromImage($header)
$graphicsSidebar.Clear([System.Drawing.Color]::Navy)
$graphicsHeader.Clear([System.Drawing.Color]::Navy)
$sidebar.Save("c:\Users\YERSI\.gemini\antigravity-ide\scratch\NextVent\src-tauri\installer\sidebar.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$header.Save("c:\Users\YERSI\.gemini\antigravity-ide\scratch\NextVent\src-tauri\installer\header.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
