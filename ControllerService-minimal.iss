[Setup]
#define MyAppSetupName 'Handheld Companion'
#define MyBuildId 'HandheldCompanion'
#define MyAppVersion '0.15.1.1'
#define MyAppPublisher 'BenjaminLSR'
#define MyAppCopyright 'Copyright © BenjaminLSR'
#define MyAppURL 'https://github.com/Valkirie/HandheldCompanion'
#define MyAppExeName "HandheldCompanion.exe"
#define MySerExeName "ControllerService.exe"
#define MyConfiguration "Release"

#define MyConfigurationExt "net7.0"

AppName={#MyAppSetupName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppSetupName}
AppCopyright={#MyAppCopyright}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
OutputBaseFilename={#MyBuildId}-{#MyAppVersion}-minimal
DefaultGroupName={#MyAppSetupName}
DefaultDirName={autopf}\{#MyAppSetupName}
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile="{#SourcePath}\HandheldCompanion\Resources\icon.ico"
SourceDir=redist
OutputDir={#SourcePath}\install
AllowNoIcons=yes

MinVersion=7.0
PrivilegesRequired=admin

// remove next line if you only deploy 32-bit binaries and dependencies
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: en; MessagesFile: "compiler:Default.isl"

[Setup]
AlwaysRestart = no
CloseApplications = yes

[Files]
Source: "{#SourcePath}\bin\{#MyConfiguration}\{#MyConfigurationExt}-windows10.0.19041.0\WinRing0x64.dll"; DestDir: "{app}"; Flags: onlyifdoesntexist
Source: "{#SourcePath}\bin\{#MyConfiguration}\{#MyConfigurationExt}-windows10.0.19041.0\WinRing0x64.sys"; DestDir: "{app}"; Flags: onlyifdoesntexist
Source: "{#SourcePath}\bin\{#MyConfiguration}\{#MyConfigurationExt}-windows10.0.19041.0\*"; Excludes: "*WinRing0x64.*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

Source: "{#SourcePath}\redist\Segoe Fluent Icons.ttf"; DestDir: "{autofonts}"; FontInstall: "Segoe Fluent Icons"; Flags: onlyifdoesntexist uninsneveruninstall
Source: "{#SourcePath}\redist\PromptFont.otf"; DestDir: "{autofonts}"; FontInstall: "PromptFont"; Flags: uninsneveruninstall

[Icons]
Name: "{group}\{#MyAppSetupName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppSetupName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppSetupName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"

[Run]

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop ControllerService" ; RunOnceId: "StopService"; Flags: runascurrentuser runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete ControllerService" ; RunOnceId: "DeleteService"; Flags: runascurrentuser runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[InstallDelete]

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\Windows Error Reporting\LocalDumps"; Flags: uninsdeletekeyifempty
Root: HKLM; Subkey: "Software\Microsoft\Windows\Windows Error Reporting\LocalDumps\ControllerService.exe"; ValueType: string; ValueName: "DumpFolder"; ValueData: "{userdocs}\HandheldCompanion\dumps"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\Windows Error Reporting\LocalDumps\HandheldCompanion.exe"; ValueType: string; ValueName: "DumpFolder"; ValueData: "{userdocs}\HandheldCompanion\dumps"; Flags: uninsdeletekey

[Code]
function InitializeSetup: Boolean;
begin
  Result := True;
end;