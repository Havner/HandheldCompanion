using ControllerCommon;
using ControllerCommon.Inputs;
using ControllerCommon.Utils;
using HandheldCompanion.Controls;
using HandheldCompanion.Managers;
using ModernWpf.Controls;
using System;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Layout = ControllerCommon.Layout;
using Page = System.Windows.Controls.Page;
using Timer = System.Timers.Timer;

namespace HandheldCompanion.Views.QuickPages
{
    /// <summary>
    /// Interaction logic for QuickProfilesPage.xaml
    /// </summary>
    public partial class QuickProfilesPage : Page
    {
        private ProcessEx? currentProcess;
        private Profile? currentProfile;
        private Hotkey ProfilesPageHotkey = new(61);

        private const int UpdateInterval = 500;
        private Timer UpdateTimer;

        private object updateLock = new();

        public QuickProfilesPage()
        {
            InitializeComponent();

            // Those are the only events QuickProfiles needs to work.
            // What is the current process and what is the current profile.
            // Applied is also sent when the current profile is updated or deleted.
            ProcessManager.ForegroundChanged += ProcessManager_ForegroundChanged;
            ProfileManager.Applied += ProfileManager_Applied;

            HotkeysManager.HotkeyCreated += HotkeysManager_HotkeyCreated;
            InputsManager.TriggerUpdated += InputsManager_TriggerUpdated;

            foreach (MotionInput mode in (MotionInput[])Enum.GetValues(typeof(MotionInput)))
            {
                // create panel
                SimpleStackPanel panel = new() { Spacing = 6, Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                // create icon
                FontIcon icon = new() { Glyph = "" };

                switch (mode)
                {
                    default:
                    case MotionInput.PlayerSpace:
                        icon.Glyph = "\uF119";
                        break;
                    case MotionInput.JoystickCamera:
                        icon.Glyph = "\uE714";
                        break;
                    case MotionInput.AutoRollYawSwap:
                        icon.Glyph = "\uE7F8";
                        break;
                    case MotionInput.JoystickSteering:
                        icon.Glyph = "\uEC47";
                        break;
                }

                if (icon.Glyph != "")
                    panel.Children.Add(icon);

                // create textblock
                string description = EnumUtils.GetDescriptionFromEnumValue(mode);
                TextBlock text = new() { Text = description };
                panel.Children.Add(text);

                cB_Input.Items.Add(panel);
            }

            foreach (MotionOutput mode in (MotionOutput[])Enum.GetValues(typeof(MotionOutput)))
            {
                // create panel
                SimpleStackPanel panel = new() { Spacing = 6, Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                // create icon
                FontIcon icon = new() { Glyph = "" };

                switch (mode)
                {
                    default:
                    case MotionOutput.RightStick:
                        icon.Glyph = "\uF109";
                        break;
                    case MotionOutput.LeftStick:
                        icon.Glyph = "\uF108";
                        break;
                }

                if (icon.Glyph != "")
                    panel.Children.Add(icon);

                // create textblock
                string description = EnumUtils.GetDescriptionFromEnumValue(mode);
                TextBlock text = new() { Text = description };
                panel.Children.Add(text);

                cB_Output.Items.Add(panel);
            }

            UpdateTimer = new(UpdateInterval);
            UpdateTimer.AutoReset = false;
            UpdateTimer.Elapsed += UpdateTimer_Elapsed;
        }

        private void UpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (currentProfile is null)
                return;

            ProfileManager.UpdateOrCreateProfile(currentProfile, ProfileUpdateSource.QuickProfilesPage);
        }

        // the visibility of this button depends on both, process and profile
        private Visibility GetCreateProfileVisibility()
        {
            Visibility vis;
            if (currentProcess is null)
                vis = Visibility.Collapsed;
            else if (currentProfile is null)
                vis = Visibility.Visible;
            else
                vis = Visibility.Collapsed;
            return vis;
        }

        private void UpdateProfileContent()
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (currentProfile is null)
                {
                    b_CreateProfile.Visibility = GetCreateProfileVisibility();
                    GridProfile.Visibility = Visibility.Collapsed;
                }
                else
                {
                    b_CreateProfile.Visibility = Visibility.Collapsed;
                    GridProfile.Visibility = Visibility.Visible;

                    ProfileToggle.IsOn = currentProfile.Enabled;
                    UMCToggle.IsOn = currentProfile.MotionEnabled;
                    cB_Input.SelectedIndex = (int)currentProfile.MotionInput;
                    cB_Output.SelectedIndex = (int)currentProfile.MotionOutput;
                    cB_UMC_MotionDefaultOffOn.SelectedIndex = (int)currentProfile.MotionMode;

                    // Slider settings
                    SliderUMCAntiDeadzone.Value = currentProfile.MotionAntiDeadzone;
                    SliderSensitivityX.Value = currentProfile.MotionSensivityX;
                    SliderSensitivityY.Value = currentProfile.MotionSensivityY;

                    // todo: improve me ?
                    ProfilesPageHotkey.inputsChord.State = currentProfile.MotionTrigger.Clone() as ButtonState;
                    ProfilesPageHotkey.DrawInput();
                }
            });
        }

        private void UpdateProcessContent()
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (currentProcess is not null)
                {
                    ProcessIcon.Source = currentProcess.imgSource;
                    ProcessName.Text = currentProcess.Executable;
                    ProcessPath.Text = currentProcess.Path;

                    // disable create button if process is bypassed
                    b_CreateProfile.IsEnabled = currentProcess.Filter == ProcessEx.ProcessFilter.Allowed;
                    b_CreateProfile.Visibility = GetCreateProfileVisibility();
                }
                else
                {
                    ProcessIcon.Source = null;
                    ProcessName.Text = Properties.Resources.QuickProfilesPage_Waiting;
                    ProcessPath.Text = string.Empty;

                    b_CreateProfile.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void ProfileManager_Applied(Profile profile, ProfileUpdateSource source)
        {
            if (source == ProfileUpdateSource.QuickProfilesPage)
                return;

            if (Monitor.TryEnter(updateLock))
            {
                // if an update is pending, execute it and stop timer
                if (UpdateTimer.Enabled)
                {
                    UpdateTimer.Stop();
                    ProfileManager.UpdateOrCreateProfile(currentProfile, ProfileUpdateSource.QuickProfilesPage);
                }

                // don't store or display the default profile
                if (!profile.Default)
                    currentProfile = profile.Clone() as Profile;
                else
                    currentProfile = null;
                UpdateProfileContent();

                Monitor.Exit(updateLock);
            }
        }

        private void ProcessManager_ForegroundChanged(ProcessEx process)
        {
            currentProcess = process;
            UpdateProcessContent();
        }

        private void RequestUpdate()
        {
            UpdateTimer.Stop();
            UpdateTimer.Start();
        }

        private void ProfileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (currentProfile is null)
                return;

            if (Monitor.TryEnter(updateLock))
            {
                currentProfile.Enabled = ProfileToggle.IsOn;
                RequestUpdate();

                Monitor.Exit(updateLock);
            }
        }

        private void UMCToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (currentProfile is null)
                return;

            if (Monitor.TryEnter(updateLock))
            {
                currentProfile.MotionEnabled = UMCToggle.IsOn;
                RequestUpdate();

                Monitor.Exit(updateLock);
            }
        }

        private void Input_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cB_Input.SelectedIndex == -1)
                return;

            MotionInput input = (MotionInput)cB_Input.SelectedIndex;

            // Check which input type is selected and automatically
            // set the most used output joystick accordingly.
            switch (input)
            {
                case MotionInput.PlayerSpace:
                case MotionInput.JoystickCamera:
                    cB_Output.SelectedIndex = (int)MotionOutput.RightStick;
                    GridSensivity.Visibility = Visibility.Visible;
                    break;
                case MotionInput.JoystickSteering:
                    cB_Output.SelectedIndex = (int)MotionOutput.LeftStick;
                    GridSensivity.Visibility = Visibility.Collapsed;
                    break;
            }

            Text_InputHint.Text = Profile.InputDescription[input];

            if (currentProfile is null)
                return;

            if (Monitor.TryEnter(updateLock))
            {
                currentProfile.MotionInput = (MotionInput)cB_Input.SelectedIndex;
                RequestUpdate();

                Monitor.Exit(updateLock);
            }
        }

        private void Output_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentProfile is null)
                return;

            if (Monitor.TryEnter(updateLock))
            {
                currentProfile.MotionOutput = (MotionOutput)cB_Output.SelectedIndex;
                RequestUpdate();

                Monitor.Exit(updateLock);
            }
        }

        private void CreateProfile_Click(object sender, RoutedEventArgs e)
        {
            if (currentProcess is null)
                return;

            if (UpdateTimer.Enabled)
                UpdateTimer.Stop();

            // create profile
            currentProfile = new(currentProcess.Path)
            {
                Layout = LayoutTemplate.DefaultLayout.Layout.Clone() as Layout,
                LayoutTitle = LayoutTemplate.DesktopLayout.Name
            };

            ProfileManager.UpdateOrCreateProfile(currentProfile, ProfileUpdateSource.Creation);
        }

        private void SliderUMCAntiDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentProfile is null)
                return;

            if (Monitor.TryEnter(updateLock))
            {
                currentProfile.MotionAntiDeadzone = (int)SliderUMCAntiDeadzone.Value;
                RequestUpdate();

                Monitor.Exit(updateLock);
            }
        }

        private void SliderSensitivityX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentProfile is null)
                return;

            if (Monitor.TryEnter(updateLock))
            {
                currentProfile.MotionSensivityX = (float)SliderSensitivityX.Value;
                RequestUpdate();

                Monitor.Exit(updateLock);
            }
        }

        private void SliderSensitivityY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentProfile is null)
                return;

            if (Monitor.TryEnter(updateLock))
            {
                currentProfile.MotionSensivityY = (float)SliderSensitivityY.Value;
                RequestUpdate();

                Monitor.Exit(updateLock);
            }
        }

        private void HotkeysManager_HotkeyCreated(Hotkey hotkey)
        {
            switch (hotkey.inputsHotkey.Listener)
            {
                case "shortcutProfilesPage@@":
                    {
                        HotkeyControl hotkeyBorder = hotkey.GetControl();
                        if (hotkeyBorder is null || hotkeyBorder.Parent is not null)
                            return;

                        // pull hotkey
                        ProfilesPageHotkey = hotkey;

                        this.UMC_Activator.Children.Add(hotkeyBorder);
                    }
                    break;
            }
        }

        private void InputsManager_TriggerUpdated(string listener, InputsChord inputs, InputsManager.ListenerType type)
        {
            if (currentProfile is null)
                return;

            // no Monitor on threaded calls ?
            switch (listener)
            {
                case "shortcutProfilesPage@":
                case "shortcutProfilesPage@@":
                    currentProfile.MotionTrigger = inputs.State.Clone() as ButtonState;
                    RequestUpdate();
                    break;
            }
        }

        private void UMC_MotionDefaultOffOn_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (cB_UMC_MotionDefaultOffOn.SelectedIndex == -1 || currentProfile is null)
                return;

            if (Monitor.TryEnter(updateLock))
            {
                currentProfile.MotionMode = (MotionMode)cB_UMC_MotionDefaultOffOn.SelectedIndex;
                RequestUpdate();

                Monitor.Exit(updateLock);
            }
        }
    }
}
