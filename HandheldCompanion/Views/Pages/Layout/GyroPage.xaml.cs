using HandheldCompanion.Controllers;
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
                AxisStack panel = new(axis);
                GyroscopePanel.Children.Add(panel);

                AxisStacks.Add(axis, panel);
            }
        }

        public override void UpdateController(IController controller)
        {
            base.UpdateController(controller);

            bool gyro = CheckController(controller, Gyroscope);

            gridGyroscope.Visibility = gyro ? Visibility.Visible : Visibility.Collapsed;

            enabled = gyro;
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
