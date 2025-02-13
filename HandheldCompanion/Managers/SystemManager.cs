using HandheldCompanion.Managers.Desktop;
using HandheldCompanion.Views;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HandheldCompanion.Managers
{
    public static class SystemManager
    {

        #region imports
        public enum DMDO
        {
            DEFAULT = 0,
            D90 = 1,
            D180 = 2,
            D270 = 3
        }

        public const int CDS_UPDATEREGISTRY = 0x01;
        public const int CDS_TEST = 0x02;
        public const int DISP_CHANGE_SUCCESSFUL = 0;
        public const int DISP_CHANGE_RESTART = 1;
        public const int DISP_CHANGE_FAILED = -1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DEVMODE
        {
            public const int DM_DISPLAYFREQUENCY = 0x400000;
            public const int DM_PELSWIDTH = 0x80000;
            public const int DM_PELSHEIGHT = 0x100000;
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;

            public int dmPositionX;
            public int dmPositionY;
            public DMDO dmDisplayOrientation;
            public int dmDisplayFixedOutput;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;

            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;

            public override string ToString()
            {
                return $"{dmPelsWidth}x{dmPelsHeight}, {dmDisplayFrequency}, {dmBitsPerPel}";
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int ChangeDisplaySettings([In] ref DEVMODE lpDevMode, int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, Int32 iModeNum, ref DEVMODE lpDevMode); [Flags()]

        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x16,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DisplayDevice
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("User32.dll")]
        static extern int EnumDisplayDevices(string lpDevice, int iDevNum, ref DisplayDevice lpDisplayDevice, int dwFlags);
        #endregion

        #region events
        public static event DisplaySettingsChangedEventHandler DisplaySettingsChanged;
        public delegate void DisplaySettingsChangedEventHandler(ScreenResolution resolution);

        public static event PrimaryScreenChangedEventHandler PrimaryScreenChanged;
        public delegate void PrimaryScreenChangedEventHandler(DesktopScreen screen);

        public static event VolumeNotificationEventHandler VolumeNotification;
        public delegate void VolumeNotificationEventHandler(int volume);

        public static event BrightnessNotificationEventHandler BrightnessNotification;
        public delegate void BrightnessNotificationEventHandler(int brightness);

        public static event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();
        #endregion

        private static DesktopScreen DesktopScreen;
        private static ScreenResolution ScreenResolution;
        private static ScreenFrequency ScreenFrequency;

        private static MMDeviceEnumerator DevEnum;
        private static MMDevice multimediaDevice;
        private static MMDeviceNotificationClient notificationClient;
        private static bool VolumeSupport = false;

        private static ManagementEventWatcher EventWatcher;
        private static ManagementScope Scope;
        private static bool BrightnessSupport = false;

        private static bool FanControlSupport = false;

        private static Screen PrimaryScreen;
        public static bool IsInitialized;

        private class MMDeviceNotificationClient : IMMNotificationClient
        {
            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                SetDefaultAudioEndPoint();
            }

            public void OnDeviceAdded(string deviceId) { }
            public void OnDeviceRemoved(string deviceId) { }
            public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
            public void OnPropertyValueChanged(string deviceId, PropertyKey key) { }
        }

        static SystemManager()
        {
            // setup the multimedia device and get current volume value
            notificationClient = new MMDeviceNotificationClient();
            DevEnum = new MMDeviceEnumerator();
            DevEnum.RegisterEndpointNotificationCallback(notificationClient);
            SetDefaultAudioEndPoint();

            // get current brightness value
            Scope = new ManagementScope(@"\\.\root\wmi");
            Scope.Connect();

            // creating the watcher
            EventWatcher = new ManagementEventWatcher(Scope, new EventQuery("Select * From WmiMonitorBrightnessEvent"));
            EventWatcher.EventArrived += new EventArrivedEventHandler(onWMIEvent);

            // start brightness watcher
            EventWatcher.Start();

            // check if we have control over brightness
            BrightnessSupport = GetBrightness() != -1;

            if (MainWindow.CurrentDevice.IsOpen && MainWindow.CurrentDevice.IsSupported)
            {
                if (MainWindow.CurrentDevice.HasFanControl())
                    FanControlSupport = true;
            }

            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;
            HotkeysManager.CommandExecuted += HotkeysManager_CommandExecuted;
        }

        private static void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            VolumeNotification?.Invoke((int)Math.Round(data.MasterVolume * 100.0f));
        }

        private static void SetDefaultAudioEndPoint()
        {
            try
            {
                if (multimediaDevice is not null && multimediaDevice.AudioEndpointVolume is not null)
                {
                    VolumeSupport = false;
                    multimediaDevice.AudioEndpointVolume.OnVolumeNotification -= AudioEndpointVolume_OnVolumeNotification;
                }

                multimediaDevice = DevEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                if (multimediaDevice is not null && multimediaDevice.AudioEndpointVolume is not null)
                {
                    VolumeSupport = true;
                    multimediaDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
                }

                // do this even when no device found, to set to 0
                VolumeNotification?.Invoke(GetVolume());
            }
            catch (Exception)
            {
                LogManager.LogError("No AudioEndpoint available");
            }
        }

        public static bool HasFanControlSupport()
        {
            return FanControlSupport;
        }

        private static void SettingsManager_SettingValueChanged(string name, object value)
        {
            switch (name)
            {
                case "FanControlEnabled":
                    if (!HasFanControlSupport())
                        return;

                    if (Convert.ToBoolean(value))
                    {
                        MainWindow.CurrentDevice.SetFanControl(true);
                        MainWindow.CurrentDevice.SetFanDuty(SettingsManager.GetUInt("FanControlValue"));
                    }
                    else if (SettingsManager.IsInitialized)
                    {
                        MainWindow.CurrentDevice.SetFanControl(false);
                    }
                    break;

                case "FanControlValue":
                    if (!HasFanControlSupport())
                        return;

                    if (SettingsManager.GetBoolean("FanControlEnabled"))
                        MainWindow.CurrentDevice.SetFanDuty(Convert.ToUInt32(value));
                    break;
            }
        }

        private static void HotkeysManager_CommandExecuted(string listener)
        {
            switch (listener)
            {
                case "increaseBrightness":
                    {
                        int stepRoundDn = (int)Math.Floor(GetBrightness() / 5.0d);
                        int brightness = stepRoundDn * 5 + 5;
                        SystemManager.SetBrightness(brightness);
                    }
                    break;
                case "decreaseBrightness":
                    {
                        int stepRoundUp = (int)Math.Ceiling(GetBrightness() / 5.0d);
                        int brightness = stepRoundUp * 5 - 5;
                        SystemManager.SetBrightness(brightness);
                    }
                    break;

                case "increaseVolume":
                    {
                        int stepRoundDn = (int)Math.Floor(GetVolume() / 5.0d);
                        int volume = stepRoundDn * 5 + 5;
                        SystemManager.SetVolume(volume);
                    }
                    break;
                case "decreaseVolume":
                    {
                        int stepRoundUp = (int)Math.Ceiling(GetVolume() / 5.0d);
                        int volume = stepRoundUp * 5 - 5;
                        SystemManager.SetVolume(volume);
                    }
                    break;

                case "FanControlEnabled":
                    {
                        bool value = !SettingsManager.GetBoolean(listener);
                        SettingsManager.SetProperty(listener, value);

                        ToastManager.SendToast("Fan control override", $"is now {(value ? "enabled" : "disabled")}");
                    }
                    break;
            }
        }

        public static void Start()
        {
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            SystemEvents_DisplaySettingsChanged(null, null);

            IsInitialized = true;
            Initialized?.Invoke();

            LogManager.LogInformation("{0} has started", "SystemManager");
        }

        private static void onWMIEvent(object sender, EventArrivedEventArgs e)
        {
            int brightness = Convert.ToInt32(e.NewEvent.Properties["Brightness"].Value);
            BrightnessNotification?.Invoke(brightness);
        }

        private static void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            if (PrimaryScreen is null || PrimaryScreen.DeviceName != Screen.PrimaryScreen.DeviceName)
            {
                // update current primary screen
                PrimaryScreen = Screen.PrimaryScreen;

                // pull resolutions details
                var resolutions = GetResolutions(PrimaryScreen.DeviceName);

                // update current desktop screen
                DesktopScreen = new DesktopScreen(PrimaryScreen.DeviceName);

                foreach (DEVMODE mode in resolutions)
                {
                    ScreenResolution res = new ScreenResolution(mode.dmPelsWidth, mode.dmPelsHeight);

                    var frequencies = resolutions.Where(a => a.dmPelsWidth == mode.dmPelsWidth && a.dmPelsHeight == mode.dmPelsHeight).Select(b => b.dmDisplayFrequency).Distinct().ToList();
                    res.AddFrequencies(frequencies);

                    // sort frequencies
                    res.SortFrequencies();

                    if (!DesktopScreen.HasResolution(res))
                        DesktopScreen.resolutions.Add(res);
                }

                // sort resolutions
                DesktopScreen.SortResolutions();

                // raise event
                PrimaryScreenChanged?.Invoke(DesktopScreen);
            }

            // pull current resolution details
            var resolution = GetResolution(PrimaryScreen.DeviceName);

            // update current desktop resolution
            ScreenResolution = DesktopScreen.GetResolution(resolution.dmPelsWidth, resolution.dmPelsHeight);
            ScreenFrequency = new ScreenFrequency(resolution.dmDisplayFrequency);

            // raise event
            DisplaySettingsChanged?.Invoke(ScreenResolution);
        }

        public static DesktopScreen GetDesktopScreen()
        {
            return DesktopScreen;
        }

        public static ScreenResolution GetScreenResolution()
        {
            return ScreenResolution;
        }

        public static ScreenFrequency GetScreenFrequency()
        {
            return ScreenFrequency;
        }

        public static void Stop()
        {
            if (!IsInitialized)
                return;

            DevEnum.UnregisterEndpointNotificationCallback(notificationClient);

            IsInitialized = false;

            LogManager.LogInformation("{0} has stopped", "SystemManager");
        }

        public static bool SetResolution(int width, int height, int displayFrequency)
        {
            if (!IsInitialized)
                return false;

            bool ret = false;
            long RetVal = 0;
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            dm.dmPelsWidth = width;
            dm.dmPelsHeight = height;
            dm.dmDisplayFrequency = displayFrequency;
            dm.dmFields = DEVMODE.DM_PELSWIDTH | DEVMODE.DM_PELSHEIGHT | DEVMODE.DM_DISPLAYFREQUENCY;
            RetVal = ChangeDisplaySettings(ref dm, CDS_TEST);
            if (RetVal == 0)
            {
                RetVal = ChangeDisplaySettings(ref dm, 0);
                ret = true;
            }
            return ret;
        }

        public static bool SetResolution(int width, int height, int displayFrequency, int bitsPerPel)
        {
            if (!IsInitialized)
                return false;

            bool ret = false;
            long RetVal = 0;
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            dm.dmPelsWidth = width;
            dm.dmPelsHeight = height;
            dm.dmDisplayFrequency = displayFrequency;
            dm.dmBitsPerPel = bitsPerPel;
            dm.dmFields = DEVMODE.DM_PELSWIDTH | DEVMODE.DM_PELSHEIGHT | DEVMODE.DM_DISPLAYFREQUENCY;
            RetVal = ChangeDisplaySettings(ref dm, CDS_TEST);
            if (RetVal == 0)
            {
                RetVal = ChangeDisplaySettings(ref dm, 0);
                ret = true;
            }
            return ret;
        }

        public static DEVMODE GetResolution(string DeviceName)
        {
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            bool mybool;
            mybool = EnumDisplaySettings(DeviceName, -1, ref dm);
            return dm;
        }

        public static List<DEVMODE> GetResolutions(string DeviceName)
        {
            List<DEVMODE> allMode = new List<DEVMODE>();
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            int index = 0;
            while (EnumDisplaySettings(DeviceName, index, ref dm))
            {
                allMode.Add(dm);
                index++;
            }
            return allMode;
        }

        public static void PlayWindowsMedia(string file)
        {
            string path = Path.Combine(@"c:\Windows\Media\", file);
            if (File.Exists(path))
                new SoundPlayer(path).Play();
        }

        public static bool HasVolumeSupport()
        {
            return VolumeSupport;
        }

        public static void SetVolume(int volume)
        {
            if (!VolumeSupport)
                return;

            multimediaDevice.AudioEndpointVolume.MasterVolumeLevelScalar = (float)(volume / 100.0f);
        }

        public static int GetVolume()
        {
            if (!VolumeSupport)
                return 0;

            return (int)Math.Round(multimediaDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100.0f);
        }

        public static bool HasBrightnessSupport()
        {
            return BrightnessSupport;
        }

        public static void SetBrightness(int brightness)
        {
            if (!BrightnessSupport)
                return;

            try
            {
                using (var mclass = new ManagementClass("WmiMonitorBrightnessMethods"))
                {
                    mclass.Scope = new ManagementScope(@"\\.\root\wmi");
                    using (var instances = mclass.GetInstances())
                    {
                        foreach (ManagementObject instance in instances)
                        {
                            object[] args = new object[] { 1, (byte)brightness };
                            instance.InvokeMethod("WmiSetBrightness", args);
                        }
                    }
                }
            }
            catch { }
        }

        public static int GetBrightness()
        {
            try
            {
                using (var mclass = new ManagementClass("WmiMonitorBrightness"))
                {
                    mclass.Scope = new ManagementScope(@"\\.\root\wmi");
                    using (var instances = mclass.GetInstances())
                    {
                        foreach (ManagementObject instance in instances)
                        {
                            return (byte)instance.GetPropertyValue("CurrentBrightness");
                        }
                    }
                }
                return 0;
            }
            catch { }

            return -1;
        }
    }
}
