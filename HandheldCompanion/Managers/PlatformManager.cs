using ControllerCommon.Managers;
using ControllerCommon.Platforms;
using HandheldCompanion.Platforms;
using System.Diagnostics;

namespace HandheldCompanion.Managers
{
    public static class PlatformManager
    {
        // gaming platforms
        private static SteamPlatform Steam = new();
        private static GOGGalaxy GOGGalaxy = new();
        private static UbisoftConnect UbisoftConnect = new();

        private static bool IsInitialized;

        public static void Start()
        {
            if (Steam.IsInstalled)
            {
                Steam.Start();
            }

            if (GOGGalaxy.IsInstalled)
            {
                // do something
            }

            if (UbisoftConnect.IsInstalled)
            {
                // do something
            }

            IsInitialized = true;

            LogManager.LogInformation("{0} has started", "PlatformManager");
        }

        public static void Stop()
        {
            if (Steam.IsInstalled)
            {
                Steam.Stop();
            }

            IsInitialized = false;

            LogManager.LogInformation("{0} has stopped", "PlatformManager");
        }

        public static PlatformType GetPlatform(Process proc)
        {
            if (!IsInitialized)
                return PlatformType.Windows;

            // is this process part of a specific platform
            if (Steam.IsRelated(proc))
                return Steam.PlatformType;
            else if (GOGGalaxy.IsRelated(proc))
                return GOGGalaxy.PlatformType;
            else if (UbisoftConnect.IsRelated(proc))
                return UbisoftConnect.PlatformType;
            else
                return PlatformType.Windows;
        }
    }
}
