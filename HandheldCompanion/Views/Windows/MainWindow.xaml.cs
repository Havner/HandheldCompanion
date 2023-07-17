using ControllerCommon;
using ControllerCommon.Devices;
using ControllerCommon.Managers;
using HandheldCompanion.Managers;
using HandheldCompanion.Views.Pages;
using HandheldCompanion.Views.Windows;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Navigation;
using Application = System.Windows.Application;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // devices vars
        public static IDevice CurrentDevice;

        // page vars
        private static Dictionary<string, Page> _pages = new();
        private string preNavItemTag;

        public static ControllerPage controllerPage;
        public static ProfilesPage profilesPage;
        public static SettingsPage settingsPage;
        public static AboutPage aboutPage;
        public static OverlayPage overlayPage;
        public static HotkeysPage hotkeysPage;
        public static LayoutPage layoutPage;

        // overlay(s) vars
        public static OverlayModel overlayModel;
        public static OverlayQuickTools overlayquickTools;

        private WindowState visibleWindowState;
        private NotifyIcon notifyIcon;

        public static string CurrentExe;
        private bool appClosing;
        private bool IsReady;

        private static MainWindow CurrentWindow;
        public static FileVersionInfo fileVersionInfo;

        public static string InstallPath;
        public static string SettingsPath;
        public static string CurrentPageName;

        public MainWindow(FileVersionInfo _fileVersionInfo, Assembly CurrentAssembly)
        {
            InitializeComponent();

            fileVersionInfo = _fileVersionInfo;
            CurrentWindow = this;

            // get process
            Process process = Process.GetCurrentProcess();

            // fix touch support
            var tablets = Tablet.TabletDevices;

            // define current directory
            InstallPath = AppDomain.CurrentDomain.BaseDirectory;
            SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HandheldCompanion");

            // initialiaze path
            if (!Directory.Exists(SettingsPath))
                Directory.CreateDirectory(SettingsPath);

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // initialize notifyIcon
            notifyIcon = new()
            {
                Text = Title,
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Visible = true,
                ContextMenuStrip = new()
            };

            notifyIcon.DoubleClick += (sender, e) => { SwapWindowState(); };

            AddNotifyIconItem("Main Window");
            AddNotifyIconItem("Quick Tools");

            AddNotifyIconSeparator();
            AddNotifyIconItem("Exit");

            // paths
            CurrentExe = process.MainModule.FileName;

            // initialize HidHide
            HidHide.RegisterApplication(CurrentExe);

            // initialize title
            this.Title += $" ({fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart})";

            // initialize device
            CurrentDevice = IDevice.GetDefault();
            CurrentDevice.Open();

            // load window(s)
            loadWindows();

            // load page(s)
            loadPages();

            // start static managers in sequence

            ToastManager.Start();
            ToastManager.IsEnabled = SettingsManager.GetBoolean("ToastEnable");

            ProfileManager.Start();
            ControllerManager.Start();
            MotionManager.Start();

            HotkeysManager.CommandExecuted += HotkeysManager_CommandExecuted;
            HotkeysManager.Start();

            DeviceManager.Start();

            PlatformManager.Start();
            LayoutManager.Start();
            ProcessManager.Start();

            PowerManager.SystemStatusChanged += OnSystemStatusChanged;
            PowerManager.Start();

            SystemManager.Start();
            VirtualManager.Start();

            InputsManager.Start();
            TimerManager.Start();

            TaskManager.Start();
            PerformanceManager.Start();

            // start setting last
            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;
            SettingsManager.Start();

            // update Position and Size
            Height = (int)Math.Max(MinHeight, SettingsManager.GetDouble("MainWindowHeight"));
            Width = (int)Math.Max(MinWidth, SettingsManager.GetDouble("MainWindowWidth"));
            Left = Math.Min(SystemParameters.PrimaryScreenWidth - MinWidth, SettingsManager.GetDouble("MainWindowLeft"));
            Top = Math.Min(SystemParameters.PrimaryScreenHeight - MinHeight, SettingsManager.GetDouble("MainWindowTop"));
            navView.IsPaneOpen = SettingsManager.GetBoolean("MainWindowIsPaneOpen");
        }

        private void AddNotifyIconItem(string name, object tag = null)
        {
            tag ??= string.Concat(name.Where(c => !char.IsWhiteSpace(c)));

            ToolStripMenuItem menuItemMainWindow = new ToolStripMenuItem(name);
            menuItemMainWindow.Tag = tag;
            menuItemMainWindow.Click += MenuItem_Click;
            notifyIcon.ContextMenuStrip.Items.Add(menuItemMainWindow);
        }

        private void AddNotifyIconSeparator()
        {
            ToolStripSeparator separator = new ToolStripSeparator();
            notifyIcon.ContextMenuStrip.Items.Add(separator);
        }

        private void SettingsManager_SettingValueChanged(string name, object value)
        {
            switch (name)
            {
                case "ToastEnable":
                    ToastManager.IsEnabled = Convert.ToBoolean(value);
                    break;
                case "DesktopProfileOnStart":
                    if (SettingsManager.IsInitialized)
                        break;

                    bool DesktopLayout = Convert.ToBoolean(value);
                    SettingsManager.SetProperty("DesktopLayoutEnabled", DesktopLayout, false, true);
                    break;
            }
        }

        public void SwapWindowState()
        {
            // UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (WindowState)
                {
                    case WindowState.Normal:
                    case WindowState.Maximized:
                        WindowState = WindowState.Minimized;
                        break;
                    case WindowState.Minimized:
                        WindowState = visibleWindowState;
                        break;
                }
            });
        }

        public static MainWindow GetCurrent()
        {
            return CurrentWindow;
        }

        private void loadPages()
        {
            // initialize pages
            controllerPage = new ControllerPage("controller");
            controllerPage.Loaded += ControllerPage_Loaded;

            profilesPage = new ProfilesPage("profiles");
            settingsPage = new SettingsPage("settings");
            aboutPage = new AboutPage("about");
            overlayPage = new OverlayPage("overlay");
            hotkeysPage = new HotkeysPage("hotkeys");
            layoutPage = new LayoutPage("layout");

            // store pages
            _pages.Add("ControllerPage", controllerPage);
            _pages.Add("ProfilesPage", profilesPage);
            _pages.Add("AboutPage", aboutPage);
            _pages.Add("OverlayPage", overlayPage);
            _pages.Add("SettingsPage", settingsPage);
            _pages.Add("HotkeysPage", hotkeysPage);
            _pages.Add("LayoutPage", layoutPage);

            // handle controllerPage events
            controllerPage.HIDchanged += (HID) =>
            {
                overlayModel.UpdateHIDMode(HID);
            };
        }

        private void loadWindows()
        {
            // initialize overlay
            overlayModel = new OverlayModel();
            overlayquickTools = new OverlayQuickTools();
        }

        private void HotkeysManager_CommandExecuted(string listener)
        {
            switch (listener)
            {
                case "shortcutMainWindow":
                    SwapWindowState();
                    break;
                case "shortcutQuickTools":
                    overlayquickTools.UpdateVisibility();
                    break;
                case "overlayGamepad":
                    overlayModel.UpdateVisibility();
                    break;
            }
        }

        private void MenuItem_Click(object? sender, EventArgs e)
        {
            switch (((ToolStripMenuItem)sender).Tag)
            {
                case "MainWindow":
                    SwapWindowState();
                    break;
                case "QuickTools":
                    overlayquickTools.UpdateVisibility();
                    break;
                case "Exit":
                    appClosing = true;
                    this.Close();
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // do something
        }

        private void ControllerPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsReady)
                return;

            // home page has loaded, display main window
            WindowState = SettingsManager.GetBoolean("StartMinimized") ? WindowState.Minimized : (WindowState)SettingsManager.GetInt("MainWindowVisibleState");
            visibleWindowState = (WindowState)SettingsManager.GetInt("MainWindowVisibleState");

            IsReady = true;
        }

        #region UI
        private void navView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is null)
                return;

            NavigationViewItem navItem = (NavigationViewItem)args.InvokedItemContainer;
            preNavItemTag = (string)navItem.Tag;
            NavView_Navigate(preNavItemTag);
        }

        public void NavView_Navigate(string navItemTag)
        {
            var item = _pages.FirstOrDefault(p => p.Key.Equals(navItemTag));
            Page _page = item.Value;

            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            var preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (!(_page is null) && !Type.Equals(preNavPageType, _page))
                NavView_Navigate(_page);
        }

        public static void NavView_Navigate(Page _page)
        {
            CurrentWindow.ContentFrame.Navigate(_page);
        }

        private void navView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            CurrentDevice.Close();

            notifyIcon.Visible = false;
            notifyIcon.Dispose();

            overlayModel.Close();
            overlayquickTools.Close(true);

            TaskManager.Stop();
            PerformanceManager.Stop();
            VirtualManager.Stop();
            SystemManager.Stop();
            MotionManager.Stop();
            ControllerManager.Stop();
            HotkeysManager.Stop();
            InputsManager.Stop();
            DeviceManager.Stop();
            PlatformManager.Stop();
            ProfileManager.Stop();
            LayoutManager.Stop();
            ProcessManager.Stop();
            PowerManager.Stop();
            ToastManager.Stop();
            SettingsManager.Stop();
            TimerManager.Stop();

            // closing page(s)
            controllerPage.Page_Closed();
            profilesPage.Page_Closed();
            settingsPage.Page_Closed();
            overlayPage.Page_Closed();
            hotkeysPage.Page_Closed();
            layoutPage.Page_Closed();

            // force kill application
            Environment.Exit(0);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                    // save normal position and size settings
                    SettingsManager.SetProperty("MainWindowLeft", Left);
                    SettingsManager.SetProperty("MainWindowTop", Top);
                    SettingsManager.SetProperty("MainWindowWidth", Width);
                    SettingsManager.SetProperty("MainWindowHeight", Height);
                    break;
            }

            SettingsManager.SetProperty("MainWindowVisibleState", (int)visibleWindowState);
            SettingsManager.SetProperty("MainWindowIsPaneOpen", navView.IsPaneOpen);

            if (SettingsManager.GetBoolean("CloseMinimises") && !appClosing)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
                return;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Minimized:
                    ShowInTaskbar = false;
                    break;

                case WindowState.Normal:
                case WindowState.Maximized:
                    ShowInTaskbar = true;
                    this.Activate();
                    visibleWindowState = WindowState;
                    break;
            }
        }

        private void navView_Loaded(object sender, RoutedEventArgs e)
        {
            // Add handler for ContentFrame navigation.
            ContentFrame.Navigated += On_Navigated;

            // NavView doesn't load any page by default, so load home page.
            navView.SelectedItem = navView.MenuItems[0];

            // If navigation occurs on SelectionChanged, this isn't needed.
            // Because we use ItemInvoked to navigate, we need to call Navigate
            // here to load the home page.
            preNavItemTag = "ControllerPage";
            NavView_Navigate(preNavItemTag);
        }

        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (navView.IsPaneOpen &&
                (navView.DisplayMode == NavigationViewDisplayMode.Compact ||
                 navView.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            navView.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType is not null)
            {
                CurrentPageName = ContentFrame.CurrentSourcePageType.Name;

                var NavViewItem = navView.MenuItems
                    .OfType<NavigationViewItem>()
                    .Where(n => n.Tag.Equals(CurrentPageName)).FirstOrDefault();

                if (!(NavViewItem is null))
                    navView.SelectedItem = NavViewItem;

                navView.Header = new TextBlock() { Text = (string)((Page)e.Content).Title };
            }
        }
        #endregion

        // no code from the cases inside this function will be called on program start
        private async void OnSystemStatusChanged(PowerManager.SystemStatus status, PowerManager.SystemStatus prevStatus)
        {
            if (status == prevStatus)
                return;

            switch (status)
            {
                case PowerManager.SystemStatus.SystemReady:
                    // resume from sleep
                    // TODO: lower those delays?
                    if (prevStatus == PowerManager.SystemStatus.SystemPending)
                    {
                        await Task.Delay(2000);

                        // restore inputs manager
                        InputsManager.Start();

                        // start timer manager
                        TimerManager.Start();

                        // resume the virtual controller last
                        await Task.Delay(CurrentDevice.ResumeDelay);
                        VirtualManager.Resume();
                    }
                    break;

                case PowerManager.SystemStatus.SystemPending:
                    // sleep
                    {
                        // stop the virtual controller
                        VirtualManager.Pause();

                        // stop timer manager
                        TimerManager.Stop();

                        // pause inputs manager
                        InputsManager.Stop();
                    }
                    break;
            }
        }
    }
}
