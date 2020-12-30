using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CliWrap;
using CliWrap.Buffered;
using WireGuardServerForWindows.Properties;
using SpecialFolder = System.Environment.SpecialFolder;

namespace WireGuardServerForWindows.Models
{
    public class InternetSharingPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public InternetSharingPrerequisite() : base
        (
            title: Resources.InternetSharingTitle,
            successMessage: Resources.InternetSharingSuccess,
            errorMessage: Resources.InternetSharingError,
            resolveText: Resources.InternetSharingResolve,
            configureText: Resources.InternetSharingConfigure
        )
        {
        }

        public override bool Fulfilled => _state;
        private bool _state = false;

        public override void Resolve()
        {
            if (Directory.Exists(Path.GetDirectoryName(ScriptPath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScriptPath));
            }

            if (File.Exists(ScriptPath) == false)
            {
                File.WriteAllText(ScriptPath, ScriptText);
            }

            Configure();
        }

        public override void Configure()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var _ = Cli.Wrap("powershell")
                .WithArguments(a => a
                    // Import the script
                    .Add("import-module").Add(ScriptPath).Add(";")
                    // Execute the function
                    .Add("set-netconnectionsharing").Add(Path.GetFileNameWithoutExtension(ServerConfigurationPrerequisite.ServerWGPath)).Add($"${_state}"))
                .WithValidation(CommandResultValidation.None);

                var a = _.ToString();
                ;
                var result = _.ExecuteBufferedAsync().Task.Result;

            if (string.IsNullOrEmpty(result.StandardError) == false)
            {
                MessageBox.Show(result.StandardError);
            }
            else
            {
                _state = !_state;
                RaisePropertyChanged(nameof(Fulfilled));
            }

            Mouse.OverrideCursor = null;
        }

        #endregion

        #region Private static fields

        private static string ScriptPath { get; } = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "WS4W", "scripts", "wireguard.ps1");

        private static string ScriptText { get; } = @"Function Set-NetConnectionSharing
{
    Param
    (
        [Parameter(Mandatory=$true)]
        [string]
        $LocalConnection,

        [Parameter(Mandatory=$true)]
        [bool]
        $Enabled        
    )

    Begin
    {
        $netShare = $null

        try
        {
            # Create a NetSharingManager object
            $netShare = New-Object -ComObject HNetCfg.HNetShare
        }
        catch
        {
            # Register the HNetCfg library (once)
            regsvr32 /s hnetcfg.dll

            # Create a NetSharingManager object
            $netShare = New-Object -ComObject HNetCfg.HNetShare
        }
    }

    Process
    {
		#Clear Existing Share	       
		$oldConnections = $netShare.EnumEveryConnection |? { $netShare.INetSharingConfigurationForINetConnection.Invoke($_).SharingEnabled -eq $true}           
		foreach($oldShared in $oldConnections)
        {
            $oldConfig = $netShare.INetSharingConfigurationForINetConnection.Invoke($oldShared)
            $oldConfig.DisableSharing()                        
        }        
	
        # Find connections
        $InternetConnection = Get-NetRoute | ? DestinationPrefix -eq '0.0.0.0/0' | Get-NetIPInterface | Where ConnectionState -eq 'Connected'        
        $publicConnection = $netShare.EnumEveryConnection |? { $netShare.NetConnectionProps.Invoke($_).Name -eq $InternetConnection.InterfaceAlias }
        $privateConnection = $netShare.EnumEveryConnection |? { $netShare.NetConnectionProps.Invoke($_).Name -eq $LocalConnection }

        # Get sharing configuration
        $publicConfig = $netShare.INetSharingConfigurationForINetConnection.Invoke($publicConnection)
        $privateConfig = $netShare.INetSharingConfigurationForINetConnection.Invoke($privateConnection)

        if ($Enabled)
        { 			
            $publicConfig.EnableSharing(0)
            $privateConfig.EnableSharing(1)
        }
        else
        {
            $publicConfig.DisableSharing()
            $privateConfig.DisableSharing()
        }
    }
}";

        #endregion
    }
}
