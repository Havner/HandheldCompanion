using HandheldCompanion.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Windows.System.Diagnostics;
using static HandheldCompanion.Managers.ProcessEx;
using static HandheldCompanion.Misc.WinAPI;
using Timer = System.Timers.Timer;

namespace HandheldCompanion.Managers
{
    public static class ProcessManager
    {
        public static event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();
        public static event ForegroundChangedEventHandler ForegroundChanged;
        public delegate void ForegroundChangedEventHandler(ProcessEx processEx);
        public static event ProcessStartedEventHandler ProcessStarted;
        public delegate void ProcessStartedEventHandler(ProcessEx processEx, bool OnStartup);
        public static event ProcessStoppedEventHandler ProcessStopped;
        public delegate void ProcessStoppedEventHandler(ProcessEx processEx);

        // process vars
        private static Timer ForegroundTimer;
        private static ConcurrentDictionary<int, ProcessEx> Processes = new();
        private static ProcessEx? foregroundProcess;
        private static bool IsInitialized;

        static ProcessManager()
        {
            ForegroundTimer = new Timer(1000);
            ForegroundTimer.Elapsed += ForegroundCallback;
        }

        public static void Start()
        {
            // start processes monitor
            ForegroundTimer.Start();

            IsInitialized = true;
            Initialized?.Invoke();

            LogManager.LogInformation("{0} has started", "ProcessManager");
        }

        public static void Stop()
        {
            if (!IsInitialized)
                return;

            IsInitialized = false;

            // stop processes monitor
            ForegroundTimer.Elapsed -= ForegroundCallback;
            ForegroundTimer.Stop();

            LogManager.LogInformation("{0} has stopped", "ProcessManager");
        }

        public static ProcessEx? GetForegroundProcess()
        {
            return foregroundProcess;
        }

        private static void ForegroundCallback(object? sender, EventArgs e)
        {
            IntPtr hWnd = GetforegroundWindow();

            ProcessDiagnosticInfo processInfo = new ProcessUtils.FindHostedProcess(hWnd)._realProcess;
            if (processInfo is null)
                return;

            try
            {
                int processId = (int)processInfo.ProcessId;

                if (!Processes.TryGetValue(processId, out ProcessEx process))
                {
                    if (!ProcessCreated(processId, (int)hWnd))
                        return;
                    process = Processes[processId];
                }

                ProcessEx prevProcess = foregroundProcess;

                // filter based on current process status
                ProcessFilter filter = GetFilter(process.Executable, process.Path, ProcessUtils.GetWindowTitle(hWnd));
                switch (filter)
                {
                    // do nothing on QuickTools window, current process is kept
                    case ProcessFilter.HandheldCompanion:
                        return;
                    // foreground of those processes is ignored, they fallback to default
                    case ProcessFilter.Desktop:
                        foregroundProcess = null;
                        break;
                    // update foreground process
                    default:
                        foregroundProcess = process;
                        foregroundProcess.MainWindowHandle = hWnd;
                        break;
                }

                // nothing's changed
                if (foregroundProcess == prevProcess)
                    return;

                if (foregroundProcess is not null)
                    LogManager.LogDebug("{0} process {1} now has the foreground", foregroundProcess.Platform, foregroundProcess.Executable);
                else
                    LogManager.LogDebug("No current foreground process or it is ignored");

                // raise event
                ForegroundChanged?.Invoke(foregroundProcess);
            }
            catch
            {
                // process has too high elevation
                return;
            }
        }

        private static void ProcessHalted(object? sender, EventArgs e)
        {
            int processId = ((Process)sender).Id;

            if (!Processes.TryGetValue(processId, out ProcessEx processEx))
                return;

            // stopped process can't have foreground
            if (foregroundProcess == processEx)
            {
                LogManager.LogDebug("{0} process {1} that had foreground has halted", foregroundProcess.Platform, foregroundProcess.Executable);
                foregroundProcess = null;
                ForegroundChanged?.Invoke(foregroundProcess);
            }

            Processes.TryRemove(new KeyValuePair<int, ProcessEx>(processId, processEx));

            // raise event
            ProcessStopped?.Invoke(processEx);

            LogManager.LogDebug("Process halted: {0}", processEx.Executable);

            processEx.Dispose();
        }

        private static bool ProcessCreated(int ProcessID, int NativeWindowHandle = 0, bool OnStartup = false)
        {
            try
            {
                // process has exited on arrival
                Process proc = Process.GetProcessById(ProcessID);
                if (proc.HasExited)
                    return false;

                // hook into events
                proc.EnableRaisingEvents = true;

                if (Processes.ContainsKey(proc.Id))
                    return true;

                // check process path
                string path = ProcessUtils.GetPathToApp(proc.Id);
                if (string.IsNullOrEmpty(path))
                    return false;

                string exec = Path.GetFileName(path);
                IntPtr hWnd = NativeWindowHandle != 0 ? NativeWindowHandle : proc.MainWindowHandle;

                // get filter
                ProcessFilter filter = GetFilter(exec, path);

                ProcessEx processEx = new ProcessEx(proc, path, exec, filter);
                processEx.MainWindowHandle = hWnd;

                Processes.TryAdd(processEx.GetProcessId(), processEx);
                proc.Exited += ProcessHalted;

                if (processEx.Filter != ProcessFilter.Allowed)
                    return true;

                // raise event
                ProcessStarted?.Invoke(processEx, OnStartup);

                LogManager.LogDebug("Process detected: {0}", processEx.Executable);

                return true;
            }
            catch
            {
                // process has too high elevation
            }

            return false;
        }

        private static ProcessFilter GetFilter(string exec, string path, string MainWindowTitle = "")
        {
            if (string.IsNullOrEmpty(path))
                return ProcessFilter.Restricted;

            // manual filtering
            switch (exec.ToLower())
            {
                // handheld companion
                case "handheldcompanion.exe":
                    {
                        if (!string.IsNullOrEmpty(MainWindowTitle))
                        {
                            switch (MainWindowTitle)
                            {
                                case "QuickTools":
                                    return ProcessFilter.HandheldCompanion;
                            }
                        }

                        return ProcessFilter.Restricted;
                    }

                case "rw.exe":                  // Used to change TDP
                case "kx.exe":                  // Used to change TDP
                case "devenv.exe":              // Visual Studio
                case "msedge.exe":              // Edge has energy awareness
                case "webviewhost.exe":
                case "taskmgr.exe":
                case "procmon.exe":
                case "procmon64.exe":
                case "widgets.exe":

                // System shell
                case "dwm.exe":
                case "sihost.exe":
                case "fontdrvhost.exe":
                case "chsime.exe":
                case "ctfmon.exe":
                case "csrss.exe":
                case "smss.exe":
                case "svchost.exe":
                case "wudfrd.exe":

                // Other
                case "bdagent.exe":             // Bitdefender Agent
                case "monotificationux.exe":

                // Controller service
                case "controllerservice.exe":
                case "controllerservice.dll":
                    return ProcessFilter.Restricted;

                // Desktop
                case "radeonsoftware.exe":
                case "applicationframehost.exe":
                case "shellexperiencehost.exe":
                case "startmenuexperiencehost.exe":
                case "searchhost.exe":
                case "explorer.exe":
                case "hwinfo64.exe":
                case "searchapp.exe":
                case "logioverlay.exe":
                case "gog galaxy notifications renderer.exe":
                    return ProcessFilter.Desktop;

                default:
                    return ProcessFilter.Allowed;
            }
        }
    }
}
