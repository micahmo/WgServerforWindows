# Installer

## Prerequisite

Download and install [Inno Setup](https://jrsoftware.org/isinfo.php).

## Generate Installer for New Version

Open the main WireGuardServerForWindows.sln in Visual Studio. Bump versions in the following files.
* VersionInfo.xml (todo)
* WireGuardServerForWindows.csproj
* WireGuardServerForWindows.Cli.csproj

Change the build configuration to Release. Rebuild solution.

Open WS4WSetupScript.iss in Inno Setup. Bump the `MyAppVersion` preprocessor definition. Compile.