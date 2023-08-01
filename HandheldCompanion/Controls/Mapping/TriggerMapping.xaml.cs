using HandheldCompanion.Actions;
using HandheldCompanion.Controllers;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using ModernWpf.Controls;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HandheldCompanion.Controls
{
    /// <summary>
    /// Interaction logic for TriggerMapping.xaml
    /// </summary>
    public partial class TriggerMapping : IMapping
    {
        public TriggerMapping()
        {
            InitializeComponent();
        }

        public TriggerMapping(AxisLayoutFlags axis) : this()
        {
            this.Value = axis;

            foreach (ButtonFlags button in Enum.GetValues(typeof(ButtonFlags)))
            {
                Label buttonLabel = new Label() { Tag = button, Content = button.ToString() };
                ShiftComboBox.Items.Add(buttonLabel);
            }
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

        internal void SetIActions(IActions actions)
        {
            // this reset is required to trigger SelectionChanged events
            Reset();
            base.SetIActions(actions);

            // update UI
            this.ActionComboBox.SelectedIndex = (int)actions.ActionType;
        }

        private void Action_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActionComboBox.SelectedItem is null)
                return;

            // we're not ready yet
            if (TargetComboBox is null)
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

            if (type == ActionType.Trigger)
            {
                if (this.Actions is null || this.Actions is not TriggerActions)
                    this.Actions = new TriggerActions();

                // we need a controller to get compatible buttons
                if (controller is null)
                    return;

                foreach (AxisLayoutFlags axis in IController.GetTargetTriggers())
                {
                    // create a label, store AxisLayoutFlags as Tag and Label as controller specific string
                    Label buttonLabel = new Label() { Tag = axis, Content = controller.GetAxisName(axis) };
                    TargetComboBox.Items.Add(buttonLabel);

                    if (axis.Equals(((TriggerActions)this.Actions).Axis))
                        TargetComboBox.SelectedItem = buttonLabel;
                }

                // settings
                Trigger2TriggerInnerDeadzone.Value = ((TriggerActions)this.Actions).AxisDeadZoneInner;
                Trigger2TriggerOuterDeadzone.Value = ((TriggerActions)this.Actions).AxisDeadZoneOuter;
                Trigger2TriggerAntiDeadzone.Value = ((TriggerActions)this.Actions).AxisAntiDeadZone;
            }

            if (TargetComboBox.SelectedItem is not null)
            {
                foreach (Label label in ShiftComboBox.Items)
                    if (label.Tag.Equals(this.Actions.ShiftButton))
                        ShiftComboBox.SelectedItem = label;
            }
            else
                this.Actions.ShiftButton = (ButtonFlags)((Label)ShiftComboBox.SelectedItem).Tag;

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
                case ActionType.Trigger:
                    {
                        Label buttonLabel = TargetComboBox.SelectedItem as Label;
                        ((TriggerActions)this.Actions).Axis = (AxisLayoutFlags)buttonLabel.Tag;
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

        private void Shift_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Actions is null)
                return;

            this.Actions.ShiftButton = (ButtonFlags)((Label)ShiftComboBox.SelectedItem).Tag;

            base.Update();
        }

        private void Trigger2TriggerInnerDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Trigger:
                    ((TriggerActions)this.Actions).AxisDeadZoneInner = (int)Trigger2TriggerInnerDeadzone.Value;
                    break;
            }

            base.Update();
        }

        private void Trigger2TriggerOuterDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Trigger:
                    ((TriggerActions)this.Actions).AxisDeadZoneOuter = (int)Trigger2TriggerOuterDeadzone.Value;
                    break;
            }

            base.Update();
        }

        private void Trigger2TriggerAntiDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Trigger:
                    ((TriggerActions)this.Actions).AxisAntiDeadZone = (int)Trigger2TriggerAntiDeadzone.Value;
                    break;
            }

            base.Update();
        }
    }
}
