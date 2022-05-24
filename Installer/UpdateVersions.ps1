# This script is intended to be run from the root of the repo, like .\Installer\UpdateVersions.ps1

$newVersion = Read-Host "Enter the new version number (without 'v' and without trailing '.0')"

# Directory.Build.props
$directoryBuildPropsFile = Get-Content "Directory.Build.props"
for ($i = 0; $i -lt $directoryBuildPropsFile.Length; $i += 1) {
    $line = $directoryBuildPropsFile[$i]
    
    if ($line -match "AssemblyVersion") {
        $directoryBuildPropsFile[$i] = "        <AssemblyVersion>$($newVersion).0</AssemblyVersion>"
    }

    if ($line -match "FileVersion") {
        $directoryBuildPropsFile[$i] = "        <FileVersion>$($newVersion).0</FileVersion>"
    }

    if ($line -match "InformationalVersion") {
        $directoryBuildPropsFile[$i] = "        <InformationalVersion>$($newVersion).0</InformationalVersion>"
    }
}

Set-Content "Directory.Build.props" $directoryBuildPropsFile

# WS4WSetupScript.iss
$setupScript = Get-Content "Installer\WS4WSetupScript.iss"
for ($i = 0; $i -lt $setupScript.Length; $i += 1) {
    $line = $setupScript[$i]

    if ($line -match "#define MyAppVersion") {
        $setupScript[$i] = "#define MyAppVersion ""$($newVersion)"""
    }
}

Set-Content "Installer\WS4WSetupScript.iss" $setupScript

# VersionInfo2.xml
$versionInfo = Get-Content "WireGuardServerForWindows\VersionInfo2.xml"
for ($i = 0; $i -lt $versionInfo.Length; $i += 1) {
    $line = $versionInfo[$i]

    if ($line -match "<Version>") {
        $versionInfo[$i] = "  <Version>$($newVersion).0</Version>"
    }

    if ($line -match "ReleaseDate") {
        $versionInfo[$i] = "  <ReleaseDate>$(Get-Date -Format "yyyy-MM-dd")</ReleaseDate>"
    }

    if ($line -match "DownloadLink") {
        $versionInfo[$i] = "  <DownloadLink>https://github.com/micahmo/WgServerforWindows/releases/download/v$($newVersion)/WS4WSetup-$($newVersion).exe</DownloadLink>"
    }

    if ($line -match "DownloadFileName") {
        $versionInfo[$i] = "  <DownloadFileName>WS4WSetup-$($newVersion).exe</DownloadFileName>"
    }   
}

Set-Content "WireGuardServerForWindows\VersionInfo2.xml" $versionInfo

Write-Host -ForegroundColor Red "Don't forget to update VersionNotes!"