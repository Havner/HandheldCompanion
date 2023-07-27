using HandheldCompanion.Managers;
using HandheldCompanion.Utils;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HandheldCompanion.Views.QuickPages
{
    /// <summary>
    /// Interaction logic for QuickSettingsPage.xaml
    /// </summary>
    public partial class QuickSettingsPage : Page
    {
        private LockObject updateVolumeLock = new();
        private LockObject updateBrightnessLock = new();

        public QuickSettingsPage()
        {
            InitializeComponent();

            HotkeysManager.HotkeyCreated += HotkeysManager_HotkeyCreated;
            HotkeysManager.HotkeyUpdated += HotkeysManager_HotkeyUpdated;

            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;

            SystemManager.VolumeNotification += SystemManager_VolumeNotification;
            SystemManager.BrightnessNotification += SystemManager_BrightnessNotification;
            SystemManager.Initialized += SystemManager_Initialized;
        }

        private void SystemManager_Initialized()
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (SystemManager.HasBrightnessSupport())
                {
                    SliderBrightness.IsEnabled = true;
                    SliderBrightness.Value = SystemManager.GetBrightness();
                }

                if (SystemManager.HasVolumeSupport())
                {
                    SliderVolume.IsEnabled = true;
                    SliderVolume.Value = SystemManager.GetVolume();
                }
            });
        }

        private void HotkeysManager_HotkeyUpdated(Hotkey hotkey)
        {
            UpdatePins();
        }

        private void HotkeysManager_HotkeyCreated(Hotkey hotkey)
        {
            UpdatePins();
        }

        private void UpdatePins()
        {
            // todo, implement quick hotkey order
            QuickHotkeys.Children.Clear();

            foreach (Hotkey hotkey in HotkeysManager.Hotkeys.Values.Where(item => item.IsPinned))
                QuickHotkeys.Children.Add(hotkey.GetPin());
        }

        private void SettingsManager_SettingValueChanged(string name, object value)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (name)
                {
                    case "VibrationStrength":
                        SliderVibration.Value = Convert.ToUInt32(value);
                        break;
                }
            });
        }

        private void SystemManager_BrightnessNotification(int brightness)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                using (new ScopedLock(updateBrightnessLock))
                    SliderBrightness.Value = brightness;
            });
        }

        private void SystemManager_VolumeNotification(int volume)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // todo: update volume icon on update
                using (new ScopedLock(updateVolumeLock))
                    SliderVolume.Value = volume;
            });
        }

        private void SliderVibration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("VibrationStrength", (uint)SliderVibration.Value);
        }

        private void SliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || updateBrightnessLock)
                return;

            SystemManager.SetBrightness((int)SliderBrightness.Value);
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || updateVolumeLock)
                return;

            SystemManager.SetVolume((int)SliderVolume.Value);
        }
    }
}