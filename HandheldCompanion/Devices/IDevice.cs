using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsInput.Events;

namespace HandheldCompanion.Devices
{
    [Flags]
    public enum DeviceCapabilities : ushort
    {
        None = 0,
        FanControl = 1,
    }

    public abstract class IDevice
    {
        public string InternalSensorName = string.Empty;
        public string ExternalSensorName = string.Empty;

        public string ManufacturerName;
        public string ProductName;

        public string ProductIllustration = "device_generic";
        public string ProductModel = "default";

        protected DeviceCapabilities Capabilities = DeviceCapabilities.None;

        // device TDP max limits (min, nominal, max)
        public uint[] TDP = { 10, 20, 25 };
        // device GPU max frequency limits (min, nominal/max)
        public uint[] GPU = { 100, 1800 };

        // trigger specific settings
        public List<DeviceChord> OEMChords = new();
        public IEnumerable<ButtonFlags> OEMButtons => OEMChords.SelectMany(a => a.state.Buttons).Distinct();

        public IDevice()
        {
            // We assume all the devices have those keys
            OEMChords.Add(new DeviceChord("Volume Up",
                new List<KeyCode>() { KeyCode.VolumeUp },
                new List<KeyCode>() { KeyCode.VolumeUp },
                false, ButtonFlags.VolumeUp
                ));
            OEMChords.Add(new DeviceChord("Volume Down",
                new List<KeyCode>() { KeyCode.VolumeDown },
                new List<KeyCode>() { KeyCode.VolumeDown },
                false, ButtonFlags.VolumeDown
                ));
        }

        private static IDevice device;
        public static IDevice GetDefault()
        {
            if (device is not null)
                return device;

            var ManufacturerName = MotherboardInfo.Manufacturer.ToUpper();
            var ProductName = MotherboardInfo.Product;
            var SystemName = MotherboardInfo.SystemName;
            var Version = MotherboardInfo.Version;

            switch (ManufacturerName)
            {
                case "VALVE":
                    {
                        switch (ProductName)
                        {
                            case "Jupiter":
                                device = new SteamDeck();
                                break;
                        }
                    }
                    break;
            }

            LogManager.LogInformation("{0} from {1}", ProductName, ManufacturerName);

            if (device is null)
            {
                device = new DefaultDevice();
                LogManager.LogWarning("Device not yet supported. The behavior of the application will be unpredictable");
            }

            // get the actual handheld device
            device.ManufacturerName = ManufacturerName;
            device.ProductName = ProductName;

            return device;
        }

        public bool HasFanControl()
        {
            return Capabilities.HasFlag(DeviceCapabilities.FanControl);
        }

        public virtual bool IsOpen
        {
            get { return false; }
        }

        public virtual bool IsSupported
        {
            get { return false; }
        }

        public virtual bool Open()
        {
            return false;
        }

        public virtual void Close()
        {
        }

        public string GetButtonName(ButtonFlags button)
        {
            return EnumUtils.GetDescriptionFromEnumValue(button, this.GetType().Name);
        }

        public virtual void SetFanDuty(uint percent)
        {
        }

        public virtual void SetFanControl(bool enable)
        {
        }
    }
}
