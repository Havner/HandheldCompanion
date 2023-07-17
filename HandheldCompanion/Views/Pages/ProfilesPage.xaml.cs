using HandheldCompanion.Actions;
using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
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
using Layout = HandheldCompanion.Misc.Layout;
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

        private SettingsMode0 page0 = new("SettingsMode0");
        private SettingsMode1 page1 = new("SettingsMode1");

        public ProfilesPage()
        {
            InitializeComponent();
        }

        public ProfilesPage(string Tag) : this()
        {
            this.Tag = Tag;

            // ProfilesPage doesn't care what is the currently applied profile.
            ProfileManager.Initialized += ProfileManager_Initialized;
            ProfileManager.Updated += ProfileManager_Updated;
            ProfileManager.Deleted += ProfileManager_Deleted;

            HotkeysManager.HotkeyCreated += HotkeysManager_HotkeyCreated;
            InputsManager.TriggerUpdated += InputsManager_TriggerUpdated;

            // draw input modes
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

            // auto-sort
            cB_Profiles.Items.SortDescriptions.Add(new SortDescription("", ListSortDirection.Descending));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Page_Closed()
        {
        }

        private void ProfileManager_Initialized()
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                cB_Profiles.SelectedItem = ProfileManager.GetDefault();
            });
        }

        public void ProfileManager_Updated(Profile profile, ProfileUpdateSource source)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                int idx = -1;
                foreach (Profile pr in cB_Profiles.Items)
                {
                    if (pr.Path.Equals(profile.Path, StringComparison.InvariantCultureIgnoreCase))
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
        }

        public void ProfileManager_Deleted(Profile profile, ProfileUpdateSource source)
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
                if (idx == cB_Profiles.SelectedIndex)
                    cB_Profiles.SelectedItem = ProfileManager.GetDefault();
                cB_Profiles.Items.RemoveAt(idx);
            });
        }

        private async void CreateProfile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
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
                                XmlDocument doc = new();
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

                    Profile profile = new(path);
                    profile.Layout = LayoutTemplate.DefaultLayout.Layout.Clone() as Layout;
                    profile.LayoutTitle = LayoutTemplate.DefaultLayout.Name;

                    bool exists = false;

                    // check on path rather than profile
                    if (ProfileManager.Contains(path))
                    {
                        Task<ContentDialogResult> result = Dialog.ShowAsync(
                            string.Format(Properties.Resources.ProfilesPage_AreYouSureOverwrite1, profile.Name),
                            string.Format(Properties.Resources.ProfilesPage_AreYouSureOverwrite2, profile.Name),
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

        private void AdditionalSettings_Click(object sender, RoutedEventArgs e)
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

        private void Profiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                // disable delete button if is default profile
                b_DeleteProfile.IsEnabled = !selectedProfile.Default;
                // prevent user from renaming default profile
                tB_ProfileName.IsEnabled = !selectedProfile.Default;
                // prevent user from disabling default profile
                Toggle_EnableProfile.IsEnabled = !selectedProfile.Default;

                // Profile info
                tB_ProfileName.Text = selectedProfile.Name;
                tB_ProfilePath.Text = selectedProfile.Path;
                Toggle_EnableProfile.IsOn = selectedProfile.Enabled;

                cB_GyroSteering.SelectedIndex = selectedProfile.SteeringAxis;
                Toggle_InvertHorizontal.IsOn = selectedProfile.MotionInvertHorizontal;
                Toggle_InvertVertical.IsOn = selectedProfile.MotionInvertVertical;

                cB_Input.SelectedIndex = (int)selectedProfile.MotionInput;
                cB_UMC_MotionDefaultOffOn.SelectedIndex = (int)selectedProfile.MotionMode;

                bool MotionMapped = false;
                if (selectedProfile.Layout.AxisLayout.TryGetValue(AxisLayoutFlags.Gyroscope, out IActions action))
                    if (action.ActionType != ActionType.Disabled)
                        MotionMapped = true;

                MotionControlAdditional.Visibility = MotionMapped ? Visibility.Visible : Visibility.Collapsed;
                MotionControlWarning.Visibility = MotionMapped ? Visibility.Collapsed : Visibility.Visible;

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

        private async void DeleteProfile_Click(object sender, RoutedEventArgs e)
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
                    ProfileManager.DeleteProfile(selectedProfile, ProfileUpdateSource.ProfilesPage);
                    break;
                default:
                    break;
            }
        }

        private void UpdateProfile_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProfile is null)
                return;

            // Profile
            selectedProfile.Name = tB_ProfileName.Text;
            selectedProfile.Path = tB_ProfilePath.Text;
            selectedProfile.Enabled = Toggle_EnableProfile.IsOn;

            // Motion control settings
            selectedProfile.SteeringAxis = cB_GyroSteering.SelectedIndex;
            selectedProfile.MotionInvertVertical = Toggle_InvertVertical.IsOn == true;
            selectedProfile.MotionInvertHorizontal = Toggle_InvertHorizontal.IsOn == true;

            selectedProfile.MotionInput = (MotionInput)cB_Input.SelectedIndex;
            selectedProfile.MotionMode = (MotionMode)cB_UMC_MotionDefaultOffOn.SelectedIndex;

            ProfileManager.UpdateOrCreateProfile(selectedProfile, ProfileUpdateSource.ProfilesPage);

            _ = Dialog.ShowAsync($"{Properties.Resources.ProfilesPage_ProfileUpdated1}",
                    $"{selectedProfile.Name} {Properties.Resources.ProfilesPage_ProfileUpdated2}",
                    ContentDialogButton.Primary, null, $"{Properties.Resources.ProfilesPage_OK}");
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            ((Expander)sender).BringIntoView();
        }

        private void Toggle_EnableProfile_Toggled(object sender, RoutedEventArgs e)
        {
            // do something
        }

        private void Input_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cB_Input.SelectedIndex == -1)
                return;

            MotionInput input = (MotionInput)cB_Input.SelectedIndex;

            Text_InputHint.Text = Profile.InputDescription[input];
        }

        private void UMC_MotionDefaultOffOn_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (cB_UMC_MotionDefaultOffOn.SelectedIndex == -1)
                return;
        }

        private void HotkeysManager_HotkeyCreated(Hotkey hotkey)
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

        private void InputsManager_TriggerUpdated(string listener, InputsChord inputs, InputsManager.ListenerType type)
        {
            switch (listener)
            {
                case "shortcutProfilesPage@":
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
