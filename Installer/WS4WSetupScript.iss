#define MyAppNameOld "WireGuard Server For Windows"
#define MyAppName "Wg Server for Windows"
#define MyAppVersion "2.1.1"
#define MyAppPublisher "Micah Morrison"
#define MyAppURL "https://github.com/micahmo/WgServerforWindows"
#define MyAppExeName "WgServerforWindows.exe"
#define CliName "ws4w.exe"
#define NetRuntimeMinorVersion "11"
#define NetRuntimeVersion "8.0." + NetRuntimeMinorVersion
#define NetRuntime "windowsdesktop-runtime-" + NetRuntimeVersion + "-win-x64.exe"
#define UniversalCrtKb "KB3118401"
#define BuildConfig "Release"
;#define BuildConfig "Debug"

; This is relative to SourceDir
#define RepoRoot "..\..\..\.."

[Setup]
;PrivilegesRequired=admin
AppId={{7EE6B381-7799-4674-B83C-5B07C71A5851}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\WS4W
DefaultGroupName=WS4W
AllowNoIcons=yes
; This is relative to the .iss file location
SourceDir=..\WgServerforWindows\bin\{#BuildConfig}\net80-windows\
; These are relative to SourceDir (see RepoRoot)
OutputDir={#RepoRoot}\Installer
SetupIconFile={#RepoRoot}\WgServerforWindows\Images\logo.ico
; This is an install-time path, so it must refer to something on the installed machine, like the main exe
UninstallDisplayIcon={app}\WgServerforWindows.exe
OutputBaseFilename=WS4WSetup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; .NET Desktop Runtime install can trigger this, but it doesn't actually require a restart
RestartIfNeededByRun=no

[CustomMessages]
UCrtError={#MyAppName} requires the Universal C Runtime. Please perform all outstanding Windows Updates or search for and install {#UniversalCrtKb} before installing WS4W.

[Code]
function NetRuntimeNotInstalled: Boolean;
var
  InstalledRuntimes: TArrayOfString;
  I: Integer;
  MinorVersion: String;
  MinorVersionInt: Longint;
begin
  Result := True;
  
  // Check if ANY .NET Desktop Runtime exists
  if RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App') then
  begin
    // Get all of the installed runtimes
    if RegGetValueNames(HKEY_LOCAL_MACHINE, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App', InstalledRuntimes) then
    begin
      for I := 0 to GetArrayLength(InstalledRuntimes)-1 do
      begin 
        // See if the runtime starts with 8.0.
        if WildcardMatch(InstalledRuntimes[I], '8.0.*') then
        begin
          // Get just the minor version and convert it to an int
          MinorVersion := InstalledRuntimes[I];
          Delete(MinorVersion, 1, 4);
          MinorVersionInt := StrToIntDef(MinorVersion, 0);
          
          // Check if it's at least the version we want
          if MinorVersionInt >= {#NetRuntimeMinorVersion} then
          begin
            // Finally, this system has a new enough version installed
            Result := False;
            Break;
          end
        end
      end
    end
  end
end;

// More info: https://docs.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170
function UniversalCrtInstalled: Boolean;
begin
  Result := FileExists(ExpandConstant('{sys}') + '\ucrtbase.dll');
end;

// This is a buit-in function that's called during initialization.
// We'll use it to determine whether we can proceed with the install on this system.
function InitializeSetup(): Boolean;
begin
  if not UniversalCrtInstalled then
    begin
      MsgBox(ExpandConstant('{cm:UCrtError}'), mbCriticalError, MB_OK);
      Result := False;
    end
  else
    Result := True
end;

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "setpath"; Description: "Add '{app}' to the PATH variable for CLI access."; GroupDescription: "{cm:AdditionalIcons}"

[InstallDelete]
; Manually clean up files which use the old name
Type: files; Name: "{app}\WireGuardAPI.dll"
Type: files; Name: "{app}\WireGuardAPI.pdb"
Type: files; Name: "{app}\WireGuardServerForWindows.Cli.Options.dll"
Type: files; Name: "{app}\WireGuardServerForWindows.Cli.Options.pdb"
Type: files; Name: "{app}\WireGuardServerForWindows.deps.json"
Type: files; Name: "{app}\WireGuardServerForWindows.dll"
Type: files; Name: "{app}\WireGuardServerForWindows.exe"
Type: files; Name: "{app}\WireGuardServerForWindows.pdb"
Type: files; Name: "{app}\WireGuardServerForWindows.runtimeconfig.dev.json"
Type: files; Name: "{app}\WireGuardServerForWindows.runtimeconfig.json"
; Delete old shortcuts
Type: files; Name: "{group}\{#MyAppNameOld}.lnk"
Type: files; Name: "{autodesktop}\{#MyAppNameOld}.lnk"

[Files]
; These are relative to SourceDir
Source: "*"; DestDir: "{app}"; Excludes: "de,es"; Flags: recursesubdirs;
Source: "..\..\..\..\Installer\{#NetRuntime}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NetRuntimeNotInstalled

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; .NET Desktop Runtime
Filename: "{tmp}\{#NetRuntime}"; Flags: runascurrentuser; StatusMsg: "Installing .NET Desktop Runtime..."; Check: NetRuntimeNotInstalled

; CLI in Path
Filename: "{app}\{#CliName}"; Parameters: "setpath"; Flags: runhidden nowait skipifsilent runascurrentuser; Tasks: setpath

; runascurrentuser is needed to launch as admin
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

