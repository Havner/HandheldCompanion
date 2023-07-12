using ControllerCommon.Controllers;
using ControllerCommon.Inputs;
using HandheldCompanion.Controls;
using ModernWpf.Controls;
using System.Collections.Generic;
using System.Windows;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for GyroPage.xaml
    /// </summary>
    public partial class GyroPage : ILayoutPage
    {
        public static List<AxisLayoutFlags> Gyroscope = new() { AxisLayoutFlags.Gyroscope };

        public GyroPage()
        {
            InitializeComponent();

            // draw UI
            foreach (AxisLayoutFlags axis in Gyroscope)
            {
                AxisMapping axisMapping = new AxisMapping(axis);
                GyroscopePanel.Children.Add(axisMapping);

                MappingAxis.Add(axis, axisMapping);
            }
        }

        public override void UpdateController(IController Controller)
        {
            // UI thread (async)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // controller based
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

        public GyroPage(string Tag) : this()
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
