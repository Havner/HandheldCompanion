using ControllerCommon.Controllers;
using ControllerCommon.Managers;
using ControllerCommon.Utils;
using HandheldCompanion.Targets;
using Nefarius.ViGEm.Client;
using System;
using System.Threading;
using static ControllerCommon.Managers.PowerManager;
using IDevice = ControllerCommon.Devices.IDevice;

namespace HandheldCompanion.Managers
{
    public static class VirtualManager
    {
        // controllers vars
        public static ViGEmClient vClient;
        public static ViGEmTarget vTarget;

        private static DSUServer DSUServer;

        // devices vars
        public static IDevice CurrentDevice;

        // settings vars
        private static HIDmode HIDmode = HIDmode.NoController;
        private static HIDstatus HIDstatus = HIDstatus.Disconnected;

        public static bool IsInitialized;

        public static event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();

        public static event VibrateEventHandler Vibrated;
        public delegate void VibrateEventHandler(byte LargeMotor, byte SmallMotor);

        static VirtualManager()
        {
            // verifying ViGEm is installed
            try
            {
                vClient = new ViGEmClient();
            }
            catch (Exception)
            {
                LogManager.LogCritical("ViGEm is missing. Please get it from: {0}", "https://github.com/ViGEm/ViGEmBus/releases");
                throw new InvalidOperationException();
            }
        }

        public static void Start()
        {
            // initialize device
            CurrentDevice = IDevice.GetDefault();

            // initialize DSUClient
            DSUServer = new DSUServer();

            PowerManager.SystemStatusChanged += OnSystemStatusChanged;
            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;
            SettingsManager.Initialized += SettingsManager_Initialized;

            IsInitialized = true;
            Initialized?.Invoke();
        }

        public static void Stop()
        {
            // update virtual controller
            SetControllerMode(HIDmode.NoController);

            // stop DSUClient
            DSUServer?.Stop();

            IsInitialized = false;
        }

        private static void SettingsManager_SettingValueChanged(string name, object value)
        {
            switch (name)
            {
                case "HIDmode":
                    SetControllerMode((HIDmode)Convert.ToInt32(value));
                    break;
                case "HIDstatus":
                    SetControllerStatus((HIDstatus)Convert.ToInt32(value));
                    break;
                case "DSUEnabled":
                    if (SettingsManager.IsInitialized)
                        SetDSUStatus(Convert.ToBoolean(value));
                    break;
                case "DSUip":
                    DSUServer.ip = Convert.ToString(value);
                    if (SettingsManager.IsInitialized)
                        SetDSUStatus(SettingsManager.GetBoolean("DSUEnabled"));
                    break;
                case "DSUport":
                    DSUServer.port = Convert.ToInt32(value);
                    if (SettingsManager.IsInitialized)
                        SetDSUStatus(SettingsManager.GetBoolean("DSUEnabled"));
                    break;
            }
        }

        private static void SettingsManager_Initialized()
        {
            SetDSUStatus(SettingsManager.GetBoolean("DSUEnabled"));
        }

        private static void SetDSUStatus(bool started)
        {
            if (started)
                DSUServer.Start();
            else
                DSUServer.Stop();
        }

        private static void SetControllerMode(HIDmode mode)
        {
            // do not disconnect if similar to previous mode
            if (HIDmode == mode && vTarget is not null)
                return;

            // disconnect current virtual controller
            if (vTarget is not null)
                vTarget.Disconnect();

            switch (mode)
            {
                default:
                case HIDmode.NoController:
                    if (vTarget is not null)
                    {
                        vTarget.Dispose();
                        vTarget = null;
                    }
                    break;
                case HIDmode.DualShock4Controller:
                    vTarget = new DualShock4Target();
                    break;
                case HIDmode.Xbox360Controller:
                    vTarget = new Xbox360Target();
                    break;
            }

            // failed to initialize controller
            if (vTarget is null)
            {
                if (mode != HIDmode.NoController)
                    LogManager.LogError("Failed to initialise virtual controller with HIDmode: {0}", mode);
                return;
            }

            vTarget.Connected += OnTargetConnected;
            vTarget.Disconnected += OnTargetDisconnected;
            vTarget.Vibrated += OnTargetVibrated;

            // update status
            SetControllerStatus(HIDstatus);

            // update current HIDmode
            HIDmode = mode;
        }

        private static void SetControllerStatus(HIDstatus status)
        {
            if (vTarget is null)
                return;

            switch (status)
            {
                default:
                case HIDstatus.Connected:
                    vTarget.Connect();
                    break;
                case HIDstatus.Disconnected:
                    vTarget.Disconnect();
                    break;
            }

            // update current HIDstatus
            HIDstatus = status;
        }

        private static void OnTargetConnected(ViGEmTarget target)
        {
            ToastManager.SendToast($"{target}", "is now connected", $"HIDmode{(uint)target.HID}");
        }

        private static void OnTargetDisconnected(ViGEmTarget target)
        {
            ToastManager.SendToast($"{target}", "is now disconnected", $"HIDmode{(uint)target.HID}");
        }

        private static void OnTargetVibrated(byte LargeMotor, byte SmallMotor)
        {
            Vibrated?.Invoke(LargeMotor, SmallMotor);
        }

        public static void UpdateInputs(ControllerState controllerState)
        {
            // TODO: put touch first
            vTarget?.UpdateInputs(controllerState);
            DSUServer?.UpdateInputs(controllerState);
            DS4Touch.UpdateInputs(controllerState);
        }

        // TODO: move to MainWindow?
        private static void OnSystemStatusChanged(SystemStatus status, SystemStatus prevStatus)
        {
            if (status == prevStatus)
                return;

            switch (status)
            {
                case SystemStatus.SystemReady:
                    {
                        if (prevStatus == SystemStatus.SystemPending)
                        {
                            // resume from sleep
                            // TODO: switch to Task.Delay
                            Thread.Sleep(CurrentDevice.ResumeDelay);
                        }

                        // check if service/system was suspended previously
                        if (vTarget is not null)
                            return;

                        while (vTarget is null || !vTarget.IsConnected)
                        {
                            // reset vigem
                            ResetViGEm();

                            // create new ViGEm client
                            vClient = new ViGEmClient();

                            // set controller mode
                            SetControllerMode(HIDmode);

                            Thread.Sleep(1000);
                        }
                    }
                    break;
                case SystemStatus.SystemPending:
                    {
                        // reset vigem
                        ResetViGEm();
                    }
                    break;
            }
        }

        private static void ResetViGEm()
        {
            // dispose virtual controller
            if (vTarget is not null)
            {
                vTarget.Dispose();
                vTarget = null;
            }

            // dispose ViGEm drivers
            if (vClient is not null)
            {
                vClient.Dispose();
                vClient = null;
            }
        }
    }
}
