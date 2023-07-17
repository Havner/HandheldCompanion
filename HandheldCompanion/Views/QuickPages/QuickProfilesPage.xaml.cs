using ControllerCommon.Misc;
using HandheldCompanion.Controls;
using HandheldCompanion.Managers;
using System.Threading;
using System.Timers;
using System.Windows;
using Layout = ControllerCommon.Misc.Layout;
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
    }
}
