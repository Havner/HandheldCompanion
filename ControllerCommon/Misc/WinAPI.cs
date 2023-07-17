using System;
using System.Runtime.InteropServices;

namespace ControllerCommon.Misc
{
    public static class WinAPI
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern nint GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowThreadProcessId(
            nint hWnd,
            out int lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SetPriorityClass(nint hProcess, int dwPriorityClass);

        [Flags]
        public enum PriorityClass : uint
        {
            ABOVE_NORMAL_PRIORITY_CLASS = 0x8000,
            BELOW_NORMAL_PRIORITY_CLASS = 0x4000,
            HIGH_PRIORITY_CLASS = 0x80,
            IDLE_PRIORITY_CLASS = 0x40,
            NORMAL_PRIORITY_CLASS = 0x20,
            PROCESS_MODE_BACKGROUND_BEGIN = 0x100000,
            PROCESS_MODE_BACKGROUND_END = 0x200000,
            REALTIME_PRIORITY_CLASS = 0x100
        }

        public static int GetWindowProcessId(nint hwnd)
        {
            int pid;
            GetWindowThreadProcessId(hwnd, out pid);
            return pid;
        }

        public static nint GetforegroundWindow()
        {
            return GetForegroundWindow();
        }
    }
}
