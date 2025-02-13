using HandheldCompanion.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace HandheldCompanion.Platforms
{
    public enum PlatformType
    {
        Windows = 0,
        Steam = 1,
        Origin = 2,
        UbisoftConnect = 3,
        GOG = 4,
    }

    public abstract class IPlatform
    {
        protected string Name;
        protected string ExecutableName;

        protected string InstallPath;
        protected string SettingsPath;
        protected string ExecutablePath;

        protected Process Process
        {
            get
            {
                try
                {
                    var processes = Process.GetProcessesByName(Name);
                    if (processes.Length == 0)
                        return null;

                    var process = processes.FirstOrDefault();
                    if (process.HasExited)
                        return null;

                    process.EnableRaisingEvents = true;

                    return process;
                }
                catch
                {
                    return null;
                }
            }
        }

        public bool IsInstalled;
        public bool HasModules
        {
            get
            {
                foreach (var file in Modules)
                {
                    var filename = Path.Combine(InstallPath, file);
                    if (File.Exists(filename))
                        continue;
                    else
                        return false;
                }

                return true;
            }
        }

        public PlatformType PlatformType;

        protected List<string> Modules = new();

        public string GetName()
        {
            return Name;
        }

        public string GetInstallPath()
        {
            return InstallPath;
        }

        public string GetSettingsPath()
        {
            return SettingsPath;
        }

        public virtual string GetSetting(string key)
        {
            return string.Empty;
        }

        public virtual bool IsRelated(Process proc)
        {
            try
            {
                foreach (ProcessModule module in proc.Modules)
                    if (Modules.Contains(module.ModuleName))
                        return true;
            }
            catch (Win32Exception)
            {
            }
            catch (InvalidOperationException)
            {
            }

            return false;
        }

        public virtual bool IsRunning()
        {
            return Process is not null;
        }

        public bool IsFileOverwritten(string FilePath, byte[] content)
        {
            try
            {
                var configPath = Path.Combine(InstallPath, FilePath);
                if (!File.Exists(configPath))
                    return false;

                byte[] diskContent = File.ReadAllBytes(configPath);
                return content.SequenceEqual(diskContent);
            }
            catch (DirectoryNotFoundException)
            {
                // Steam was installed, but got removed
                return false;
            }
            catch (IOException)
            {
                LogManager.LogError("Couldn't locate {0} configuration file", this.PlatformType);
                return false;
            }
        }

        public bool ResetFile(string FilePath)
        {
            try
            {
                var configPath = Path.Combine(InstallPath, FilePath);
                if (!File.Exists(configPath))
                    return false;

                var origPath = $"{configPath}.orig";
                if (!File.Exists(origPath))
                    return false;

                File.Move(origPath, configPath, true);
                return true;
            }
            catch (FileNotFoundException)
            {
                // File was not found (which is valid as it might be before first start of the application)
                LogManager.LogError("Couldn't locate {0} configuration file", this.PlatformType);
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                // Steam was installed, but got removed
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (System.Security.SecurityException)
            {
                return false;
            }
            catch (IOException)
            {
                LogManager.LogError("Failed to overwrite {0} configuration file", this.PlatformType);
                return false;
            }
        }

        public bool OverwriteFile(string FilePath, byte[] content, bool backup)
        {
            try
            {
                var configPath = Path.Combine(InstallPath, FilePath);
                if (!File.Exists(configPath))
                    return false;

                // file has already been overwritten
                if (IsFileOverwritten(FilePath, content))
                    return false;

                if (backup)
                {
                    var origPath = $"{configPath}.orig";
                    File.Copy(configPath, origPath, true);
                }

                File.WriteAllBytes(configPath, content);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (System.Security.SecurityException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                // Steam was installed, but got removed
                return false;
            }
            catch (IOException)
            {
                LogManager.LogError("Failed to overwrite {0} configuration file", this.PlatformType);
                return false;
            }
        }
    }
}
