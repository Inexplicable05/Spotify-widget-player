#define AppVersion "1.2.0"
#define RepoURL "https://github.com/Inexplicable05/Spotify-widget-player"

[Setup]
AppId=SpotiFloat
AppName=SpotiFloat
AppVersion={#AppVersion}
AppPublisher=Inexplicable05
AppPublisherURL={#RepoURL}
AppSupportURL={#RepoURL}/issues
AppUpdatesURL={#RepoURL}/releases
DefaultDirName={localappdata}\Programs\SpotiFloat
DefaultGroupName=SpotiFloat
PrivilegesRequired=lowest
OutputBaseFilename=SpotiFloat-Setup-{#AppVersion}-win-x64
SetupIconFile=..\icon.ico
UninstallDisplayIcon={app}\SpotiFloat.exe
UninstallDisplayName=SpotiFloat
Compression=lzma2
SolidCompression=yes
LicenseFile=..\..\LICENSE
InfoBeforeFile=InfoBefore.txt
MinVersion=10.0.19041
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\SpotiFloat"; Filename: "{app}\SpotiFloat.exe"
Name: "{autodesktop}\SpotiFloat"; Filename: "{app}\SpotiFloat.exe"; Tasks: desktopicon

[Registry]
; Settings owns startup opt-in; uninstall removes its entry but keeps preferences.
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "SpotiFloat"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\SpotiFloat.exe"; Description: "{cm:LaunchProgram,SpotiFloat}"; Flags: nowait postinstall skipifsilent
