using HandheldCompanion.Managers;
using ModernWpf;
using System;
using System.Windows;
using System.Windows.Controls;
using Page = System.Windows.Controls.Page;

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

            // initialize manager(s)
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
                }
            });
        }

        private void Page_Loaded(object? sender, RoutedEventArgs? e)
        {
        }

        public void Page_Closed()
        {
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
    }
}
