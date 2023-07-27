using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.Pages.Profiles
{
    /// <summary>
    /// Interaction logic for SettingsMode0.xaml
    /// </summary>
    public partial class SettingsMode0 : Page
    {
        private Hotkey ProfilesPageHotkey;

        private LockObject updateLock = new();

        public SettingsMode0()
        {
            InitializeComponent();
        }

        public SettingsMode0(string Tag) : this()
        {
            this.Tag = Tag;

            MotionManager.SettingsMode0Update += MotionManager_SettingsMode0Update;

            HotkeysManager.HotkeyCreated += HotkeysManager_HotkeyCreated;
            InputsManager.TriggerUpdated += InputsManager_TriggerUpdated;
        }

        public void SetProfile()
        {
            using (new ScopedLock(updateLock))
            {
                SliderSensitivityX.Value = ProfilesPage.selectedProfile.MotionSensivityX;
                SliderSensitivityY.Value = ProfilesPage.selectedProfile.MotionSensivityY;
                Toggle_StackCurveEnabled.IsOn = ProfilesPage.selectedProfile.MotionSensivityArrayEnabled;
                tb_ProfileAimingDownSightsMultiplier.Value = ProfilesPage.selectedProfile.AimingSightsMultiplier;

                // todo: improve me ?
                ProfilesPageHotkey.inputsChord.State = ProfilesPage.selectedProfile.AimingSightsTrigger.Clone() as ButtonState;
                ProfilesPageHotkey.DrawInput();

                // temp
                StackCurve.Children.Clear();
                foreach (var elem in ProfilesPage.selectedProfile.MotionSensivityArray)
                {
                    // skip first item ?
                    if (elem.Key == 0)
                        continue;

                    double height = elem.Value * StackCurve.Height;
                    Thumb thumb = new Thumb()
                    {
                        Tag = elem.Key,
                        Width = 8,
                        MaxHeight = StackCurve.Height,
                        Height = height,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Background = (Brush)Application.Current.Resources["SystemControlHighlightAltListAccentLowBrush"],
                        BorderThickness = new Thickness(0),
                        BorderBrush = (Brush)Application.Current.Resources["SystemControlHighlightAltListAccentHighBrush"],
                        IsEnabled = false // prevent the control from being clickable
                    };

                    StackCurve.Children.Add(thumb);
                }

                StackCurvePanel.Visibility = ProfilesPage.selectedProfile.MotionSensivityArrayEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Page_Closed()
        {
        }

        private void MotionManager_SettingsMode0Update(Vector3 gyrometer)
        {
            Highlight_Thumb(Math.Max(Math.Max(Math.Abs(gyrometer.Z), Math.Abs(gyrometer.X)), Math.Abs(gyrometer.Y)));
        }

        private void SliderSensitivityX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || updateLock)
                return;

            ProfilesPage.selectedProfile.MotionSensivityX = (float)SliderSensitivityX.Value;
            ProfilesPage.RequestUpdate();
        }

        private void SliderSensitivityY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || updateLock)
                return;

            ProfilesPage.selectedProfile.MotionSensivityY = (float)SliderSensitivityY.Value;
            ProfilesPage.RequestUpdate();
        }

        private void Toggle_StackCurveEnabled_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || updateLock)
                return;

            ProfilesPage.selectedProfile.MotionSensivityArrayEnabled = Toggle_StackCurveEnabled.IsOn;
            StackCurvePanel.Visibility = ProfilesPage.selectedProfile.MotionSensivityArrayEnabled ? Visibility.Visible : Visibility.Collapsed;
            ProfilesPage.RequestUpdate();
        }

        private void Highlight_Thumb(float value)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                double dist_x = value / MotionManager.gyroscopeSpec.maxIn;

                foreach (Control control in StackCurve.Children)
                {
                    double x = (double)control.Tag;

                    if (dist_x > x)
                        control.BorderThickness = new Thickness(0, 0, 0, 20);
                    else
                        control.BorderThickness = new Thickness(0);
                }
            });
        }

        private void StackCurve_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StackCurve_MouseMove(sender, (MouseEventArgs)e);
        }

        private void StackCurve_MouseMove(object sender, MouseEventArgs e)
        {
            Control Thumb = null;

            foreach (Control control in StackCurve.Children)
            {
                Point position = e.GetPosition(control);
                double dist_x = Math.Abs(position.X);

                control.Background = (Brush)Application.Current.Resources["SystemControlHighlightAltListAccentLowBrush"];

                if (dist_x <= control.Width)
                    Thumb = control;
            }

            if (Thumb is null)
                return;

            Thumb.Background = (Brush)Application.Current.Resources["SystemControlHighlightAltListAccentHighBrush"];

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double x = (double)Thumb.Tag;
                Thumb.Height = StackCurve.ActualHeight - e.GetPosition(StackCurve).Y;
                ProfilesPage.selectedProfile.MotionSensivityArray[x] = Thumb.Height / StackCurve.Height;
                ProfilesPage.RequestUpdate();
            }
        }

        private void Button_Click_Default(object sender, RoutedEventArgs e)
        {
            // default preset
            foreach (Control Thumb in StackCurve.Children)
            {
                double x = (double)Thumb.Tag;
                Thumb.Height = StackCurve.Height / 2.0f;
                ProfilesPage.selectedProfile.MotionSensivityArray[x] = Thumb.Height / StackCurve.Height;
                ProfilesPage.RequestUpdate();
            }
        }

        private void Button_Click_Aggressive(object sender, RoutedEventArgs e)
        {
            // agressive preset
            float tempx = 24f / Profile.SensivityArraySize;
            foreach (Control Thumb in StackCurve.Children)
            {
                double x = (double)Thumb.Tag;
                float value = (float)(-Math.Sqrt(x * tempx) + 0.85f);

                Thumb.Height = StackCurve.Height * value;
                ProfilesPage.selectedProfile.MotionSensivityArray[x] = Thumb.Height / StackCurve.Height;
                ProfilesPage.RequestUpdate();
            }
        }

        private void Button_Click_Precise(object sender, RoutedEventArgs e)
        {
            // precise preset
            float tempx = 12f / Profile.SensivityArraySize;
            foreach (Control Thumb in StackCurve.Children)
            {
                double x = (double)Thumb.Tag;
                float value = (float)(Math.Sqrt(x * tempx) + 0.25f - (tempx * x));

                Thumb.Height = StackCurve.Height * value;
                ProfilesPage.selectedProfile.MotionSensivityArray[x] = Thumb.Height / StackCurve.Height;
                ProfilesPage.RequestUpdate();
            }
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            ((Expander)sender).BringIntoView();
        }

        private void SliderAimingDownSightsMultiplier_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || updateLock)
                return;

            ProfilesPage.selectedProfile.AimingSightsMultiplier = (float)tb_ProfileAimingDownSightsMultiplier.Value;
            ProfilesPage.RequestUpdate();
        }

        private void HotkeysManager_HotkeyCreated(Hotkey hotkey)
        {
            switch (hotkey.inputsHotkey.Listener)
            {
                case "shortcutProfilesSettingsMode0":
                    {
                        // pull hotkey
                        ProfilesPageHotkey = hotkey;

                        // add to UI
                        HotkeyControl hotkeyBorder = ProfilesPageHotkey.GetControl();
                        if (hotkeyBorder is null || hotkeyBorder.Parent is not null)
                            return;

                        if (AimingSights_Activator.Children.Count == 0)
                            AimingSights_Activator.Children.Add(hotkeyBorder);
                    }
                    break;
            }
        }

        private void InputsManager_TriggerUpdated(string listener, InputsChord inputs, InputsManager.ListenerType type)
        {
            switch (listener)
            {
                case "shortcutProfilesSettingsMode0":
                    ProfilesPage.selectedProfile.AimingSightsTrigger = inputs.State.Clone() as ButtonState;
                    ProfilesPage.RequestUpdate();
                    break;
            }
        }
    }
}
