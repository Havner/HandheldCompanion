using ControllerCommon;
using ControllerCommon.Inputs;
using ControllerCommon.Managers;
using ControllerCommon.Processor;
using ControllerCommon.Utils;
using HandheldCompanion.Controls;
using HandheldCompanion.Managers;
using HandheldCompanion.Views.Pages.Profiles;
using Microsoft.Win32;
using ModernWpf.Controls;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Layout = ControllerCommon.Layout;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for Profiles.xaml
    /// </summary>
    public partial class ProfilesPage : Page
    {
        public static Profile selectedProfile;
        private Hotkey ProfilesPageHotkey = new(60);

        private SettingsMode0 page0 = new SettingsMode0("SettingsMode0");
        private SettingsMode1 page1 = new SettingsMode1("SettingsMode1");

        public ProfilesPage()
        {
            InitializeComponent();
        }

        public ProfilesPage(string Tag) : this()
        {
            this.Tag = Tag;

            ProfileManager.Deleted += ProfileDeleted;
            ProfileManager.Updated += ProfileUpdated;
            ProfileManager.Applied += ProfileApplied;

            ProfileManager.Initialized += ProfileManagerLoaded;

            HotkeysManager.HotkeyCreated += TriggerCreated;
            InputsManager.TriggerUpdated += TriggerUpdated;

            // draw input modes
            foreach (MotionInput mode in (MotionInput[])Enum.GetValues(typeof(MotionInput)))
            {
                // create panel
                SimpleStackPanel panel = new SimpleStackPanel() { Spacing = 6, Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                // create icon
                FontIcon icon = new FontIcon() { Glyph = "" };

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
                TextBlock text = new TextBlock() { Text = description };
                panel.Children.Add(text);

                cB_Input.Items.Add(panel);
            }

            // draw output modes
            foreach (MotionOutput mode in (MotionOutput[])Enum.GetValues(typeof(MotionOutput)))
            {
                // create panel
                SimpleStackPanel panel = new SimpleStackPanel() { Spacing = 6, Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                // create icon
                FontIcon icon = new FontIcon() { Glyph = "" };

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
                TextBlock text = new TextBlock() { Text = description };
                panel.Children.Add(text);

                cB_Output.Items.Add(panel);
            }

            // auto-sort
            cB_Profiles.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Descending));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Page_Closed()
        {
        }

        #region UI
        private void ProfileApplied(Profile profile)
        {
            if (profile.Default)
                return;

            ProfileUpdated(profile, ProfileUpdateSource.Background, true);
        }

        public void ProfileUpdated(Profile profile, ProfileUpdateSource source, bool isCurrent)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                int idx = -1;
                foreach (Profile pr in cB_Profiles.Items)
                {
                    bool isCurrent = pr.Path.Equals(profile.Path, StringComparison.InvariantCultureIgnoreCase);
                    if (isCurrent)
                    {
                        idx = cB_Profiles.Items.IndexOf(pr);
                        break;
                    }
                }

                if (idx != -1)
                    cB_Profiles.Items[idx] = profile;
                else
                    cB_Profiles.Items.Add(profile);

                cB_Profiles.Items.Refresh();

                cB_Profiles.SelectedItem = profile;
            });

            switch (source)
            {
                case ProfileUpdateSource.Background:
                case ProfileUpdateSource.Creation:
                case ProfileUpdateSource.Serializer:
                    return;
            }

            _ = Dialog.ShowAsync($"{Properties.Resources.ProfilesPage_ProfileUpdated1}",
                             $"{profile.Name} {Properties.Resources.ProfilesPage_ProfileUpdated2}",
                             ContentDialogButton.Primary, null, $"{Properties.Resources.ProfilesPage_OK}");
        }

        public void ProfileDeleted(Profile profile)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                int idx = -1;
                foreach (Profile pr in cB_Profiles.Items)
                {
                    bool isCurrent = pr.Path.Equals(profile.Path, StringComparison.InvariantCultureIgnoreCase);
                    if (isCurrent)
                    {
                        idx = cB_Profiles.Items.IndexOf(pr);
                        break;
                    }
                }
                cB_Profiles.Items.RemoveAt(idx);
            });
        }

        private void ProfileManagerLoaded()
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                cB_Profiles.SelectedItem = ProfileManager.GetDefault();
            });
        }
        #endregion

        private async void b_CreateProfile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var path = openFileDialog.FileName;
                    var folder = Path.GetDirectoryName(path);

                    var file = openFileDialog.SafeFileName;
                    var ext = Path.GetExtension(file);

                    switch (ext)
                    {
                        default:
                        case ".exe":
                            break;
                        case ".xml":
                            try
                            {
                                XmlDocument doc = new XmlDocument();
                                doc.Load(path);

                                XmlNodeList Applications = doc.GetElementsByTagName("Applications");
                                foreach (XmlNode node in Applications)
                                {
                                    foreach (XmlNode child in node.ChildNodes)
                                    {
                                        if (child.Name.Equals("Application"))
                                        {
                                            if (child.Attributes is not null)
                                            {
                                                foreach (XmlAttribute attribute in child.Attributes)
                                                {
                                                    switch (attribute.Name)
                                                    {
                                                        case "Executable":
                                                            path = Path.Combine(folder, attribute.InnerText);
                                                            file = Path.GetFileName(path);
                                                            break;
                                                    }
                                                    continue;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogManager.LogError(ex.Message, true);
                            }
                            break;
                    }

                    Profile profile = new Profile(path);
                    profile.Layout = LayoutTemplate.DefaultLayout.Layout.Clone() as Layout;
                    profile.LayoutTitle = LayoutTemplate.DefaultLayout.Name;

                    bool exists = false;

                    // check on path rather than profile
                    if (ProfileManager.Contains(path))
                    {
                        Task<ContentDialogResult> result = Dialog.ShowAsync(
                            String.Format(Properties.Resources.ProfilesPage_AreYouSureOverwrite1, profile.Name),
                            String.Format(Properties.Resources.ProfilesPage_AreYouSureOverwrite2, profile.Name),
                            ContentDialogButton.Primary,
                            $"{Properties.Resources.ProfilesPage_Cancel}",
                            $"{Properties.Resources.ProfilesPage_Yes}");

                        await result; // sync call

                        switch (result.Result)
                        {
                            case ContentDialogResult.Primary:
                                exists = false;
                                break;
                            default:
                                exists = true;
                                break;
                        }
                    }

                    if (!exists)
                        ProfileManager.UpdateOrCreateProfile(profile, ProfileUpdateSource.Creation);
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex.Message);
                }
            }
        }

        private void b_AdditionalSettings_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProfile is null)
                return;

            switch ((MotionInput)cB_Input.SelectedIndex)
            {
                default:
                case MotionInput.JoystickCamera:
                case MotionInput.PlayerSpace:
                    page0.SetProfile();
                    MainWindow.NavView_Navigate(page0);
                    break;
                case MotionInput.JoystickSteering:
                    page1.SetProfile();
                    MainWindow.NavView_Navigate(page1);
                    break;
            }
        }

        private void cB_Profiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cB_Profiles.SelectedItem is null)
                return;

            // update current profile
            Profile profile = (Profile)cB_Profiles.SelectedItem;
            selectedProfile = profile.Clone() as Profile;

            // todo: find a way to avoid a useless circle of drawing when profile was update from ProfilesPage
            DrawProfile();
        }

        private void DrawProfile()
        {
            if (selectedProfile is null)
                return;

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // enable all expanders
                ProfileDetails.IsEnabled = true;
                MotionSettings.IsEnabled = true;
                UniversalSettings.IsEnabled = true;

                // disable button if is default profile or application is running
                b_DeleteProfile.IsEnabled = !selectedProfile.ErrorCode.HasFlag(ProfileErrorCode.Default & ProfileErrorCode.Running);

                // prevent user from renaming default profile
                tB_ProfileName.IsEnabled = !selectedProfile.Default;
                // prevent user from disabling default profile
                Toggle_EnableProfile.IsEnabled = !selectedProfile.Default;

                // Profile info
                tB_ProfileName.Text = selectedProfile.Name;
                tB_ProfilePath.Text = selectedProfile.Path;
                Toggle_EnableProfile.IsOn = selectedProfile.Enabled;

                // Motion control settings
                tb_ProfileGyroValue.Value = selectedProfile.GyrometerMultiplier;
                tb_ProfileAcceleroValue.Value = selectedProfile.AccelerometerMultiplier;

                cB_GyroSteering.SelectedIndex = selectedProfile.SteeringAxis;
                cB_InvertHorizontal.IsChecked = selectedProfile.MotionInvertHorizontal;
                cB_InvertVertical.IsChecked = selectedProfile.MotionInvertVertical;

                // Layout settings
                Toggle_ControllerLayout.IsOn = selectedProfile.LayoutEnabled;

                // UMC settings
                Toggle_UniversalMotion.IsOn = selectedProfile.MotionEnabled;
                cB_Input.SelectedIndex = (int)selectedProfile.MotionInput;
                cB_Output.SelectedIndex = (int)selectedProfile.MotionOutput;
                tb_ProfileUMCAntiDeadzone.Value = selectedProfile.MotionAntiDeadzone;
                cB_UMC_MotionDefaultOffOn.SelectedIndex = (int)selectedProfile.MotionMode;

                // todo: improve me ?
                ProfilesPageHotkey.inputsChord.State = selectedProfile.MotionTrigger.Clone() as ButtonState;
                ProfilesPageHotkey.DrawInput();

                // display warnings
                switch (selectedProfile.ErrorCode)
                {
                    default:
                    case ProfileErrorCode.None:
                        WarningBorder.Visibility = Visibility.Collapsed;
                        break;

                    case ProfileErrorCode.Running:
                    case ProfileErrorCode.MissingExecutable:
                    case ProfileErrorCode.MissingPath:
                    case ProfileErrorCode.MissingPermission:
                    case ProfileErrorCode.Default:
                        WarningBorder.Visibility = Visibility.Visible;
                        WarningContent.Text = EnumUtils.GetDescriptionFromEnumValue(selectedProfile.ErrorCode);
                        break;
                }
            });
        }

        private async void b_DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProfile is null)
                return;

            Task<ContentDialogResult> result = Dialog.ShowAsync($"{Properties.Resources.ProfilesPage_AreYouSureDelete1} \"{selectedProfile.Name}\"?",
                                                                $"{Properties.Resources.ProfilesPage_AreYouSureDelete2}",
                                                                ContentDialogButton.Primary,
                                                                $"{Properties.Resources.ProfilesPage_Cancel}",
                                                                $"{Properties.Resources.ProfilesPage_Delete}");
            await result; // sync call

            switch (result.Result)
            {
                case ContentDialogResult.Primary:
                    ProfileManager.DeleteProfile(selectedProfile);
                    cB_Profiles.SelectedIndex = 0;
                    break;
                default:
                    break;
            }
        }

        private void b_ApplyProfile_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProfile is null)
                return;

            // Profile
            selectedProfile.Name = tB_ProfileName.Text;
            selectedProfile.Path = tB_ProfilePath.Text;
            selectedProfile.Enabled = (bool)Toggle_EnableProfile.IsOn;

            // Motion control settings
            selectedProfile.GyrometerMultiplier = (float)tb_ProfileGyroValue.Value;
            selectedProfile.AccelerometerMultiplier = (float)tb_ProfileAcceleroValue.Value;

            selectedProfile.SteeringAxis = cB_GyroSteering.SelectedIndex;
            selectedProfile.MotionInvertVertical = (bool)cB_InvertVertical.IsChecked;
            selectedProfile.MotionInvertHorizontal = (bool)cB_InvertHorizontal.IsChecked;

            // UMC settings
            selectedProfile.MotionEnabled = (bool)Toggle_UniversalMotion.IsOn;
            selectedProfile.MotionInput = (MotionInput)cB_Input.SelectedIndex;
            selectedProfile.MotionOutput = (MotionOutput)cB_Output.SelectedIndex;
            selectedProfile.MotionAntiDeadzone = (float)tb_ProfileUMCAntiDeadzone.Value;
            selectedProfile.MotionMode = (MotionMode)cB_UMC_MotionDefaultOffOn.SelectedIndex;

            // Layout settings
            selectedProfile.LayoutEnabled = (bool)Toggle_ControllerLayout.IsOn;

            ProfileManager.UpdateOrCreateProfile(selectedProfile, ProfileUpdateSource.ProfilesPage);
        }

        private void cB_Overlay_Checked(object sender, RoutedEventArgs e)
        {
            // do something
        }

        private void cB_Wrapper_Checked(object sender, RoutedEventArgs e)
        {
            // do something
        }

        private void cB_EnableHook_Checked(object sender, RoutedEventArgs e)
        {
            // do something
        }

        private void cB_ExclusiveHook_Checked(object sender, RoutedEventArgs e)
        {
            // do something
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            ((Expander)sender).BringIntoView();
        }

        private void Toggle_EnableProfile_Toggled(object sender, RoutedEventArgs e)
        {
            // do something
        }

        private void cB_Input_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                case MotionInput.AutoRollYawSwap:
                    cB_Output.SelectedIndex = (int)MotionOutput.RightStick;
                    break;
                case MotionInput.JoystickSteering:
                    cB_Output.SelectedIndex = (int)MotionOutput.LeftStick;
                    break;
            }

            Text_InputHint.Text = Profile.InputDescription[input];
        }

        private void cB_UMC_MotionDefaultOffOn_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (cB_UMC_MotionDefaultOffOn.SelectedIndex == -1)
                return;
        }

        private void TriggerCreated(Hotkey hotkey)
        {
            switch (hotkey.inputsHotkey.Listener)
            {
                case "shortcutProfilesPage@":
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

        private void TriggerUpdated(string listener, InputsChord inputs, InputsManager.ListenerType type)
        {
            switch (listener)
            {
                case "shortcutProfilesPage@":
                case "shortcutProfilesPage@@":
                    selectedProfile.MotionTrigger = inputs.State.Clone() as ButtonState;
                    break;
            }
        }

        private void ControllerSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // prepare layout editor
            LayoutTemplate layoutTemplate = new(selectedProfile.Layout)
            {
                Name = selectedProfile.LayoutTitle,
                Description = "Your modified layout for this executable.",
                Author = Environment.UserName,
                Executable = selectedProfile.Executable,
                Product = selectedProfile.Name,
            };
            layoutTemplate.Updated += Template_Updated;

            MainWindow.layoutPage.UpdateLayout(layoutTemplate);
            MainWindow.NavView_Navigate(MainWindow.layoutPage);
        }

        private void Template_Updated(LayoutTemplate layoutTemplate)
        {
            selectedProfile.LayoutTitle = layoutTemplate.Name;
        }
    }
}
