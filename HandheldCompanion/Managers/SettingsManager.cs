using HandheldCompanion.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace HandheldCompanion.Managers
{
    public static class SettingsManager
    {
        public static bool IsInitialized { get; internal set; }
        private static Dictionary<string, object> Settings = new();

        public static event SettingValueChangedEventHandler SettingValueChanged;
        public delegate void SettingValueChangedEventHandler(string name, object value);

        public static event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();

        static SettingsManager()
        {
        }

        public static void Start()
        {
            var properties = Properties.Settings
                .Default
                .Properties
                .Cast<SettingsProperty>()
                .OrderBy(s => s.Name);

            foreach (SettingsProperty property in properties)
                SettingValueChanged(property.Name, GetProperty(property.Name));

            IsInitialized = true;
            Initialized?.Invoke();

            LogManager.LogInformation("{0} has started", "SettingsManager");
        }

        public static void Stop()
        {
            if (!IsInitialized)
                return;

            IsInitialized = false;

            LogManager.LogInformation("{0} has stopped", "SettingsManager");
        }

        public static void SetProperty(string name, object value, bool force = false, bool temporary = false)
        {
            object prevValue = GetProperty(name, temporary);
            if (prevValue.ToString() == value.ToString() && !force)
                return;

            try
            {
                if (!temporary)
                {
                    Properties.Settings.Default[name] = value;
                    Properties.Settings.Default.Save();
                }

                // update internal settings dictionary (used for temporary settings)
                Settings[name] = value;

                // raise event
                SettingValueChanged?.Invoke(name, value);

                LogManager.LogDebug("Settings {0} set to {1}", name, value);
            }
            catch { }
        }

        private static bool PropertyExists(string name)
        {
            return Properties.Settings.Default.Properties.Cast<SettingsProperty>().Any(prop => prop.Name == name);
        }

        public static SortedDictionary<string, object> GetProperties()
        {
            SortedDictionary<string, object> result = new();

            foreach (SettingsProperty property in Properties.Settings.Default.Properties)
                result.Add(property.Name, GetProperty(property.Name));

            return result;
        }

        private static object GetProperty(string name, bool temporary = false)
        {
            switch (name)
            {
                // variable defaults based on device
                case "PerformanceTDPValue":
                    uint TDPvalue = Convert.ToUInt32(Properties.Settings.Default[name]);
                    return TDPvalue != 0 ? TDPvalue : MainWindow.CurrentDevice.TDP[1];
                case "PerformanceGPUValue":
                    uint GPUvalue = Convert.ToUInt32(Properties.Settings.Default[name]);
                    return GPUvalue != 0 ? GPUvalue : MainWindow.CurrentDevice.GPU[1];

                // virtual settings, only for hotkeys
                case "HasBrightnessSupport":
                    return SystemManager.HasBrightnessSupport();
                case "HasVolumeSupport":
                    return SystemManager.HasVolumeSupport();
                case "HasFanControlSupport":
                    return SystemManager.HasFanControlSupport();
                case "HasTDPSupport":
                    return PerformanceManager.CanChangeTDP();

                default:
                    if (temporary && Settings.ContainsKey(name))
                        return Settings[name];
                    else if (PropertyExists(name))
                        return Properties.Settings.Default[name];

                    return false;
            }
        }

        public static string GetString(string name, bool temporary = false)
        {
            return Convert.ToString(GetProperty(name, temporary));
        }

        public static bool GetBoolean(string name, bool temporary = false)
        {
            return Convert.ToBoolean(GetProperty(name, temporary));
        }

        public static int GetInt(string name, bool temporary = false)
        {
            return Convert.ToInt32(GetProperty(name, temporary));
        }

        public static uint GetUInt(string name, bool temporary = false)
        {
            return Convert.ToUInt32(GetProperty(name, temporary));
        }

        public static DateTime GetDateTime(string name, bool temporary = false)
        {
            return Convert.ToDateTime(GetProperty(name, temporary));
        }

        public static double GetDouble(string name, bool temporary = false)
        {
            return Convert.ToDouble(GetProperty(name, temporary));
        }
    }
}
