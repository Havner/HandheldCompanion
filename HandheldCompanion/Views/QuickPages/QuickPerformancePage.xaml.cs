using HandheldCompanion.Managers;
using HandheldCompanion.Managers.Desktop;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.QuickPages
{
    /// <summary>
    /// Interaction logic for QuickPerformancePage.xaml
    /// </summary>
    public partial class QuickPerformancePage : Page
    {
        private readonly object powerModeLock = new();

        public QuickPerformancePage()
        {
            InitializeComponent();

            MainWindow.performanceManager.Initialized += PerformanceManager_Initialized;
            MainWindow.performanceManager.PowerModeChanged += PerformanceManager_PowerModeChanged;

            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;

            SystemManager.PrimaryScreenChanged += DesktopManager_PrimaryScreenChanged;
            SystemManager.DisplaySettingsChanged += DesktopManager_DisplaySettingsChanged;
        }

        private void DesktopManager_PrimaryScreenChanged(DesktopScreen screen)
        {
            ComboBoxResolution.Items.Clear();
            foreach (ScreenResolution resolution in screen.resolutions)
                ComboBoxResolution.Items.Add(resolution);
        }

        private void DesktopManager_DisplaySettingsChanged(ScreenResolution resolution)
        {
            ComboBoxResolution.SelectedItem = resolution;
            ComboBoxFrequency.SelectedItem = SystemManager.GetScreenFrequency();
        }

        private void PerformanceManager_Initialized()
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                TDPToggle.IsEnabled = TDPSlider.IsEnabled = MainWindow.performanceManager.CanChangeTDP();
                GPUToggle.IsEnabled = GPUSlider.IsEnabled = MainWindow.performanceManager.CanChangeGPU();

                TDPSlider.Minimum = MainWindow.CurrentDevice.TDP[0];
                TDPSlider.Maximum = MainWindow.CurrentDevice.TDP[2];

                GPUSlider.Minimum = MainWindow.CurrentDevice.GPU[0];
                GPUSlider.Maximum = MainWindow.CurrentDevice.GPU[1];

                FanControlToggle.IsEnabled = FanControlSlider.IsEnabled = SystemManager.HasFanControlSupport();
            });
        }

        private void PerformanceManager_PowerModeChanged(int idx)
        {
            if (Monitor.TryEnter(powerModeLock))
            {
                // UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PowerModeSlider.Value = idx;
                });

                Monitor.Exit(powerModeLock);
            }
        }

        private void SettingsManager_SettingValueChanged(string name, object value)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (name)
                {
                    case "PerformanceTDPEnabled":
                        TDPToggle.IsOn = Convert.ToBoolean(value);
                        break;
                    case "PerformanceTDPValue":
                        TDPSlider.Value = Convert.ToUInt32(value);
                        break;

                    case "PerformanceGPUEnabled":
                        GPUToggle.IsOn = Convert.ToBoolean(value);
                        break;
                    case "PerformanceGPUValue":
                        GPUSlider.Value = Convert.ToUInt32(value);
                        break;

                    case "FanControlEnabled":
                        FanControlToggle.IsOn = Convert.ToBoolean(value);
                        break;
                    case "FanControlValue":
                        FanControlSlider.Value = Convert.ToUInt32(value);
                        break;
                }
            });
        }

        private void TDPSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
                return;
            SettingsManager.SetProperty("PerformanceTDPValue", (uint)TDPSlider.Value);
        }

        private void TDPToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;
            SettingsManager.SetProperty("PerformanceTDPEnabled", TDPToggle.IsOn);
        }

        private void GPUSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
                return;
            SettingsManager.SetProperty("PerformanceGPUValue", (uint)GPUSlider.Value);
        }

        private void GPUToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;
            SettingsManager.SetProperty("PerformanceGPUEnabled", GPUToggle.IsOn);
        }

        private void PowerModeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // update settings
            int idx = (int)PowerModeSlider.Value;

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (TextBlock tb in PowerModeGrid.Children)
                    tb.SetResourceReference(Control.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");

                TextBlock TextBlock = (TextBlock)PowerModeGrid.Children[idx];
                TextBlock.SetResourceReference(Control.ForegroundProperty, "AccentButtonBackground");
            });

            if (!IsLoaded)
                return;

            if (Monitor.TryEnter(powerModeLock))
            {
                MainWindow.performanceManager.RequestPowerMode(idx);
                Monitor.Exit(powerModeLock);
            }
        }

        private void ComboBoxResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxResolution.SelectedItem is null)
                return;

            ScreenResolution resolution = (ScreenResolution)ComboBoxResolution.SelectedItem;

            ComboBoxFrequency.Items.Clear();
            foreach (ScreenFrequency frequency in resolution.frequencies)
                ComboBoxFrequency.Items.Add(frequency);

            // pick current frequency, if available
            ComboBoxFrequency.SelectedItem = SystemManager.GetScreenFrequency();

            SetResolution();
        }

        private void ComboBoxFrequency_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxFrequency.SelectedItem is null)
                return;

            SetResolution();
        }

        private void SetResolution()
        {
            if (ComboBoxResolution.SelectedItem is null)
                return;

            if (ComboBoxFrequency.SelectedItem is null)
                return;

            ScreenResolution resolution = (ScreenResolution)ComboBoxResolution.SelectedItem;
            ScreenFrequency frequency = (ScreenFrequency)ComboBoxFrequency.SelectedItem;

            // update current screen resolution
            SystemManager.SetResolution(resolution.width, resolution.height, frequency.frequency);
        }

        private void FanControlToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;
            SettingsManager.SetProperty("FanControlEnabled", FanControlToggle.IsOn);
        }

        private void FanControlSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
                return;
            SettingsManager.SetProperty("FanControlValue", (uint)FanControlSlider.Value);
        }
    }
}