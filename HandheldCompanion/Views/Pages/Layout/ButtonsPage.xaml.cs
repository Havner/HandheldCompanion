using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for ButtonsPage.xaml
    /// </summary>
    public partial class ButtonsPage : ILayoutPage
    {
        public static List<ButtonFlags> ABXY = new() { ButtonFlags.B1, ButtonFlags.B2, ButtonFlags.B3, ButtonFlags.B4 };
        public static List<ButtonFlags> BUMPERS = new() { ButtonFlags.L1, ButtonFlags.R1 };
        public static List<ButtonFlags> MENU = new() { ButtonFlags.Back, ButtonFlags.Start, ButtonFlags.Special, ButtonFlags.Quick };
        public static List<ButtonFlags> BACKGRIPS = new() { ButtonFlags.L4, ButtonFlags.L5, ButtonFlags.R4, ButtonFlags.R5 };

        public ButtonsPage()
        {
            InitializeComponent();

            // draw UI
            foreach (ButtonFlags button in ABXY)
            {
                ButtonStack panel = new(button);
                ButtonsStackPanel.Children.Add(panel);

                ButtonStacks.Add(button, panel);
            }

            foreach (ButtonFlags button in BUMPERS)
            {
                ButtonStack panel = new(button);
                BumpersStackPanel.Children.Add(panel);

                ButtonStacks.Add(button, panel);
            }

            foreach (ButtonFlags button in MENU)
            {
                ButtonStack panel = new(button);
                MenuStackPanel.Children.Add(panel);

                ButtonStacks.Add(button, panel);
            }

            foreach (ButtonFlags button in BACKGRIPS)
            {
                ButtonStack panel = new(button);
                BackgripsStackPanel.Children.Add(panel);

                ButtonStacks.Add(button, panel);
            }
        }

        public override void UpdateController(IController controller)
        {
            base.UpdateController(controller);

            bool abxy = CheckController(controller, ABXY);
            bool bumpers = CheckController(controller, BUMPERS);
            bool menu = CheckController(controller, MENU);
            bool backgrips = CheckController(controller, BACKGRIPS);

            gridButtons.Visibility = abxy ? Visibility.Visible : Visibility.Collapsed;
            gridBumpers.Visibility = bumpers ? Visibility.Visible : Visibility.Collapsed;
            gridMenu.Visibility = menu ? Visibility.Visible : Visibility.Collapsed;
            gridBackgrips.Visibility = backgrips ? Visibility.Visible : Visibility.Collapsed;

            enabled = abxy || bumpers || menu || backgrips;
        }

        public ButtonsPage(string Tag) : this()
        {
            this.Tag = Tag;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Page_Closed()
        {
        }
    }
}
