# This script is intended to be run from the root of the repo, like .\Installer\GenerateRelease.ps1
# This script is intended to be run after the "Bump version..." commit has already been pushed.
# Requires that "C:\Program Files (x86)\Inno Setup 6" be in the PATH for iscc.

foreach ($configuration in "Debug", "Release") {
    if (Test-Path -Path WireGuardServerForWindows\bin\$configuration) {
        Remove-Item -Recurse WireGuardServerForWindows\bin\$configuration
    }
}

msbuild WireGuardServerForWindows.sln /property:Configuration=Release

Remove-Item Installer\WS4WSetup-*.exe

iscc Installer\WS4WSetupScript.iss

Start-Process Installer
