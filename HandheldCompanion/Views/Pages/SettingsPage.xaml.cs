using ControllerCommon.Utils;
using HandheldCompanion.Managers;
using ModernWpf;
using System;
using System.Linq;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;
using Page = System.Windows.Controls.Page;
using ServiceControllerStatus = ControllerCommon.Managers.ServiceControllerStatus;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();

            // initialize components
            foreach (ServiceStartMode mode in ((ServiceStartMode[])Enum.GetValues(typeof(ServiceStartMode))).Where(mode => mode >= ServiceStartMode.Automatic))
            {
                RadioButton radio = new() { Content = EnumUtils.GetDescriptionFromEnumValue(mode) };
                switch (mode)
                {
                    case ServiceStartMode.Disabled:
                        radio.IsEnabled = false;
                        break;
                }

                cB_StartupType.Items.Add(radio);
            }

            // initialize manager(s)
            MainWindow.serviceManager.Updated += OnServiceUpdate;
            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;
        }

        public SettingsPage(string? Tag) : this()
        {
            this.Tag = Tag;
        }

        private void SettingsManager_SettingValueChanged(string? name, object value)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (name)
                {
                    case "MainWindowTheme":
                        cB_Theme.SelectedIndex = Convert.ToInt32(value);
                        cB_Theme_SelectionChanged(this, null); // bug: SelectionChanged not triggered when control isn't loaded
                        break;
                    case "RunAtStartup":
                        Toggle_AutoStart.IsOn = Convert.ToBoolean(value);
                        break;
                    case "StartMinimized":
                        Toggle_Background.IsOn = Convert.ToBoolean(value);
                        break;
                    case "CloseMinimises":
                        Toggle_CloseMinimizes.IsOn = Convert.ToBoolean(value);
                        break;
                    case "DesktopProfileOnStart":
                        Toggle_DesktopProfileOnStart.IsOn = Convert.ToBoolean(value);
                        break;
                    case "ToastEnable":
                        Toggle_Notification.IsOn = Convert.ToBoolean(value);
                        break;
                    case "StartServiceWithCompanion":
                        Toggle_ServiceStartup.IsOn = Convert.ToBoolean(value);
                        break;
                    case "HaltServiceWithCompanion":
                        Toggle_ServiceShutdown.IsOn = Convert.ToBoolean(value);
                        break;
                    case "ServiceStartMode":
                        cB_StartupType.SelectedIndex = Convert.ToInt32(value);
                        cB_StartupType_SelectionChanged(this, null); // bug: SelectionChanged not triggered when control isn't loaded
                        break;
                }
            });
        }

        private void Page_Loaded(object? sender, RoutedEventArgs? e)
        {
        }

        public void Page_Closed()
        {
            MainWindow.serviceManager.Updated -= OnServiceUpdate;
        }

        private void Toggle_AutoStart_Toggled(object? sender, System.Windows.RoutedEventArgs? e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("RunAtStartup", Toggle_AutoStart.IsOn);
        }

        private void Toggle_Background_Toggled(object? sender, System.Windows.RoutedEventArgs? e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("StartMinimized", Toggle_Background.IsOn);
        }

        private void cB_StartupType_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
        {
            if (cB_StartupType.SelectedIndex == -1)
                return;

            ServiceStartMode mode;
            switch (cB_StartupType.SelectedIndex)
            {
                case 0:
                    mode = ServiceStartMode.Automatic;
                    break;
                default:
                case 1:
                    mode = ServiceStartMode.Manual;
                    break;
                case 2:
                    mode = ServiceStartMode.Disabled;
                    break;
            }

            MainWindow.serviceManager.SetStartType(mode);

            // service was not found
            if (!cB_StartupType.IsEnabled)
                return;

            SettingsManager.SetProperty("ServiceStartMode", cB_StartupType.SelectedIndex);
        }

        private void Toggle_CloseMinimizes_Toggled(object? sender, System.Windows.RoutedEventArgs? e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("CloseMinimises", Toggle_CloseMinimizes.IsOn);
        }

        private void Toggle_DesktopProfileOnStart_Toggled(object? sender, System.Windows.RoutedEventArgs? e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("DesktopProfileOnStart", Toggle_DesktopProfileOnStart.IsOn);
        }

        private void Toggle_ServiceShutdown_Toggled(object? sender, System.Windows.RoutedEventArgs? e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("HaltServiceWithCompanion", Toggle_ServiceShutdown.IsOn);
        }

        private void Toggle_ServiceStartup_Toggled(object? sender, System.Windows.RoutedEventArgs? e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("StartServiceWithCompanion", Toggle_ServiceStartup.IsOn);
        }

        private void Toggle_Notification_Toggled(object? sender, System.Windows.RoutedEventArgs? e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("ToastEnable", Toggle_Notification.IsOn);
        }

        private void cB_Theme_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
        {
            if (cB_Theme.SelectedIndex == -1)
                return;

            ThemeManager.Current.ApplicationTheme = (ApplicationTheme)cB_Theme.SelectedIndex;

            // update default style
            MainWindow.GetCurrent().UpdateDefaultStyle();
            MainWindow.overlayquickTools.UpdateDefaultStyle();

            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("MainWindowTheme", cB_Theme.SelectedIndex);
        }

        #region serviceManager
        private void OnServiceUpdate(ServiceControllerStatus status, int mode)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (status)
                {
                    case ServiceControllerStatus.Paused:
                    case ServiceControllerStatus.Stopped:
                    case ServiceControllerStatus.Running:
                    case ServiceControllerStatus.ContinuePending:
                    case ServiceControllerStatus.PausePending:
                    case ServiceControllerStatus.StartPending:
                    case ServiceControllerStatus.StopPending:
                        cB_StartupType.IsEnabled = true;
                        break;
                    default:
                        cB_StartupType.IsEnabled = false;
                        break;
                }

                if (mode != -1)
                {
                    ServiceStartMode serviceMode = (ServiceStartMode)mode;
                    switch (serviceMode)
                    {
                        case ServiceStartMode.Automatic:
                            cB_StartupType.SelectedIndex = 0;
                            break;
                        default:
                        case ServiceStartMode.Manual:
                            cB_StartupType.SelectedIndex = 1;
                            break;
                        case ServiceStartMode.Disabled:
                            cB_StartupType.SelectedIndex = 2;
                            break;
                    }

                    // only allow users to set those options when service mode is set to Manual
                    Toggle_ServiceStartup.IsEnabled = (serviceMode != ServiceStartMode.Automatic);
                    Toggle_ServiceShutdown.IsEnabled = (serviceMode != ServiceStartMode.Automatic);
                }
            });
        }
        #endregion
    }
}
