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
        public static List<ButtonFlags> ABXY = new() { ButtonFlags.B1, ButtonFlags.B2, ButtonFlags.B3, ButtonFlags.B4, ButtonFlags.B5, ButtonFlags.B6, ButtonFlags.B7, ButtonFlags.B8 };
        public static List<ButtonFlags> BUMPERS = new() { ButtonFlags.L1, ButtonFlags.R1 };
        public static List<ButtonFlags> MENU = new() { ButtonFlags.Start, ButtonFlags.Back, ButtonFlags.Special };
        public static List<ButtonFlags> BACKGRIPS = new() { ButtonFlags.L4, ButtonFlags.R4, ButtonFlags.L5, ButtonFlags.R5 };
        public static List<ButtonFlags> OEM = new() { ButtonFlags.OEM1, ButtonFlags.OEM2, ButtonFlags.OEM3, ButtonFlags.OEM4, ButtonFlags.OEM5, ButtonFlags.OEM6, ButtonFlags.OEM7, ButtonFlags.OEM8, ButtonFlags.OEM9, ButtonFlags.OEM10 };

        public ButtonsPage()
        {
            InitializeComponent();

            // draw UI
            foreach (ButtonFlags button in ABXY)
            {
                ButtonMapping buttonMapping = new ButtonMapping(button);
                ButtonsStackPanel.Children.Add(buttonMapping);

                MappingButtons.Add(button, buttonMapping);
            }

            foreach (ButtonFlags button in BUMPERS)
            {
                ButtonMapping buttonMapping = new ButtonMapping(button);
                BumpersStackPanel.Children.Add(buttonMapping);

                MappingButtons.Add(button, buttonMapping);
            }

            foreach (ButtonFlags button in MENU)
            {
                ButtonMapping buttonMapping = new ButtonMapping(button);
                MenuStackPanel.Children.Add(buttonMapping);

                MappingButtons.Add(button, buttonMapping);
            }

            foreach (ButtonFlags button in BACKGRIPS)
            {
                ButtonMapping buttonMapping = new ButtonMapping(button);
                BACKGRIPSStackPanel.Children.Add(buttonMapping);

                MappingButtons.Add(button, buttonMapping);
            }

            foreach (ButtonFlags button in OEM)
            {
                if (!MainWindow.CurrentDevice.OEMButtons.Contains(button))
                    continue;

                ButtonMapping buttonMapping = new ButtonMapping(button);
                OEMStackPanel.Children.Add(buttonMapping);

                MappingButtons.Add(button, buttonMapping);
            }

            // manage layout pages visibility
            gridOEM.Visibility = MainWindow.CurrentDevice.OEMButtons.Count() > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public override void UpdateController(IController controller)
        {
            // this will collapse all OEM buttons
            base.UpdateController(controller);

            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // controller based
                foreach (var mapping in MappingButtons)
                {
                    ButtonFlags button = mapping.Key;
                    ButtonMapping buttonMapping = mapping.Value;

                    // specific buttons are handled elsewhere
                    if (OEM.Contains(button))
                    {
                        buttonMapping.Name.Text = MainWindow.CurrentDevice.GetButtonName(button);
                        buttonMapping.Visibility = Visibility.Visible;

                        // update icon
                        FontIcon newIcon = controller.GetFontIcon(button);
                        string newLabel = controller.GetButtonName(button);

                        buttonMapping.UpdateIcon(newIcon, newLabel);
                    }
                }

                // manage layout pages visibility
                bool HasBackGrips = controller.HasSourceButton(ButtonFlags.L4) || controller.HasSourceButton(ButtonFlags.L5) || controller.HasSourceButton(ButtonFlags.R4) || controller.HasSourceButton(ButtonFlags.R5);
                gridBACKGRIPS.Visibility = HasBackGrips ? Visibility.Visible : Visibility.Collapsed;
            });
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
