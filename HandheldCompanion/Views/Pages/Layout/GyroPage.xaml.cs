using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
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

                AxisMappings.Add(axis, axisMapping);
            }
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
