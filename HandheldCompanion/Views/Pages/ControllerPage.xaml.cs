using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Devices;
using HandheldCompanion.Managers;
using HandheldCompanion.Utils;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for Devices.xaml
    /// </summary>
    public partial class ControllerPage : Page
    {
        // controllers vars
        private HIDmode controllerMode = HIDmode.NoController;
        private HIDstatus controllerStatus = HIDstatus.Disconnected;

        public event HIDchangedEventHandler HIDchanged;
        public delegate void HIDchangedEventHandler(HIDmode HID);

        public ControllerPage()
        {
            InitializeComponent();

            // initialize components
            foreach (HIDmode mode in ((HIDmode[])Enum.GetValues(typeof(HIDmode))).Where(a => a != HIDmode.NoController))
                cB_HidMode.Items.Add(EnumUtils.GetDescriptionFromEnumValue(mode));

            foreach (HIDstatus status in ((HIDstatus[])Enum.GetValues(typeof(HIDstatus))))
                cB_ServiceSwitch.Items.Add(EnumUtils.GetDescriptionFromEnumValue(status));

            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;

            ControllerManager.ControllerPlugged += ControllerPlugged;
            ControllerManager.ControllerUnplugged += ControllerUnplugged;

            // device specific settings
            Type DeviceType = MainWindow.CurrentDevice.GetType();
            if (DeviceType == typeof(SteamDeck))
                SteamDeckPanel.Visibility = Visibility.Visible;
        }

        public ControllerPage(string Tag) : this()
        {
            this.Tag = Tag;
        }

        private void SettingsManager_SettingValueChanged(string name, object value)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (name)
                {
                    case "HIDcloakonconnect":
                        Toggle_Cloaked.IsOn = Convert.ToBoolean(value);
                        break;
                    case "HIDuncloakonclose":
                        Toggle_Uncloak.IsOn = Convert.ToBoolean(value);
                        break;
                    case "DesktopLayoutEnabled":
                        Toggle_DesktopLayout.IsOn = Convert.ToBoolean(value);
                        break;
                    case "SteamDeckMuteController":
                        Toggle_SDMuteController.IsOn = Convert.ToBoolean(value);
                        break;
                    case "HIDmode":
                        cB_HidMode.SelectedIndex = Convert.ToInt32(value);
                        break;
                    case "HIDstatus":
                        cB_ServiceSwitch.SelectedIndex = Convert.ToInt32(value);
                        UpdateControllerImage();
                        break;
                }
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Page_Closed()
        {
        }

        private void ControllerUnplugged(IController Controller)
        {
            LogManager.LogDebug("Controller unplugged: {0}", Controller.ToString());

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // Search for an existing controller, remove it
                foreach (Border border in InputDevices.Children)
                {
                    // pull controller from panel
                    IController ctrl = (IController)border.Tag;
                    if (ctrl is null)
                        continue;

                    if (ctrl.GetInstancePath() == Controller.GetInstancePath())
                    {
                        InputDevices.Children.Remove(border);
                        break;
                    }
                }

                ControllerRefresh();
            });
        }

        private void ControllerPlugged(IController Controller)
        {
            LogManager.LogDebug("Controller plugged: {0}", Controller.ToString());

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // Add new controller to list if no existing controller was found
                FrameworkElement control = Controller.GetControl();
                InputDevices.Children.Add(control);

                Button ui_button_hook = Controller.GetButtonHook();
                ui_button_hook.Click += (sender, e) => ControllerHookClicked(Controller);

                Button ui_button_hide = Controller.GetButtonHide();
                ui_button_hide.Click += (sender, e) => ControllerHideClicked(Controller);

                ControllerRefresh();
            });
        }

        private void ControllerHookClicked(IController Controller)
        {
            string path = Controller.GetInstancePath();
            ControllerManager.SetTargetController(path);
        }

        private void ControllerHideClicked(IController Controller)
        {
            if (Controller.IsHidden())
                Controller.Unhide();
            else
                Controller.Hide();
        }

        private void ControllerRefresh()
        {
            bool hascontroller = InputDevices.Children.Count != 0;

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                InputDevices.Visibility = hascontroller ? Visibility.Visible : Visibility.Collapsed;
                NoDevices.Visibility = hascontroller ? Visibility.Collapsed : Visibility.Visible;
            });
        }

        private void UpdateControllerImage()
        {
            BitmapImage controllerImage;
            if (controllerMode == HIDmode.NoController || controllerStatus == HIDstatus.Disconnected)
                controllerImage = new BitmapImage(new Uri($"pack://application:,,,/Resources/controller_2_0.png"));
            else
                controllerImage = new BitmapImage(new Uri($"pack://application:,,,/Resources/controller_{Convert.ToInt32(controllerMode)}_{Convert.ToInt32(controllerStatus)}.png"));

            // update UI icon to match HIDmode
            ImageBrush uniformToFillBrush = new ImageBrush()
            {
                Stretch = Stretch.Uniform,
                ImageSource = controllerImage,
            };
            uniformToFillBrush.Freeze();

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                ControllerGrid.Background = uniformToFillBrush;
            });
        }

        private void cB_HidMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cB_HidMode.SelectedIndex == -1)
                return;

            controllerMode = (HIDmode)cB_HidMode.SelectedIndex;
            UpdateControllerImage();

            // raise event
            HIDchanged?.Invoke(controllerMode);

            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("HIDmode", (int)controllerMode);
        }

        private void cB_ServiceSwitch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cB_HidMode.SelectedIndex == -1)
                return;

            controllerStatus = (HIDstatus)cB_ServiceSwitch.SelectedIndex;
            UpdateControllerImage();

            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("HIDstatus", (int)controllerStatus);
        }

        private void Toggle_Cloaked_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("HIDcloakonconnect", Toggle_Cloaked.IsOn);
        }

        private void Toggle_Uncloak_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("HIDuncloakonclose", Toggle_Uncloak.IsOn);
        }

        private void Toggle_SDMuteController_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("SteamDeckMuteController", Toggle_SDMuteController.IsOn);
        }

        private void Button_Layout_Click(object sender, RoutedEventArgs e)
        {
            // prepare layout editor, desktopLayout gets saved automatically
            LayoutTemplate desktopTemplate = new(LayoutManager.GetDesktop())
            {
                Name = LayoutTemplate.DesktopLayout.Name,
                Description = LayoutTemplate.DesktopLayout.Description,
                Author = Environment.UserName,
                Executable = string.Empty,
                Product = string.Empty,  // UI might've set something here, nullify
            };
            MainWindow.layoutPage.UpdateLayout(desktopTemplate);
            MainWindow.NavView_Navigate(MainWindow.layoutPage);
        }

        private void Toggle_DesktopLayout_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            // temporary settings
            SettingsManager.SetProperty("DesktopLayoutEnabled", Toggle_DesktopLayout.IsOn, false, true);
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            ((Expander)sender).BringIntoView();
        }
    }
}
