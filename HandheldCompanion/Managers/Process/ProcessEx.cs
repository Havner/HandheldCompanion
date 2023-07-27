using HandheldCompanion.Platforms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace HandheldCompanion.Managers
{
    public partial class ProcessEx : IDisposable
    {
        public enum ProcessFilter
        {
            Allowed = 0,
            Restricted = 1,
            HandheldCompanion = 2,
            Desktop = 3,
        }

        public Process Process;

        public IntPtr MainWindowHandle;

        public Icon? icon;
        public string Path;
        public string Executable;

        public ProcessFilter Filter;
        public PlatformType Platform { get; set; }

        public ProcessEx()
        {
        }

        // foreground detection thread
        public ProcessEx(Process process, string path, string executable, ProcessFilter filter) : this()
        {
            Process = process;
            Path = path;

            Executable = executable;

            Filter = filter;
            Platform = PlatformManager.GetPlatform(Process);

            if (File.Exists(Path))
                icon = Icon.ExtractAssociatedIcon(Path);
        }

        public int GetProcessId()
        {
            try
            {
                if (Process is not null)
                    return Process.Id;
            }
            catch { }
            return 0;
        }

        public void Dispose()
        {
            if (Process is not null)
                Process.Dispose();

            GC.SuppressFinalize(this); //now, the finalizer won't be called
        }
    }
}
