using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using System.Collections.Generic;
using System.Windows;

namespace HandheldCompanion.Views.Pages
{
    /// <summary>
    /// Interaction logic for DpadPage.xaml
    /// </summary>
    public partial class DpadPage : ILayoutPage
    {
        public static List<ButtonFlags> DPAD = new() { ButtonFlags.DPadUp, ButtonFlags.DPadDown, ButtonFlags.DPadLeft, ButtonFlags.DPadRight };

        public DpadPage()
        {
            InitializeComponent();

            // draw UI
            foreach (ButtonFlags button in DPAD)
            {
                ButtonMapping buttonMapping = new ButtonMapping(button);
                DpadStackPanel.Children.Add(buttonMapping);

                MappingButtons.Add(button, buttonMapping);
            }
        }

        public DpadPage(string Tag) : this()
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
