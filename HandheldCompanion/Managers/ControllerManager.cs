using HandheldCompanion.Controllers;
using HandheldCompanion.Inputs;
using HandheldCompanion.Misc;
using HandheldCompanion.Platforms;
using HandheldCompanion.Utils;
using HandheldCompanion.Views;
using Nefarius.Utilities.DeviceManagement.PnP;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DeviceType = SharpDX.DirectInput.DeviceType;

namespace HandheldCompanion.Managers
{
    public static class ControllerManager
    {
        #region events
        public static event ControllerPluggedEventHandler ControllerPlugged;
        public delegate void ControllerPluggedEventHandler(IController Controller);

        public static event ControllerUnpluggedEventHandler ControllerUnplugged;
        public delegate void ControllerUnpluggedEventHandler(IController Controller);

        public static event ControllerSelectedEventHandler ControllerSelected;
        public delegate void ControllerSelectedEventHandler(IController Controller);

        public static event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();
        #endregion

        private static Dictionary<string, IController> Controllers = new();
        private static Dictionary<UserIndex, bool> XUsbControllers = new()
        {
            { UserIndex.One, true },
            { UserIndex.Two, true },
            { UserIndex.Three, true },
            { UserIndex.Four, true },
        };

        private static XInputController? emptyXInput = new();
        private static DS4Controller? emptyDS4 = new();

        private static IController? targetController;
        private static ProcessEx? foregroundProcess;

        private static bool IsInitialized;

        public static void Start()
        {
            DeviceManager.XUsbDeviceArrived += DeviceManager_XUsbDeviceArrived;
            DeviceManager.XUsbDeviceRemoved += DeviceManager_XUsbDeviceRemoved;

            DeviceManager.HidDeviceArrived += DeviceManager_HidDeviceArrived;
            DeviceManager.HidDeviceRemoved += DeviceManager_HidDeviceRemoved;

            DeviceManager.Initialized += DeviceManager_Initialized;

            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;

            ProcessManager.ForegroundChanged += ProcessManager_ForegroundChanged;

            VirtualManager.Vibrated += VirtualManager_Vibrated;

            // enable HidHide
            HidHide.SetCloaking(true);

            IsInitialized = true;
            Initialized?.Invoke();

            // summon an empty controller, used to feed Layout UI
            // todo: improve me
            ControllerSelected?.Invoke(GetEmulatedController());

            LogManager.LogInformation("{0} has started", "ControllerManager");
        }

        private static void ProcessManager_ForegroundChanged(ProcessEx processEx)
        {
            foregroundProcess = processEx;
        }

        public static void Stop()
        {
            if (!IsInitialized)
                return;

            IsInitialized = false;

            DeviceManager.XUsbDeviceArrived -= DeviceManager_XUsbDeviceArrived;
            DeviceManager.XUsbDeviceRemoved -= DeviceManager_XUsbDeviceRemoved;

            DeviceManager.HidDeviceArrived -= DeviceManager_HidDeviceArrived;
            DeviceManager.HidDeviceRemoved -= DeviceManager_HidDeviceRemoved;

            SettingsManager.SettingValueChanged -= SettingsManager_SettingValueChanged;

            // uncloak on close, if requested
            if (SettingsManager.GetBoolean("HIDuncloakonclose"))
                foreach (IController controller in Controllers.Values)
                    controller.Unhide();

            // unplug on close
            targetController?.Unplug();

            LogManager.LogInformation("{0} has stopped", "ControllerManager");
        }

        private static void SettingsManager_SettingValueChanged(string name, object value)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (name)
                {
                    case "VibrationStrength":
                        uint VibrationStrength = Convert.ToUInt32(value);
                        SetHIDStrength(VibrationStrength);
                        break;

                    case "SteamDeckMuteController":
                        {
                            if (targetController is null)
                                return;

                            if (typeof(NeptuneController) != targetController.GetType())
                                return;

                            bool Muted = Convert.ToBoolean(value);
                            ((NeptuneController)targetController).SetVirtualMuted(Muted);
                        }
                        break;
                }
            });
        }

        private static void DeviceManager_Initialized()
        {
            // search for last known controller and connect
            string path = SettingsManager.GetString("HIDInstancePath");

            if (Controllers.ContainsKey(path))
            {
                SetTargetController(path);
            }
            else if (Controllers.Count != 0)
            {
                // no known controller, connect to the first available
                path = Controllers.Keys.FirstOrDefault();
                SetTargetController(path);
            }
        }

        private static void SetHIDStrength(uint value)
        {
            targetController?.SetVibrationStrength(value);
        }

        private static void VirtualManager_Vibrated(byte LargeMotor, byte SmallMotor)
        {
            targetController?.SetVibration(LargeMotor, SmallMotor);
        }

        // usb thread, IController contains lots of WPF controls
        private static void DeviceManager_HidDeviceArrived(PnPDetails details, DeviceEventArgs obj)
        {
            DirectInput directInput = new DirectInput();
            int VendorId = details.attributes.VendorID;
            int ProductId = details.attributes.ProductID;

            // We need to wait for each controller to initialize and take (or not) its slot in the array
            Joystick joystick = null;
            IController controller = null;

            // search for the plugged controller
            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
            {
                try
                {
                    // Instantiate the joystick
                    var lookup_joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                    string SymLink = DeviceManager.PathToInstanceId(lookup_joystick.Properties.InterfacePath, obj.InterfaceGuid.ToString());

                    // IG_ means it is an XInput controller and therefore is handled elsewhere
                    if (lookup_joystick.Properties.InterfacePath.Contains("IG_", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    if (SymLink.Equals(details.SymLink, StringComparison.InvariantCultureIgnoreCase))
                    {
                        joystick = lookup_joystick;
                        break;
                    }
                }
                catch { }
            }

            if (joystick is not null)
            {
                // supported controller
                VendorId = joystick.Properties.VendorId;
                ProductId = joystick.Properties.ProductId;
            }
            else
            {
                // unsupported controller
                LogManager.LogError("Couldn't find matching DInput controller: VID:{0} and PID:{1}", details.GetVendorID(), details.GetProductID());
            }

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // search for a supported controller
                switch (VendorId)
                {
                    // SONY
                    case 0x054C:
                        {
                            switch (ProductId)
                            {
                                case 0x0268:    // DualShock 3
                                case 0x05C4:    // DualShock 4
                                case 0x09CC:    // DualShock 4 (2nd Gen)
                                case 0x0CE6:    // DualSense
                                    controller = new DS4Controller(joystick, details);
                                    break;
                            }
                        }
                        break;

                    // STEAM
                    case 0x28DE:
                        {
                            switch (ProductId)
                            {
                                // WIRED STEAM CONTROLLER
                                case 0x1102:
                                    // TODO: implement
                                    break;
                                // WIRELESS STEAM CONTROLLER
                                case 0x1142:
                                    // TODO: The dongle registers 4 controller devices, regardless how many are
                                    // actually connected. There is no easy way to check for connection without
                                    // actually talking to each controller. Handle only the first for now.
                                    if (details.GetMI() == 1)
                                        controller = new GordonController(details, 1);
                                    break;
                                // STEAM DECK
                                case 0x1205:
                                    controller = new NeptuneController(details);
                                    break;
                            }
                        }
                        break;

                    // NINTENDO
                    case 0x057E:
                        {
                            switch (ProductId)
                            {
                                // Nintendo Wireless Gamepad
                                case 0x2009:
                                    break;
                            }
                        }
                        break;
                }

                // unsupported controller
                if (controller is null)
                {
                    LogManager.LogError("Unsupported DInput controller: VID:{0} and PID:{1}", details.GetVendorID(), details.GetProductID());
                    return;
                }

                // failed to initialize
                if (controller.Details is null)
                    return;

                if (!controller.IsConnected())
                    return;

                if (controller.IsVirtual())
                    return;

                // update or create controller
                string path = controller.GetInstancePath();
                Controllers[path] = controller;

                // raise event
                ControllerPlugged?.Invoke(controller);
                ToastManager.SendToast(controller.ToString(), "detected");

                // automatically connect DInput controller if only available
                if (GetControllerCount() == 1 && SystemManager.IsInitialized)
                    SetTargetController(path);
            });
        }

        private static void DeviceManager_HidDeviceRemoved(PnPDetails details, DeviceEventArgs obj)
        {
            if (!Controllers.TryGetValue(details.deviceInstanceId, out IController controller))
                return;

            if (!controller.IsConnected())
                return;

            if (controller.IsVirtual())
                return;

            // XInput controller are handled elsewhere
            if (controller.GetType() == typeof(XInputController))
                return;

            // controller was unplugged
            Controllers.Remove(details.deviceInstanceId);

            // raise event
            ControllerUnplugged?.Invoke(controller);
        }

        // usb thread, IController contains lots of WPF controls
        private static void DeviceManager_XUsbDeviceArrived(PnPDetails details, DeviceEventArgs obj)
        {
            // trying to guess XInput behavior...
            // get first available slot
            UserIndex slot = UserIndex.One;
            Controller _controller = new(slot);

            for (slot = UserIndex.One; slot <= UserIndex.Four; slot++)
            {
                _controller = new(slot);

                // check if controller is connected and slot free
                if (_controller.IsConnected && XUsbControllers[slot])
                    break;
            }

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // We need to wait for each controller to initialize and take (or not) its slot in the array
                XInputController controller = new(_controller);

                // failed to initialize
                if (controller.Details is null)
                    return;

                if (!controller.IsConnected())
                    return;

                // slot is now busy
                XUsbControllers[slot] = false;

                if (controller.IsVirtual())
                    return;

                // update or create controller
                string path = controller.GetInstancePath();
                Controllers[path] = controller;

                // raise event
                ControllerPlugged?.Invoke(controller);
                ToastManager.SendToast(controller.ToString(), "detected");

                // automatically connect XInput controller if only available
                if (GetControllerCount() == 1 && SystemManager.IsInitialized)
                    SetTargetController(path);
            });
        }

        private static void DeviceManager_XUsbDeviceRemoved(PnPDetails details, DeviceEventArgs obj)
        {
            if (!Controllers.TryGetValue(details.deviceInstanceId, out IController controller))
                return;

            if (controller.IsConnected())
                return;

            // slot is now free
            UserIndex slot = (UserIndex)controller.GetUserIndex();
            XUsbControllers[slot] = true;

            if (controller.IsVirtual())
                return;

            // controller was unplugged
            Controllers.Remove(details.deviceInstanceId);

            // raise event
            ControllerUnplugged?.Invoke(controller);
        }

        public static void SetTargetController(string baseContainerDeviceInstancePath)
        {
            // unplug previous controller
            if (targetController is not null)
            {
                targetController.InputsUpdated -= TargetController_InputsUpdated;
                targetController.Unplug();
            }

            // look for new controller
            if (!Controllers.TryGetValue(baseContainerDeviceInstancePath, out IController controller))
                return;

            if (controller is null)
                return;

            if (controller.IsVirtual())
                return;

            // update target controller
            targetController = controller;

            targetController.InputsUpdated += TargetController_InputsUpdated;

            targetController.Plug();

            if (SettingsManager.GetBoolean("HIDcloakonconnect"))
                targetController.Hide();

            // update settings
            SettingsManager.SetProperty("HIDInstancePath", baseContainerDeviceInstancePath);

            // raise event
            ControllerSelected?.Invoke(targetController);
        }

        public static IController GetTargetController()
        {
            return targetController;
        }

        public static bool HasController()
        {
            return Controllers.Count != 0;
        }

        public static int GetControllerCount()
        {
            return Controllers.Count;
        }

        public static List<IController> GetControllers()
        {
            return Controllers.Values.ToList();
        }

        private static void TargetController_InputsUpdated(ControllerState controllerState)
        {
            // TODO: why clone? InputsManager clones for prevState
            ButtonState InputsState = controllerState.ButtonState.Clone() as ButtonState;

            // pass inputs to Inputs manager
            InputsManager.UpdateReport(InputsState);

            // pass to MotionManager for calculations
            MotionManager.UpdateReport(controllerState);

            // pass inputs to Overlay Model
            MainWindow.overlayModel.UpdateReport(controllerState);

            // TODO: remove and implement mute keys
            // cut the mapper with mute keys
            if (controllerState.ButtonState[ButtonFlags.Special] == true ||
                controllerState.ButtonState[ButtonFlags.OEM1] == true)
                return;

            // pass inputs to Layout manager
            controllerState = LayoutManager.MapController(controllerState);

            // Controller specific scenarios
            if (targetController?.GetType() == typeof(NeptuneController))
            {
                NeptuneController neptuneController = (NeptuneController)targetController;

                // mute virtual controller if foreground process is Steam or Steam-related and user a toggle the mute setting
                if (foregroundProcess?.Platform == PlatformType.Steam)
                    if (neptuneController.IsVirtualMuted())
                        return;
            }

            VirtualManager.UpdateInputs(controllerState);
        }

        internal static IController GetEmulatedController()
        {
            HIDmode HIDmode = (HIDmode)SettingsManager.GetInt("HIDmode", true);
            switch (HIDmode)
            {
                default:
                case HIDmode.NoController:
                case HIDmode.Xbox360Controller:
                    return emptyXInput;

                case HIDmode.DualShock4Controller:
                    return emptyDS4;
            }
        }
    }
}
