using GregsStack.InputSimulatorStandard.Native;
using HandheldCompanion.Actions;
using HandheldCompanion.Controllers;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Utils;
using ModernWpf.Controls;
using System;
using System.Threading;
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
        public ButtonMapping()
        {
            InitializeComponent();
        }

        public ButtonMapping(ButtonFlags button) : this()
        {
            this.Value = button;
            this.prevValue = button;
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

            // TODO: is this required?
            this.Update();
        }

        public void SetIActions(IActions actions)
        {
            // TODO: why is that reset required? Shouldn't update be enough?
            // reset and update mapping IActions
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

            // we're busy
            if (!Monitor.TryEnter(updateLock))
                return;

            // clear current dropdown values
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
                Toggle_Turbo.IsOn = this.Actions.Turbo;
                Turbo_Slider.Value = this.Actions.TurboDelay;
                Toggle_Toggle.IsOn = this.Actions.Toggle;
            }
            else if (type == ActionType.Keyboard)
            {
                if (this.Actions is null || this.Actions is not KeyboardActions)
                    this.Actions = new KeyboardActions();

                foreach (VirtualKeyCode key in Enum.GetValues(typeof(VirtualKeyCode)))
                {
                    // create a label, store VirtualKeyCode as Tag and Label as controller specific string
                    Label buttonLabel = new Label() { Tag = key, Content = KeyboardSimulator.GetVirtualKey(key) };
                    TargetComboBox.Items.Add(buttonLabel);

                    if (key.Equals(((KeyboardActions)this.Actions).Key))
                        TargetComboBox.SelectedItem = buttonLabel;
                }

                // settings
                if (TargetComboBox.SelectedItem is not null)
                    PressComboBox.SelectedIndex = (int)this.Actions.PressType;
                else
                    this.Actions.PressType = (PressType)PressComboBox.SelectedIndex;
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

            // we're busy
            if (!Monitor.TryEnter(updateLock))
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

        private void Update()
        {
            // force full update
            Action_SelectionChanged(null, null);
            Target_SelectionChanged(null, null);
        }

        public void Reset()
        {
            if (Monitor.TryEnter(updateLock))
            {
                ActionComboBox.SelectedIndex = 0;
                TargetComboBox.SelectedItem = null;
                Monitor.Exit(updateLock);
            }
        }

        private void Press_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Actions is null)
                return;

            this.Actions.PressType = (PressType)PressComboBox.SelectedIndex;

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
