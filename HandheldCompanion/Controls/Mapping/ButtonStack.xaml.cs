using HandheldCompanion.Actions;
using HandheldCompanion.Inputs;
using ModernWpf.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace HandheldCompanion.Controls
{
    /// <summary>
    /// Interaction logic for MappingStack.xaml
    /// </summary>
    public partial class ButtonStack : SimpleStackPanel
    {
        private ButtonFlags button;
        // must always be kept with at least one element
        private List<ButtonMapping> buttonMappings;

        FontIcon fontIcon = new();
        string label = "";

        public event UpdatedEventHandler Updated;
        public delegate void UpdatedEventHandler(object sender, List<IActions> actions);
        public event DeletedEventHandler Deleted;
        public delegate void DeletedEventHandler(object sender);

        public ButtonStack() : base()
        {
            InitializeComponent();
        }

        public ButtonStack(ButtonFlags button) : this()
        {
            this.button = button;
            this.buttonMappings = new() { new ButtonMapping(button) };

            RefreshChildren();
        }

        public void UpdateIcon(FontIcon newIcon, string newLabel)
        {
            this.fontIcon = newIcon;
            this.label = newLabel;

            buttonMappings[0].UpdateIcon(newIcon, newLabel);
        }

        // actions cannot be null or empty
        public void SetActions(List<IActions> actions)
        {
            buttonMappings.Clear();
            foreach (var action in actions)
            {
                ButtonMapping mapping = new(button);
                mapping.SetIActions(action);
                buttonMappings.Add(mapping);
            }

            buttonMappings[0].UpdateIcon(fontIcon, label);
            RefreshChildren();
        }

        public void Reset()
        {
            this.buttonMappings = new List<ButtonMapping>() { new ButtonMapping(button) };
            buttonMappings[0].UpdateIcon(fontIcon, label);
            RefreshChildren();
        }

        private void ButtonMapping_Updated(ButtonFlags button)
        {
            List<IActions> actions = new();
            foreach (var mapping in buttonMappings)
            {
                IActions action = mapping.GetIActions();
                if (action is null)
                    continue;

                actions.Add(action);
            }

            if (actions.Count > 0)
                Updated.Invoke(button, actions);
            else
                Deleted.Invoke(button);
        }

        private void AddMappingToChildren(ButtonMapping mapping)
        {
            // doesn't matter if updated or deleted, we need to gather all active
            // we can't map those 1:1 anyway, as there can be more mappings than actions
            mapping.Updated += (sender, action) => ButtonMapping_Updated((ButtonFlags)sender);
            mapping.Deleted += (sender) => ButtonMapping_Updated((ButtonFlags)sender);

            int index = Children.Count;

            FontIcon fontIcon = new FontIcon();
            fontIcon.HorizontalAlignment = HorizontalAlignment.Center;
            fontIcon.VerticalAlignment = VerticalAlignment.Center;
            fontIcon.FontFamily = new("Segoe Fluent Icons");
            fontIcon.FontSize = 30;

            Button button = new();
            button.VerticalAlignment = VerticalAlignment.Top;
            button.Height = 48; button.Width = 48;
            button.Style = FindResource("AccentButtonStyle") as Style;
            button.Margin = new Thickness(0, 0, 1, 0);
            button.Padding = new Thickness(2, 2, 0, 0);  // the glyph is not centered within its box
            button.Tag = index;
            button.Click += Button_Click;
            if (index == 0)
                fontIcon.Glyph = "\uECC8";
            else
                fontIcon.Glyph = "\uECC9";
            button.Content = fontIcon;

            Grid grid = new();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(mapping, 1);
            grid.Children.Add(button);
            grid.Children.Add(mapping);
            Children.Add(grid);
        }

        private void CleanChildren()
        {
            foreach (var child in Children)
                (child as Grid).Children.Clear();
            Children.Clear();
        }

        // call this function always after buttonMappings were recreated
        private void RefreshChildren()
        {
            CleanChildren();

            foreach (var mapping in buttonMappings)
                AddMappingToChildren(mapping);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int index = (int)((Button)sender).Tag;

            // Add
            if (index == 0)
            {
                ButtonMapping mapping = new(button);
                buttonMappings.Add(mapping);
                AddMappingToChildren(mapping);
                // no need to register new mapping, it's empty, will be updated with event
            }
            // Del
            else
            {
                buttonMappings.RemoveAt(index);
                (Children[index] as Grid).Children.Clear();
                Children.RemoveAt(index);

                // reindex remaining, Children[0] is [(+)|(-)] button inside grid
                index = 0;
                foreach (var child in Children)
                    ((child as Grid).Children[0] as Button).Tag = index++;

                // removal needs to be registered as mapping disappears without event
                ButtonMapping_Updated(button);
            }
        }
    }
}
