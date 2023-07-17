using HandheldCompanion.Platforms;
using HandheldCompanion.Utils;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Media;

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

        public ImageSource imgSource;
        public string Path;
        public string Executable;

        public ProcessFilter Filter;
        public PlatformType Platform { get; set; }

        public ProcessEx()
        {
        }

        public ProcessEx(Process process, string path, string executable, ProcessFilter filter) : this()
        {
            Process = process;
            Path = path;

            Executable = executable;

            Filter = filter;
            Platform = PlatformManager.GetPlatform(Process);

            if (File.Exists(Path))
            {
                var icon = Icon.ExtractAssociatedIcon(Path);
                if (icon is not null)
                    imgSource = icon.ToImageSource();
            }
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
