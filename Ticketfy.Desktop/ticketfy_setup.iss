#define MyAppName "TICKETFY!"
#define MyAppVersion "3.1.17"
#define MyAppPublisher "Valcore"
#define MyAppURL "https://valcore.cloud"
#define MyAppExeName "Ticketfy.Desktop.exe"
#define MySourceDir "c:\Users\YERSI\.gemini\antigravity-ide\scratch\NextVent\Ticketfy.Desktop\bin\Release\net9.0\win-x64\publish"

[Setup]
AppId={{D8F42F99-4A9B-4C38-9B7E-7F891D817E23}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={userappdata}\Ticketfy.Desktop\current
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=Ticketfy-Setup-v3.1.17-x64
OutputDir=c:\Users\YERSI\.gemini\antigravity-ide\scratch\NextVent\Ticketfy.Desktop\Output\Releases
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no

; Metadatos Empresariales de Firma e Identidad Windows PE - VALCORE
VersionInfoCompany=Valcore
VersionInfoDescription=Valcore TICKETFY! Punto de Venta - Instalador Nivel Industrial
VersionInfoVersion=3.0.75.0
VersionInfoCopyright=Copyright © 2026 Valcore. Todos los derechos reservados.
VersionInfoProductName=Valcore TICKETFY! POS Enterprise System
VersionInfoProductVersion=3.0.75.0
VersionInfoOriginalFileName=Ticketfy-Setup-v3.0.75-x64.exe

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent



