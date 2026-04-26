[Setup]
AppName=YomogiTaskBar
AppVersion=0.1.0
AppPublisher=hamano1204
AppPublisherURL=https://github.com/hamano1204/YomogiTaskBar
AppSupportURL=https://github.com/hamano1204/YomogiTaskBar/issues
AppUpdatesURL=https://github.com/hamano1204/YomogiTaskBar/releases
DefaultDirName={autopf}\YomogiTaskBar
DefaultGroupName=YomogiTaskBar
AllowNoIcons=yes
OutputBaseFilename=YomogiTaskBar-setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\YomogiTaskBar.exe

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net9.0-windows10.0.19041.0\win-x64\publish\YomogiTaskBar.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\YomogiTaskBar"; Filename: "{app}\YomogiTaskBar.exe"
Name: "{group}\{cm:UninstallProgram,YomogiTaskBar}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\YomogiTaskBar"; Filename: "{app}\YomogiTaskBar.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\YomogiTaskBar.exe"; Description: "{cm:LaunchProgram,YomogiTaskBar}"; Flags: nowait postinstall skipifsilent
