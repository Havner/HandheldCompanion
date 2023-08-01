using GregsStack.InputSimulatorStandard.Native;
using HandheldCompanion.Actions;
using HandheldCompanion.Controllers;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Utils;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using KeyboardSimulator = HandheldCompanion.Simulators.KeyboardSimulator;

namespace HandheldCompanion.Controls
{
    /// <summary>
    /// Interaction logic for ButtonMapping.xaml
    /// </summary>
    public partial class ButtonMapping : IMapping
    {
        private static List<Label> keyList = null;

        public ButtonMapping()
        {
            // lazilly initialize
            if (keyList is null)
            {
                keyList = new();
                foreach (KeyFlags key in KeyFlagsOrder.arr)
                {
                    // create a label, store VirtualKeyCode as Tag and Label as controller specific string
                    Label buttonLabel = new Label() { Tag = (VirtualKeyCode)key, Content = EnumUtils.GetDescriptionFromEnumValue(key) };
                    keyList.Add(buttonLabel);
                }
            }

            InitializeComponent();
        }

        public ButtonMapping(ButtonFlags button) : this()
        {
            this.Value = button;
        }

        public void UpdateIcon(FontIcon newIcon, string newLabel)
        {
            this.Name.Text = newLabel;

            this.Icon.Glyph = newIcon.Glyph;
            this.Icon.FontFamily = newIcon.FontFamily;
            this.Icon.FontSize = newIcon.FontSize;

            if (newIcon.Foreground is not null)
                this.Icon.Foreground = newIcon.Foreground;
            else
                this.Icon.SetResourceReference(Control.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");
        }

        public void UpdateSelections()
        {
            Action_SelectionChanged(null, null);
        }

        public void SetIActions(IActions actions)
        {
            // this reset is required to trigger SelectionChanged events
            Reset();
            base.SetIActions(actions);

            // update UI
            this.ActionComboBox.SelectedIndex = (int)actions.ActionType;
        }

        public IActions GetIActions()
        {
            return Actions;
        }

        private void Action_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActionComboBox.SelectedItem is null)
                return;

            // we're not ready yet
            if (TargetComboBox is null)
                return;

            // clear current dropdown values
            TargetComboBox.ItemsSource = null;
            TargetComboBox.Items.Clear();
            TargetComboBox.IsEnabled = ActionComboBox.SelectedIndex != 0;

            // get current controller
            IController controller = ControllerManager.GetEmulatedController();

            // populate target dropdown based on action type
            ActionType type = (ActionType)ActionComboBox.SelectedIndex;

            if (type == ActionType.Disabled)
            {
                if (this.Actions is not null)
                    base.Delete();
                return;
            }

            if (type == ActionType.Button)
            {
                if (this.Actions is null || this.Actions is not ButtonActions)
                    this.Actions = new ButtonActions();

                foreach (ButtonFlags button in IController.GetTargetButtons())
                {
                    // create a label, store ButtonFlags as Tag and Label as controller specific string
                    Label buttonLabel = new Label() { Tag = button, Content = controller.GetButtonName(button) };
                    TargetComboBox.Items.Add(buttonLabel);

                    if (button.Equals(((ButtonActions)this.Actions).Button))
                        TargetComboBox.SelectedItem = buttonLabel;
                }

                // settings
                if (TargetComboBox.SelectedItem is not null)
                    PressComboBox.SelectedIndex = (int)this.Actions.PressType;
                else
                    this.Actions.PressType = (PressType)PressComboBox.SelectedIndex;
                LongPressDelaySlider.Value = (int)this.Actions.LongPressTime;
                Button2ButtonPressDelay.Visibility = Actions.PressType == PressType.Long ? Visibility.Visible : Visibility.Collapsed;
                Toggle_Turbo.IsOn = this.Actions.Turbo;
                Turbo_Slider.Value = this.Actions.TurboDelay;
                Toggle_Toggle.IsOn = this.Actions.Toggle;
            }
            else if (type == ActionType.Keyboard)
            {
                if (this.Actions is null || this.Actions is not KeyboardActions)
                    this.Actions = new KeyboardActions();

                TargetComboBox.ItemsSource = keyList;

                foreach (var keyLabel in keyList)
                    if (keyLabel.Tag.Equals(((KeyboardActions)this.Actions).Key))
                        TargetComboBox.SelectedItem = keyLabel;

                // settings
                if (TargetComboBox.SelectedItem is not null)
                    PressComboBox.SelectedIndex = (int)this.Actions.PressType;
                else
                    this.Actions.PressType = (PressType)PressComboBox.SelectedIndex;
                LongPressDelaySlider.Value = (int)this.Actions.LongPressTime;
                Button2ButtonPressDelay.Visibility = Actions.PressType == PressType.Long ? Visibility.Visible : Visibility.Collapsed;
                Toggle_Turbo.IsOn = this.Actions.Turbo;
                Turbo_Slider.Value = this.Actions.TurboDelay;
                Toggle_Toggle.IsOn = this.Actions.Toggle;
                ModifierComboBox.SelectedIndex = (int)((KeyboardActions)this.Actions).Modifiers;
            }
            else if (type == ActionType.Mouse)
            {
                if (this.Actions is null || this.Actions is not MouseActions)
                    this.Actions = new MouseActions();

                foreach (MouseActionsType mouseType in Enum.GetValues(typeof(MouseActionsType)))
                {
                    // skip specific scenarios
                    switch (mouseType)
                    {
                        case MouseActionsType.Move:
                        case MouseActionsType.Scroll:
                            continue;
                    }

                    // create a label, store MouseActionsType as Tag and Label as controller specific string
                    Label buttonLabel = new Label() { Tag = mouseType, Content = EnumUtils.GetDescriptionFromEnumValue(mouseType) };
                    TargetComboBox.Items.Add(buttonLabel);

                    if (mouseType.Equals(((MouseActions)this.Actions).MouseType))
                        TargetComboBox.SelectedItem = buttonLabel;
                }

                if (TargetComboBox.SelectedItem is not null)
                    PressComboBox.SelectedIndex = (int)this.Actions.PressType;
                else
                    this.Actions.PressType = (PressType)PressComboBox.SelectedIndex;
                LongPressDelaySlider.Value = (int)this.Actions.LongPressTime;
                Button2ButtonPressDelay.Visibility = Actions.PressType == PressType.Long ? Visibility.Visible : Visibility.Collapsed;
                Toggle_Turbo.IsOn = this.Actions.Turbo;
                Turbo_Slider.Value = this.Actions.TurboDelay;
                Toggle_Toggle.IsOn = this.Actions.Toggle;
                ModifierComboBox.SelectedIndex = (int)((MouseActions)this.Actions).Modifiers;
            }

            base.Update();
        }

        private void Target_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Actions is null)
                return;

            if (TargetComboBox.SelectedItem is null)
                return;

            // generate IActions based on settings
            switch (this.Actions.ActionType)
            {
                case ActionType.Button:
                    {
                        Label buttonLabel = TargetComboBox.SelectedItem as Label;
                        ((ButtonActions)this.Actions).Button = (ButtonFlags)buttonLabel.Tag;
                    }
                    break;

                case ActionType.Keyboard:
                    {
                        Label buttonLabel = TargetComboBox.SelectedItem as Label;
                        ((KeyboardActions)this.Actions).Key = (VirtualKeyCode)buttonLabel.Tag;
                    }
                    break;

                case ActionType.Mouse:
                    {
                        Label buttonLabel = TargetComboBox.SelectedItem as Label;
                        ((MouseActions)this.Actions).MouseType = (MouseActionsType)buttonLabel.Tag;
                    }
                    break;
            }

            base.Update();
        }

        public void Reset()
        {
            ActionComboBox.SelectedIndex = 0;
            TargetComboBox.SelectedItem = null;
        }

        private void Press_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Actions is null)
                return;

            this.Actions.PressType = (PressType)PressComboBox.SelectedIndex;

            Button2ButtonPressDelay.Visibility = Actions.PressType == PressType.Long ? Visibility.Visible : Visibility.Collapsed;

            base.Update();
        }

        private void LongPressDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            this.Actions.LongPressTime = (int)LongPressDelaySlider.Value;

            base.Update();
        }

        private void Modifier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Actions is null)
                return;

            ModifierSet mods = (ModifierSet)ModifierComboBox.SelectedIndex;

            switch (this.Actions.ActionType)
            {
                case ActionType.Keyboard:
                    ((KeyboardActions)this.Actions).Modifiers = mods;
                    break;
                case ActionType.Mouse:
                    ((MouseActions)this.Actions).Modifiers = mods;
                    break;
            }

            base.Update();
        }

        private void Toggle_Turbo_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.Actions is null)
                return;

            this.Actions.Turbo = Toggle_Turbo.IsOn;

            base.Update();
        }

        private void Turbo_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            this.Actions.TurboDelay = (int)Turbo_Slider.Value;

            base.Update();
        }

        private void Toggle_Toggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.Actions is null)
                return;

            this.Actions.Toggle = Toggle_Toggle.IsOn;

            base.Update();
        }
    }
}
