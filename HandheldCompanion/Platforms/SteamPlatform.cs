using HandheldCompanion.Managers;
using HandheldCompanion.Properties;
using HandheldCompanion.Utils;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace HandheldCompanion.Platforms
{
    public class SteamPlatform : IPlatform
    {
        private string RunningName;

        public static readonly Dictionary<string, byte[]> ControllerFiles = new()
        {
                { @"controller_base\chord_neptune.vdf", Resources.chord_neptune },
                { @"controller_base\chord_neptune_external.vdf", Resources.chord_neptune },
        };

        public SteamPlatform()
        {
            base.PlatformType = PlatformType.Steam;

            Name = "Steam";
            ExecutableName = "steam.exe";

            // this is for detecting steam start/stop, for some reason steam.exe often doesn't work
            RunningName = "steamwebhelper.exe";

            // store specific modules
            Modules = new List<string>()
            {
                "steam.exe",
                "steamwebhelper.exe",
                "gameoverlayrenderer.dll",
                "gameoverlayrenderer64.dll",
                "steamclient.dll",
                "steamclient64.dll",
            };

            // check if platform is installed
            InstallPath = RegistryUtils.GetString(@"SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath");
            if (Path.Exists(InstallPath))
            {
                // update paths
                SettingsPath = Path.Combine(InstallPath, @"config\config.vdf");
                ExecutablePath = Path.Combine(InstallPath, ExecutableName);

                // check executable
                IsInstalled = File.Exists(ExecutablePath);
            }
        }

        public void Start()
        {
            ProcessManager.ProcessStarted += ProcessManager_ProcessStarted;
            ProcessManager.ProcessStopped += ProcessManager_ProcessStopped;
        }

        public void Stop()
        {
            ProcessManager.ProcessStarted -= ProcessManager_ProcessStarted;
            ProcessManager.ProcessStopped -= ProcessManager_ProcessStopped;

            // restore files even if Steam is still running
            RestoreFiles();
        }

        private void ReplaceFiles()
        {
            // overwrite controller files
            foreach (var config in ControllerFiles)
                OverwriteFile(config.Key, config.Value, true);
        }

        private void RestoreFiles()
        {
            // restore controller files
            foreach (var config in ControllerFiles)
                ResetFile(config.Key);
        }

        private void ProcessManager_ProcessStarted(ProcessEx processEx, bool OnStartup)
        {
            if (!OnStartup && processEx.Executable == RunningName)
            {
                // UI thread (async)
                Application.Current.Dispatcher.BeginInvoke(async () =>
                {
                    LogManager.LogDebug("Steam started, replacing files in 3 seconds");
                    await Task.Delay(3000);
                    ReplaceFiles();
                });
            }
        }

        private void ProcessManager_ProcessStopped(ProcessEx processEx)
        {
            if (processEx.Executable == RunningName)
            {
                LogManager.LogDebug("Steam stopped, restoring files");
                RestoreFiles();
            }
        }
    }
}
