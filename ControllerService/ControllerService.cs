using ControllerCommon;
using ControllerCommon.Managers;
using ControllerCommon.Pipes;
using ControllerCommon.Utils;
using ControllerService.Targets;
using Microsoft.Extensions.Hosting;
using Nefarius.ViGEm.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ControllerCommon.Managers.PowerManager;
using IDevice = ControllerCommon.Devices.IDevice;

namespace ControllerService
{
    public class ControllerService : IHostedService
    {
        // controllers vars
        public static ViGEmClient vClient;
        public static ViGEmTarget vTarget;

        private DSUServer DSUServer;

        // devices vars
        public static IDevice CurrentDevice;

        public static string CurrentPath, CurrentPathDep;
        public static string CurrentTag;
        public static int CurrentOverlayStatus = 2;

        // settings vars
        public Configuration configuration;
        private string DSUip;
        private bool DSUEnabled;
        private int DSUport;
        private HIDmode HIDmode = HIDmode.NoController;
        private HIDstatus HIDstatus = HIDstatus.Disconnected;

        // profile vars
        public static Profile currentProfile = new();

        public ControllerService(IHostApplicationLifetime lifetime)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            // paths
            CurrentPath = AppDomain.CurrentDomain.BaseDirectory;
            CurrentPathDep = Path.Combine(CurrentPath, "dependencies");

            // settings
            // todo: move me to a specific class
            configuration = ConfigurationManager.OpenExeConfiguration("ControllerService.exe");

            HIDmode = Enum.Parse<HIDmode>(configuration.AppSettings.Settings["HIDmode"].Value);
            HIDstatus = Enum.Parse<HIDstatus>(configuration.AppSettings.Settings["HIDstatus"].Value);

            DSUEnabled = bool.Parse(configuration.AppSettings.Settings["DSUEnabled"].Value);
            DSUip = configuration.AppSettings.Settings["DSUip"].Value;
            DSUport = int.Parse(configuration.AppSettings.Settings["DSUport"].Value);

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

            // initialize PipeServer
            PipeServer.Connected += OnClientConnected;
            PipeServer.Disconnected += OnClientDisconnected;
            PipeServer.ClientMessage += OnClientMessage;

            // initialize device
            CurrentDevice = IDevice.GetDefault();

            // initialize DSUClient
            DSUServer = new DSUServer(DSUip, DSUport);
            DSUServer.Started += OnDSUStarted;
            DSUServer.Stopped += OnDSUStopped;
        }

        private void SetControllerMode(HIDmode mode)
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

            // update status
            SetControllerStatus(HIDstatus);

            // update current HIDmode
            HIDmode = mode;
        }

        private void SetControllerStatus(HIDstatus status)
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

        private void OnTargetDisconnected(ViGEmTarget target)
        {
            // send notification
            PipeServer.SendMessage(new PipeServerToast
            {
                title = $"{target}",
                content = Properties.Resources.ToastOnTargetDisconnected,
                image = $"HIDmode{(uint)target.HID}"
            });
        }

        private void OnTargetConnected(ViGEmTarget target)
        {
            // send notification
            PipeServer.SendMessage(new PipeServerToast
            {
                title = $"{target}",
                content = Properties.Resources.ToastOnTargetConnected,
                image = $"HIDmode{(uint)target.HID}"
            });
        }

        // deprecated
        private void OnDSUStopped(DSUServer server)
        {
            /* DSUEnabled = false;
            configuration.GetSection("Settings:DSUEnabled").Value = false.ToString();

            PipeServerSettings settings = new PipeServerSettings("DSUEnabled", DSUEnabled.ToString());
            pipeServer.SendMessage(settings); */
        }

        // deprecated
        private void OnDSUStarted(DSUServer server)
        {
            /* DSUEnabled = true;
            configuration.GetSection("Settings:DSUEnabled").Value = true.ToString();

            PipeServerSettings settings = new PipeServerSettings("DSUEnabled", DSUEnabled.ToString());
            pipeServer.SendMessage(settings); */
        }

        private void OnClientMessage(PipeMessage message)
        {
            switch (message.code)
            {
                case PipeCode.CLIENT_PROFILE:
                    {
                        PipeClientProfile profile = (PipeClientProfile)message;
                        UpdateProfile(profile.GetProfile());
                    }
                    break;

                case PipeCode.CLIENT_SETTINGS:
                    {
                        PipeClientSettings settings = (PipeClientSettings)message;
                        UpdateSettings(settings.settings);
                    }
                    break;

                case PipeCode.CLIENT_NAVIGATED:
                    {
                        PipeNavigation navigation = (PipeNavigation)message;
                        CurrentTag = navigation.Tag;

                        switch (navigation.Tag)
                        {
                            case "SettingsMode0":
                                // do something
                                break;
                            case "SettingsMode1":
                                // do something
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case PipeCode.CLIENT_OVERLAY:
                    {
                        PipeOverlay overlay = (PipeOverlay)message;
                        CurrentOverlayStatus = overlay.Visibility;
                    }
                    break;

                case PipeCode.CLIENT_INPUT:
                    {
                        PipeClientInputs input = (PipeClientInputs)message;

                        vTarget?.UpdateInputs(input.Inputs);
                        DSUServer.UpdateInputs(input.Inputs);
                        DS4Touch.UpdateInputs(input.Inputs);
                    }
                    break;

                case PipeCode.CLIENT_MOVEMENTS:
                    {
                        PipeClientMovements movements = (PipeClientMovements)message;

                        IMU.UpdateMovements(movements.Movements);
                    }
                    break;

                case PipeCode.CLIENT_CONTROLLER_CONNECT:
                    {
                        PipeClientControllerConnect connect = (PipeClientControllerConnect)message;
                        // do something?
                    }
                    break;

                case PipeCode.CLIENT_CONTROLLER_DISCONNECT:
                    {
                        PipeClientControllerDisconnect disconnect = (PipeClientControllerDisconnect)message;
                        // do something ?
                    }
                    break;
            }
        }

        private void OnClientDisconnected()
        {
        }

        private void OnClientConnected()
        {
            // send server settings to client
            PipeServer.SendMessage(new PipeServerSettings() { settings = GetSettings() });
        }

        internal void UpdateProfile(Profile profile)
        {
            // skip if current profile
            if (profile == currentProfile)
                return;

            // update current profile
            currentProfile = profile;

            LogManager.LogInformation("Profile {0} applied", profile.Name);
        }

        public void UpdateSettings(Dictionary<string, object> args)
        {
            foreach (KeyValuePair<string, object> pair in args)
            {
                string name = pair.Key;
                string property = pair.Value.ToString();

                if (configuration.AppSettings.Settings.AllKeys.ToList().Contains(name))
                {
                    configuration.AppSettings.Settings[name].Value = property;
                    configuration.Save(ConfigurationSaveMode.Modified);
                }

                ApplySetting(name, property);
                LogManager.LogDebug("{0} set to {1}", name, property);
            }
        }

        private void ApplySetting(string name, string property)
        {
            switch (name)
            {
                case "HIDmode":
                    {
                        HIDmode value = Enum.Parse<HIDmode>(property);

                        if (HIDmode == value)
                            return;

                        SetControllerMode(value);
                    }
                    break;
                case "HIDstatus":
                    {
                        HIDstatus value = Enum.Parse<HIDstatus>(property);

                        if (HIDstatus == value)
                            return;

                        SetControllerStatus(value);
                    }
                    break;
                case "DSUEnabled":
                    {
                        bool value = Convert.ToBoolean(property);
                        switch (value)
                        {
                            case true: DSUServer.Start(); break;
                            case false: DSUServer.Stop(); break;
                        }
                    }
                    break;
                case "DSUip":
                    {
                        string value = Convert.ToString(property);
                        DSUServer.ip = value;
                    }
                    break;
                case "DSUport":
                    {
                        int value = Convert.ToInt32(property);
                        DSUServer.port = value;
                    }
                    break;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // start master timer
            TimerManager.Start();

            // start listening from controller
            IMU.Start();

            // start DSUClient
            if (DSUEnabled)
                DSUServer.Start();

            // start Pipe Server
            PipeServer.Open();

            // start Power Manager
            PowerManager.SystemStatusChanged += OnSystemStatusChanged;
            PowerManager.Start(true);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // stop master timer
            TimerManager.Stop();

            // stop listening from controller
            IMU.Stop();

            // update virtual controller
            SetControllerMode(HIDmode.NoController);

            // stop Power Manager
            PowerManager.Stop();

            // stop DSUClient
            DSUServer?.Stop();

            // stop Pipe Server
            PipeServer.Close();

            // stop System Manager
            DeviceManager.Stop();

            return Task.CompletedTask;
        }

        private void OnSystemStatusChanged(SystemStatus status, SystemStatus prevStatus)
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
                            Thread.Sleep(CurrentDevice.ResumeDelay);
                        }

                        // check if service/system was suspended previously
                        if (vTarget is not null)
                            return;

                        while(vTarget is null || !vTarget.IsConnected)
                        {
                            // reset vigem
                            ResetViGEm();

                            // create new ViGEm client
                            vClient = new ViGEmClient();

                            // set controller mode
                            SetControllerMode(HIDmode);

                            Thread.Sleep(1000);
                        }

                        // start timer manager
                        TimerManager.Start();
                    }
                    break;
                case SystemStatus.SystemPending:
                    {
                        // stop timer manager
                        TimerManager.Stop();

                        // clear pipes
                        PipeServer.ClearQueue();

                        // reset vigem
                        ResetViGEm();
                    }
                    break;
            }
        }

        private void ResetViGEm()
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

        public Dictionary<string, string> GetSettings()
        {
            Dictionary<string, string> settings = new();

            foreach (string key in configuration.AppSettings.Settings.AllKeys)
                settings.Add(key, configuration.AppSettings.Settings[key].Value);

            return settings;
        }
    }
}
