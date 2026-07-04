[Setup]
AppName=SpotiFloat
AppVersion=1.0.0
AppPublisher=SpotiFloat
DefaultDirName={autopf}\SpotiFloat
DefaultGroupName=SpotiFloat
OutputBaseFilename=SpotiFloat_Setup
SetupIconFile=..\icon.ico
Compression=lzma2
SolidCompression=yes
InfoBeforeFile=InfoBefore.txt
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startup"; Description: "Launch SpotiFloat on Windows startup"; GroupDescription: "Windows Startup:"

[Files]
Source: "..\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\SpotiFloat.exe"; DestDir: "{app}"; Flags: ignoreversion
; We are deploying a single-file executable, so no other DLLs are strictly required to be listed here.

[Icons]
Name: "{group}\SpotiFloat"; Filename: "{app}\SpotiFloat.exe"
Name: "{autodesktop}\SpotiFloat"; Filename: "{app}\SpotiFloat.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SpotiFloat"; ValueData: """{app}\SpotiFloat.exe"""; Tasks: startup

[Run]
Filename: "{app}\SpotiFloat.exe"; Description: "{cm:LaunchProgram,SpotiFloat}"; Flags: nowait postinstall skipifsilent
