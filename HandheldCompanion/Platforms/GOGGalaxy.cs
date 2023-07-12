using ControllerCommon.Platforms;
using ControllerCommon.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace HandheldCompanion.Platforms
{
    public class GOGGalaxy : IPlatform
    {
        public GOGGalaxy()
        {
            Name = "GOG Galaxy";
            ExecutableName = "GalaxyClient.exe";

            // store specific modules
            Modules = new List<string>()
            {
                "Galaxy.dll",
                "GalaxyClient.exe",
                "GalaxyClientService.exe",
            };

            // check if platform is installed
            InstallPath = RegistryUtils.GetString(@"SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths", "client");
            if (Path.Exists(InstallPath))
            {
                // update paths
                SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"GOG.com\Galaxy\Configuration\config.json");
                ExecutablePath = Path.Combine(InstallPath, ExecutableName);

                // check executable
                IsInstalled = File.Exists(ExecutablePath);
            }

            base.PlatformType = PlatformType.GOG;
        }
    }
}