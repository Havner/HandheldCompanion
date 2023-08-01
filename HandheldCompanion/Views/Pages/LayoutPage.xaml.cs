using HandheldCompanion.Actions;
using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Layout = HandheldCompanion.Misc.Layout;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for ControllerSettings.xaml
    /// </summary>
    public partial class LayoutPage : Page
    {
        // page vars
        private ButtonsPage buttonsPage = new();
        private DpadPage dpadPage = new();
        private TriggersPage triggersPage = new();
        private JoysticksPage joysticksPage = new();
        private TrackpadsPage trackpadsPage = new();
        private GyroPage gyroPage = new();

        private Dictionary<string, (ILayoutPage, NavigationViewItem)> pages;

        private NavigationView parentNavView;

        private string preNavItemTag;

        private LayoutTemplate currentTemplate = new();

        protected LockObject updateLock = new();

        public LayoutPage()
        {
            InitializeComponent();
        }

        public LayoutPage(string Tag, NavigationView parent) : this()
        {
            this.Tag = Tag;
            this.parentNavView = parent;

            // create controller related pages
            this.pages = new()
            {
                // buttons
                { "ButtonsPage", ( buttonsPage, navButtons ) },
                { "DpadPage", ( dpadPage, navDpad ) },

                // triger
                { "TriggersPage", ( triggersPage, navTriggers ) },

                // axis
                { "JoysticksPage", ( joysticksPage, navJoysticks ) },
                { "TrackpadsPage", ( trackpadsPage, navTrackpads ) },

                // gyro
                { "GyroPage", ( gyroPage, navGyro ) },
            };

            foreach (ButtonStack buttonStack in buttonsPage.ButtonStacks.Values.Union(dpadPage.ButtonStacks.Values).Union(triggersPage.ButtonStacks.Values).Union(joysticksPage.ButtonStacks.Values).Union(trackpadsPage.ButtonStacks.Values))
            {
                buttonStack.Updated += (sender, actions) => ButtonMapping_Updated((ButtonFlags)sender, actions);
                buttonStack.Deleted += (sender) => ButtonMapping_Deleted((ButtonFlags)sender);
            }

            foreach (TriggerStack triggerStack in triggersPage.TriggerStacks.Values)
            {
                triggerStack.Updated += (sender, actions) => AxisMapping_Updated((AxisLayoutFlags)sender, actions);
                triggerStack.Deleted += (sender) => AxisMapping_Deleted((AxisLayoutFlags)sender);
            }

            foreach (AxisStack axisStack in joysticksPage.AxisStacks.Values.Union(trackpadsPage.AxisStacks.Values).Union(gyroPage.AxisStacks.Values))
            {
                axisStack.Updated += (sender, action) => AxisMapping_Updated((AxisLayoutFlags)sender, action);
                axisStack.Deleted += (sender) => AxisMapping_Deleted((AxisLayoutFlags)sender);
            }

            LayoutManager.Updated += LayoutManager_Updated;
            LayoutManager.Initialized += LayoutManager_Initialized;
            ControllerManager.ControllerSelected += ControllerManager_ControllerSelected;
            VirtualManager.ControllerSelected += VirtualManager_ControllerSelected;
            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;
        }

        private void ControllerManager_ControllerSelected(IController controller)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                RefreshLayoutList();

                // cascade update to (sub)pages
                foreach (var page in pages.Values)
                {
                    page.Item1.UpdateController(controller);
                    page.Item2.IsEnabled = page.Item1.IsEnabled();
                }
            });
        }

        private void VirtualManager_ControllerSelected(IController controller)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // cascade update to (sub)pages
                foreach (var page in pages.Values)
                    page.Item1.UpdateSelections();
            });
        }

        private void LayoutManager_Initialized()
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                RefreshLayoutList();
            });
        }

        private void LayoutManager_Updated(LayoutTemplate layoutTemplate)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // Get template separator index
                int idx = -1;

                // search if we already have this template listed
                foreach (object item in cB_Layouts.Items)
                {
                    if (item.GetType() != typeof(ComboBoxItem))
                        continue;

                    // get template
                    ComboBoxItem parent = (ComboBoxItem)item;
                    if (parent.Content.GetType() != typeof(LayoutTemplate))
                        continue;

                    LayoutTemplate template = (LayoutTemplate)parent.Content;
                    if (template.Guid.Equals(layoutTemplate.Guid))
                    {
                        idx = cB_Layouts.Items.IndexOf(parent);
                        break;
                    }
                }

                if (idx != -1)
                {
                    // found it
                    cB_Layouts.Items[idx] = new ComboBoxItem() { Content = layoutTemplate };
                }
                else
                {
                    // new entry
                    if (layoutTemplate.IsInternal)
                        idx = cB_Layouts.Items.IndexOf(cB_LayoutsSplitterTemplates) + 1;
                    else
                        idx = cB_Layouts.Items.IndexOf(cB_LayoutsSplitterCommunity) + 1;

                    cB_Layouts.Items.Insert(idx, new ComboBoxItem() { Content = layoutTemplate });
                }

                cB_Layouts.Items.Refresh();
                cB_Layouts.InvalidateVisual();
            });
        }

        private void SettingsManager_SettingValueChanged(string? name, object value)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (name)
                {
                    case "LayoutFilterOnDevice":
                        CheckBoxDeviceLayouts.IsChecked = Convert.ToBoolean(value);
                        RefreshLayoutList();
                        break;
                }
            });
        }

        private void RefreshLayoutList()
        {
            // Get filter settings
            bool FilterOnDevice = SettingsManager.GetBoolean("LayoutFilterOnDevice");

            // Get current controller
            IController controller = ControllerManager.GetTargetController();

            foreach (LayoutTemplate layoutTemplate in LayoutManager.Templates)
            {
                // get parent
                ComboBoxItem parent = layoutTemplate.Parent as ComboBoxItem;
                if (parent is null)
                    continue;

                if (layoutTemplate.ControllerType is not null && FilterOnDevice)
                {
                    if (layoutTemplate.ControllerType != controller?.GetType())
                    {
                        parent.Visibility = Visibility.Collapsed;
                        continue;
                    }
                }

                parent.Visibility = Visibility.Visible;
            }
        }

        private void ButtonMapping_Deleted(ButtonFlags button)
        {
            if (updateLock)
                return;

            currentTemplate.Layout.RemoveLayout(button);
        }

        private void ButtonMapping_Updated(ButtonFlags button, List<IActions> actions)
        {
            if (updateLock)
                return;

            currentTemplate.Layout.UpdateLayout(button, actions);
        }

        private void AxisMapping_Deleted(AxisLayoutFlags axis)
        {
            if (updateLock)
                return;

            currentTemplate.Layout.RemoveLayout(axis);
        }

        private void AxisMapping_Updated(AxisLayoutFlags axis, List<IActions> actions)
        {
            if (updateLock)
                return;

            currentTemplate.Layout.UpdateLayout(axis, actions);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Page_Closed()
        {
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            ((Expander)sender).BringIntoView();
        }

        public void UpdateLayout(LayoutTemplate layoutTemplate)
        {
            // TODO: Not entirely sure what is going on here, but the old templates were still sending
            // events. Shouldn't they be destroyed? Either there is a bug or I don't understand something
            // in C# (probably the latter). Either way this handles/fixes/workarounds the issue.

            if (layoutTemplate.Layout == currentTemplate.Layout)
                layoutTemplate.ClearDelegates();
            else
            {
                currentTemplate.ClearDelegates();
                currentTemplate = layoutTemplate;
                UpdatePages();
            }
        }

        private void UpdatePages()
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // This is a very important lock, it blocks backward events to the layout when
                // this is actually the backend that triggered the update. Notifications on higher
                // levels (pages and mappings) could potentially be blocked for optimization.
                using (new ScopedLock(updateLock))
                {
                    // cascade update to (sub)pages
                    foreach (var page in pages.Values)
                        page.Item1.Update(currentTemplate.Layout);

                    // clear layout selection
                    cB_Layouts.SelectedValue = null;
                }
            });
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
            var item = pages.FirstOrDefault(p => p.Key.Equals(navItemTag));
            Page _page = item.Value.Item1;

            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            var preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (!(_page is null) && !Type.Equals(preNavPageType, _page))
            {
                NavView_Navigate(_page);
            }
        }

        public void NavView_Navigate(Page _page)
        {
            ContentFrame.Navigate(_page);
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
            preNavItemTag = "ButtonsPage";
            NavView_Navigate(preNavItemTag);
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            navView.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType is not null)
            {
                var preNavPageType = ContentFrame.CurrentSourcePageType;
                var preNavPageName = preNavPageType.Name;

                var NavViewItem = navView.MenuItems
                    .OfType<NavigationViewItem>()
                    .Where(n => n.Tag.Equals(preNavPageName)).FirstOrDefault();

                if (!(NavViewItem is null))
                    navView.SelectedItem = NavViewItem;

                string header = currentTemplate.Product.Length > 0 ?
                    "Profile: " + currentTemplate.Product : "Layout: Desktop";
                parentNavView.Header = new TextBlock() { Text = header };
            }
        }
        #endregion

        private void cB_Layouts_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            foreach (object item in comboBox.Items)
            {
                if (item.GetType() != typeof(ComboBoxItem))
                    continue;

                ComboBoxItem layoutTemplate = (ComboBoxItem)item;
                layoutTemplate.Width = comboBox.ActualWidth;
                layoutTemplate.InvalidateVisual();
            }
        }

        private async void ButtonApplyLayout_Click(object sender, RoutedEventArgs e)
        {
            if (cB_Layouts.SelectedItem is null)
                return;

            if (cB_Layouts.SelectedItem.GetType() != typeof(ComboBoxItem))
                return;

            // get parent
            ComboBoxItem parent = cB_Layouts.SelectedItem as ComboBoxItem;

            if (parent.Content.GetType() != typeof(LayoutTemplate))
                return;

            // get template
            LayoutTemplate layoutTemplate = (LayoutTemplate)parent.Content;

            Task<ContentDialogResult> result = Dialog.ShowAsync(
                String.Format(Properties.Resources.ProfilesPage_AreYouSureOverwrite1, currentTemplate.Name),
                String.Format(Properties.Resources.ProfilesPage_AreYouSureOverwrite2, currentTemplate.Name),
                ContentDialogButton.Primary,
                $"{Properties.Resources.ProfilesPage_Cancel}",
                $"{Properties.Resources.ProfilesPage_Yes}");

            await result; // sync call

            switch (result.Result)
            {
                case ContentDialogResult.Primary:
                    {
                        // do not overwrite currentTemplate and currentTemplate.Layout as a whole
                        // because they both have important Update notifitications set
                        var newLayout = layoutTemplate.Layout.Clone() as Layout;
                        currentTemplate.Layout.AxisLayout = newLayout.AxisLayout;
                        currentTemplate.Layout.ButtonLayout = newLayout.ButtonLayout;
                        currentTemplate.Name = layoutTemplate.Name;
                        currentTemplate.Description = layoutTemplate.Description;
                        currentTemplate.Guid = layoutTemplate.Guid; // not needed

                        // the whole layout has been updated without notification, trigger one
                        currentTemplate.Layout.UpdateLayout();

                        UpdatePages();
                    }
                    break;
            }
        }

        private void cB_Layouts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ButtonApplyLayout.IsEnabled = cB_Layouts.SelectedIndex != -1;
        }

        private void CheckBoxDeviceLayouts_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            SettingsManager.SetProperty("LayoutFilterOnDevice", CheckBoxDeviceLayouts.IsChecked);
        }

        private void LayoutExportButton_Click(object sender, RoutedEventArgs e)
        {
            LayoutTemplate newLayout = new()
            {
                Layout = currentTemplate.Layout,
                Name = LayoutTitle.Text,
                Description = LayoutDescription.Text,
                Author = LayoutAuthor.Text,
                Executable = SaveGameInfo.IsChecked == true ? currentTemplate.Executable : "",
                Product = SaveGameInfo.IsChecked == true ? currentTemplate.Product : "",
                IsInternal = false
            };

            if (newLayout.Name == string.Empty)
            {
                LayoutFlyout.Hide();
                _ = Dialog.ShowAsync("Layout template name cannot be empty",
                        $"Layout was not exported.",
                        ContentDialogButton.Primary, null, $"{Properties.Resources.ProfilesPage_OK}");
                return;
            }

            if (ExportForCurrent.IsChecked == true)
                newLayout.ControllerType = ControllerManager.GetTargetController()?.GetType();

            LayoutManager.SerializeLayoutTemplate(newLayout);

            LayoutFlyout.Hide();
            _ = Dialog.ShowAsync("Layout template exported",
                    $"{newLayout.Name} was exported.",
                    ContentDialogButton.Primary, null, $"{Properties.Resources.ProfilesPage_OK}");
        }

        private void Flyout_Opening(object sender, object e)
        {
            if (currentTemplate.Executable == string.Empty && currentTemplate.Product == string.Empty)
                SaveGameInfo.IsChecked = SaveGameInfo.IsEnabled = false;
            else
                SaveGameInfo.IsChecked = SaveGameInfo.IsEnabled = true;

            string separator = (currentTemplate.Name.Length > 0 && currentTemplate.Product.Length > 0) ? " - " : "";

            LayoutTitle.Text = $"{currentTemplate.Name}{separator}{currentTemplate.Product}";
            LayoutDescription.Text = currentTemplate.Description;
            LayoutAuthor.Text = currentTemplate.Author;
        }

        private void SaveGameInfo_Toggled(object sender, RoutedEventArgs e)
        {
            if (SaveGameInfo.IsChecked == true)
            {
                string separator = (currentTemplate.Name.Length > 0 && currentTemplate.Product.Length > 0) ? " - " : "";
                LayoutTitle.Text = $"{currentTemplate.Name}{separator}{currentTemplate.Product}";
            }
            else
            {
                LayoutTitle.Text = $"{currentTemplate.Name}";
            }
        }

        private void LayoutCancelButton_Click(object sender, RoutedEventArgs e)
        {
            LayoutFlyout.Hide();
        }
    }
}
