# Installer

## Prerequisite

Download and install [Inno Setup](https://jrsoftware.org/isinfo.php).

Download the [.NET 8.0 Desktop Runtime (v8.0.11)](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.11-windows-x64-installer) and place it in `WgServerforWindows\Installer`.

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