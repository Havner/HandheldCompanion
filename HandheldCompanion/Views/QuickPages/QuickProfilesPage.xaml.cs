using HandheldCompanion.Controls;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
using System.Windows;
using System.Windows.Controls;

namespace HandheldCompanion.Views.QuickPages
{
    /// <summary>
    /// Interaction logic for QuickProfilesPage.xaml
    /// </summary>
    public partial class QuickProfilesPage : Page
    {
        private ProcessEx? currentProcess;
        private Profile? currentProfile;

        private LockObject updateLock = new();

        public QuickProfilesPage()
        {
            InitializeComponent();

            // Those are the only events QuickProfiles needs to work.
            // What is the current process and what is the current profile.
            // Applied is also sent when the current profile is updated or deleted.
            ProcessManager.ForegroundChanged += ProcessManager_ForegroundChanged;
            ProfileManager.Applied += ProfileManager_Applied;
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

        // this event can come from thread other than main
        private void ProfileManager_Applied(Profile profile, ProfileUpdateSource source)
        {
            if (source == ProfileUpdateSource.QuickProfilesPage)
                return;

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // don't store or display the default profile
                if (profile.Default)
                {
                    currentProfile = null;
                    b_CreateProfile.Visibility = GetCreateProfileVisibility();
                    GridProfile.Visibility = Visibility.Collapsed;
                }
                else
                {
                    currentProfile = profile;
                    b_CreateProfile.Visibility = Visibility.Collapsed;
                    GridProfile.Visibility = Visibility.Visible;

                    using (new ScopedLock(updateLock))
                        ProfileToggle.IsOn = currentProfile.Enabled;
                }
            });
        }

        // this event can come from thread other than main
        private void ProcessManager_ForegroundChanged(ProcessEx process)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                currentProcess = process;

                if (currentProcess is not null)
                {
                    ProcessIcon.Source = currentProcess.icon?.ToImageSource();
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

        private void ProfileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (currentProfile is null || updateLock)
                return;

            currentProfile.Enabled = ProfileToggle.IsOn;
            ProfileManager.UpdateOrCreateProfile(currentProfile, ProfileUpdateSource.QuickProfilesPage);
        }

        private void CreateProfile_Click(object sender, RoutedEventArgs e)
        {
            if (currentProcess is null)
                return;

            // create profile
            currentProfile = new(currentProcess.Path)
            {
                Layout = LayoutTemplate.DefaultLayout.Layout.Clone() as Layout,
                LayoutTitle = LayoutTemplate.DesktopLayout.Name
            };

            ProfileManager.UpdateOrCreateProfile(currentProfile, ProfileUpdateSource.Creation);
        }
    }
}
