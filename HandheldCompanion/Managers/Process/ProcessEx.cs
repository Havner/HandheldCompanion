using ControllerCommon;
using ControllerCommon.Platforms;
using ControllerCommon.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace HandheldCompanion.Managers
{
    public partial class ProcessEx : IDisposable
    {
        public enum ProcessFilter
        {
            Allowed = 0,
            Restricted = 1,
            Ignored = 2,
            HandheldCompanion = 3,
            Desktop = 4,
        }

        public Process Process;
        public ProcessThread MainThread;

        public IntPtr MainWindowHandle;

        public ImageSource imgSource;
        public string Path;

        private string _Title;
        public string Title
        {
            get
            {
                return _Title;
            }

            set
            {
                _Title = value;
            }
        }

        private string _Executable;
        public string Executable
        {
            get
            {
                return _Executable;
            }

            set
            {
                _Executable = value;
            }
        }

        public ProcessFilter Filter;
        public PlatformType Platform { get; set; }

        public event MainThreadChangedEventHandler MainThreadChanged;
        public delegate void MainThreadChangedEventHandler(ProcessEx process);

        public event TitleChangedEventHandler TitleChanged;
        public delegate void TitleChangedEventHandler(ProcessEx process);

        public ProcessEx()
        {
        }

        public ProcessEx(Process process, string path, string executable, ProcessFilter filter) : this()
        {
            Process = process;
            Path = path;

            Executable = executable;
            Title = executable;    // temporary, will be overwritten by ProcessManager

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

        private static ProcessThread GetMainThread(Process process)
        {
            ProcessThread mainThread = null;
            var startTime = DateTime.MaxValue;

            try
            {
                foreach (ProcessThread thread in process.Threads)
                {
                    if (thread.StartTime < startTime)
                    {
                        startTime = thread.StartTime;
                        mainThread = thread;
                    }
                }
            }
            catch (Win32Exception)
            {
                // Access if denied
            }
            catch (InvalidOperationException)
            {
                // thread has exited
            }

            if (mainThread is null)
                mainThread = process.Threads[0];

            return mainThread;
        }

        public void Refresh()
        {
            if (Process.HasExited)
                return;

            if (MainThread is null)
            {
                // refresh main thread
                MainThread = GetMainThread(Process);

                // raise event
                MainThreadChanged?.Invoke(this);

                // prevents null mainthread from passing
                return;
            }

            string MainWindowTitle = ProcessUtils.GetWindowTitle(MainWindowHandle);

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // refresh title
                if (!string.IsNullOrEmpty(MainWindowTitle) && !MainWindowTitle.Equals(Title))
                {
                    Title = MainWindowTitle;

                    // raise event
                    TitleChanged?.Invoke(this);
                }

                switch (MainThread.ThreadState)
                {
                    case ThreadState.Terminated:
                        {
                            // dispose from MainThread
                            MainThread.Dispose();
                            MainThread = null;
                        }
                        break;
                }
            });
        }

        public void Dispose()
        {
            if (Process is not null)
                Process.Dispose();
            if (MainThread is not null)
                MainThread.Dispose();

            GC.SuppressFinalize(this); //now, the finalizer won't be called
        }
    }
}
