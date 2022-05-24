# Installer

## Prerequisite

Download and install [Inno Setup](https://jrsoftware.org/isinfo.php).

Download the [.NET Core 3.1 Desktop Runtime (v3.1.21)](https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-3.1.21-windows-x64-installer) and place it in `WgServerforWindows\Installer`.

## Generate Installer for New Version

Open the main `WgServerforWindows.sln` in Visual Studio.
* Change the build configuration to Release.
* Edit `VersionInfo2.xml` to include the latest version, release date, and download path.
* Bump assembly versions in `Directory.Build.props`.
* Rebuild the solution

> It's probably a good idea to commit at this point so that the installer is generated from committed code.

Open `WS4WSetupScript.iss` in Inno Setup.
* Bump the `MyAppVersion` preprocessor definition.
* Compile.

Create a new release on GitHub and upload the installer.