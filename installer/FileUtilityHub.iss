#define AppName "File Utility Hub"
#ifndef AppVersion
#define AppVersion "1.0.0"
#endif
#define AppPublisher "File Utility Hub"
#define AppExeName "FileUtilityHub.WinUI.exe"

[Setup]
AppId={{E8486889-1AD0-4ED2-A5C4-1DC0EF0CE9CD}
AppName={#AppName}
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\FileUtilityHub
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=FileUtilityHub-Setup-v{#AppVersion}-win-x64
SetupIconFile=..\src\FileUtilityHub.WinUI\Assets\AppIcon.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Tạo biểu tượng ngoài màn hình"; GroupDescription: "Biểu tượng bổ sung:"; Flags: unchecked

[Files]
Source: "..\artifacts\release\win-x64\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Mở {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet8Installed: Boolean;
var
  Version: String;
begin
  Result :=
    RegQueryStringValue(
      HKLM64,
      'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost',
      'Version',
      Version
    ) and (Pos('8.', Version) = 1);
end;

function InitializeSetup: Boolean;
begin
  Result := IsDotNet8Installed;
  if not Result then
    MsgBox(
      'Máy chưa có .NET 8 x64. Vui lòng cài .NET 8 trước khi cài File Utility Hub.',
      mbError,
      MB_OK
    );
end;
