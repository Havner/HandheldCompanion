using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.System.Diagnostics;
using Point = System.Windows.Point;

namespace ControllerCommon.Utils
{
    public static class ProcessUtils
    {
        #region enums
        public enum BinaryType : uint
        {
            SCS_32BIT_BINARY = 0,   // A 32-bit Windows-based application
            SCS_64BIT_BINARY = 6,   // A 64-bit Windows-based application.
            SCS_DOS_BINARY = 1,     // An MS-DOS based application
            SCS_OS216_BINARY = 5,   // A 16-bit OS/2-based application
            SCS_PIF_BINARY = 3,     // A PIF file that executes an MS-DOS based application
            SCS_POSIX_BINARY = 4,   // A POSIX based application
            SCS_WOW_BINARY = 2      // A 16-bit Windows-based application
        }
        #endregion

        #region imports
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern bool SetForegroundWindow(IntPtr handle);

        [DllImport("User32.dll")]
        public static extern bool ShowWindow(IntPtr handle, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("User32.dll")]
        public static extern bool IsIconic(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName(
            IntPtr hProcess,
            int dwFlags,
            System.Text.StringBuilder lpExeName,
            ref int lpdwSize);

        private const int QueryLimitedInformation = 0x00001000;

        [ComImport, Guid("4ce576fa-83dc-4F88-951c-9d0782b4e376")]
        private class UIHostNoLaunch
        {
        }

        [ComImport, Guid("37c994e7-432b-4834-a2f7-dce1f13b834b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITipInvocation
        {
            void Toggle(IntPtr hwnd);
        }

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();
        #endregion

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public Rectangle rcNormalPosition;
        }

        public enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
            Restored = 9
        }

        public class FindHostedProcess
        {
            // Speical handling needs for UWP to get the child window process
            private const string UWPFrameHostApp = "ApplicationFrameHost.exe";

            public ProcessDiagnosticInfo _realProcess { get; private set; }
            private byte UWPattempt;

            public FindHostedProcess(IntPtr foregroundProcessID)
            {
                try
                {
                    if (foregroundProcessID == IntPtr.Zero)
                        return;

                    _realProcess = ProcessDiagnosticInfo.TryGetForProcessId((uint)WinAPI.GetWindowProcessId(foregroundProcessID));

                    if (_realProcess is null)
                        return;

                    // Get real process
                    while (_realProcess.ExecutableFileName == UWPFrameHostApp && UWPattempt < 10)
                    {
                        EnumChildWindows(foregroundProcessID, ChildWindowCallback, IntPtr.Zero);
                        UWPattempt++;
                        Thread.Sleep(250);
                    }
                }
                catch
                {
                    _realProcess = null;
                }
            }

            private bool ChildWindowCallback(IntPtr hwnd, IntPtr lparam)
            {
                ProcessDiagnosticInfo childProcess = ProcessDiagnosticInfo.TryGetForProcessId((uint)WinAPI.GetWindowProcessId(hwnd));

                if (childProcess.ExecutableFileName != UWPFrameHostApp)
                    _realProcess = childProcess;

                return true;
            }
        }

        public static string GetWindowTitle(IntPtr handle)
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        public static Dictionary<string, string> GetAppProperties(string filePath1)
        {
            Dictionary<string, string> AppProperties = new();

            ShellObject shellFile = ShellObject.FromParsingName(filePath1);
            foreach (var property in typeof(ShellProperties.PropertySystem).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                IShellProperty shellProperty = property.GetValue(shellFile.Properties.System, null) as IShellProperty;
                if (shellProperty?.ValueAsObject is null) continue;
                if (AppProperties.ContainsKey(property.Name)) continue;

                string[] shellPropertyValues = shellProperty.ValueAsObject as string[];

                if (shellPropertyValues is not null && shellPropertyValues.Length > 0)
                {
                    foreach (string shellPropertyValue in shellPropertyValues)
                        AppProperties[property.Name] = shellPropertyValue.ToString();
                }
                else
                    AppProperties[property.Name] = shellProperty.ValueAsObject.ToString();
            }

            return AppProperties;
        }

        public static string GetPathToApp(int pid)
        {
            var size = 1024;
            var sb = new System.Text.StringBuilder(size);
            var handle = OpenProcess(QueryLimitedInformation, false, pid);
            if (handle == IntPtr.Zero) return null;
            var success = QueryFullProcessImageName(handle, 0, sb, ref size);
            CloseHandle(handle);
            if (!success) return null;
            return sb.ToString();
        }

        public static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        public static void ShowOnScreenKeyboard()
        {
            var uiHostNoLaunch = new UIHostNoLaunch();
            var tipInvocation = (ITipInvocation)uiHostNoLaunch;
            tipInvocation.Toggle(GetDesktopWindow());
            Marshal.ReleaseComObject(uiHostNoLaunch);
        }
    }

    public static class IconUtilities
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static ImageSource ToImageSource(this Icon icon)
        {
            Bitmap bitmap = icon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap))
            {
                throw new Win32Exception();
            }

            return wpfBitmap;
        }
    }
}
