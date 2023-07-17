using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using ModernWpf.Controls;
using System.Collections.Generic;
using System.Windows;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for JoysticksPage.xaml
    /// </summary>
    public partial class JoysticksPage : ILayoutPage
    {
        public static List<ButtonFlags> LeftThumbButtons = new() { ButtonFlags.LeftStickClick, ButtonFlags.LeftStickTouch, ButtonFlags.LeftStickUp, ButtonFlags.LeftStickDown, ButtonFlags.LeftStickLeft, ButtonFlags.LeftStickRight };
        public static List<AxisLayoutFlags> LeftThumbAxis = new() { AxisLayoutFlags.LeftStick };
        public static List<ButtonFlags> RightThumbButtons = new() { ButtonFlags.RightStickClick, ButtonFlags.RightStickTouch, ButtonFlags.RightStickUp, ButtonFlags.RightStickDown, ButtonFlags.RightStickLeft, ButtonFlags.RightStickRight };
        public static List<AxisLayoutFlags> RightThumbAxis = new() { AxisLayoutFlags.RightStick };

        public JoysticksPage()
        {
            InitializeComponent();

            // draw UI
            foreach (ButtonFlags button in LeftThumbButtons)
            {
                ButtonMapping buttonMapping = new ButtonMapping(button);
                LeftJoystickButtonsPanel.Children.Add(buttonMapping);

                MappingButtons.Add(button, buttonMapping);
            }

            foreach (AxisLayoutFlags axis in LeftThumbAxis)
            {
                AxisMapping axisMapping = new AxisMapping(axis);
                LeftJoystickPanel.Children.Add(axisMapping);

                MappingAxis.Add(axis, axisMapping);
            }

            foreach (ButtonFlags button in RightThumbButtons)
            {
                ButtonMapping buttonMapping = new ButtonMapping(button);
                RightJoystickButtonsPanel.Children.Add(buttonMapping);

                MappingButtons.Add(button, buttonMapping);
            }

            foreach (AxisLayoutFlags axis in RightThumbAxis)
            {
                AxisMapping axisMapping = new AxisMapping(axis);
                RightJoystickPanel.Children.Add(axisMapping);

                MappingAxis.Add(axis, axisMapping);
            }
        }

        public JoysticksPage(string Tag) : this()
        {
            this.Tag = Tag;
        }

        public override void UpdateController(IController Controller)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // controller based
                foreach (var mapping in MappingButtons)
                {
                    ButtonFlags button = mapping.Key;
                    ButtonMapping buttonMapping = mapping.Value;

                    // update mapping visibility
                    if (!Controller.HasSourceButton(button))
                        buttonMapping.Visibility = Visibility.Collapsed;
                    else
                    {
                        buttonMapping.Visibility = Visibility.Visible;

                        // update icon
                        FontIcon newIcon = Controller.GetFontIcon(button);
                        string newLabel = Controller.GetButtonName(button);

                        buttonMapping.UpdateIcon(newIcon, newLabel);
                    }
                }

                foreach (var mapping in MappingAxis)
                {
                    AxisLayoutFlags flags = mapping.Key;
                    AxisLayout layout = AxisLayout.Layouts[flags];

                    AxisMapping axisMapping = mapping.Value;

                    // update mapping visibility
                    if (!Controller.HasSourceAxis(flags))
                        axisMapping.Visibility = Visibility.Collapsed;
                    else
                    {
                        axisMapping.Visibility = Visibility.Visible;

                        // update icon
                        FontIcon newIcon = Controller.GetFontIcon(flags);
                        string newLabel = Controller.GetAxisName(flags);
                        axisMapping.UpdateIcon(newIcon, newLabel);
                    }
                }
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // do something
        }

        public void Page_Closed()
        {
            // do something
        }
    }
}
