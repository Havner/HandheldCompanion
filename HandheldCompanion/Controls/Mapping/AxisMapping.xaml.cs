using HandheldCompanion.Actions;
using HandheldCompanion.Controllers;
using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Utils;
using ModernWpf.Controls;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HandheldCompanion.Controls
{
    /// <summary>
    /// Interaction logic for AxisMapping.xaml
    /// </summary>
    public partial class AxisMapping : IMapping
    {
        public AxisMapping()
        {
            InitializeComponent();
        }

        public AxisMapping(AxisLayoutFlags axis) : this()
        {
            this.Value = axis;

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

            if (type == ActionType.Joystick)
            {
                if (this.Actions is null || this.Actions is not AxisActions)
                    this.Actions = new AxisActions();

                // we need a controller to get compatible buttons
                if (controller is null)
                    return;

                foreach (AxisLayoutFlags axis in IController.GetTargetAxis())
                {
                    // create a label, store ButtonFlags as Tag and Label as controller specific string
                    Label buttonLabel = new Label() { Tag = axis, Content = controller.GetAxisName(axis) };
                    TargetComboBox.Items.Add(buttonLabel);

                    if (axis.Equals(((AxisActions)this.Actions).Axis))
                        TargetComboBox.SelectedItem = buttonLabel;
                }

                // settings
                Axis2AxisImproveCircularity.IsOn = ((AxisActions)this.Actions).ImproveCircularity;
                Axis2AxisAntiDeadzone.Value = ((AxisActions)this.Actions).AxisAntiDeadZone;
                Axis2AxisInnerDeadzone.Value = ((AxisActions)this.Actions).AxisDeadZoneInner;
                Axis2AxisOuterDeadzone.Value = ((AxisActions)this.Actions).AxisDeadZoneOuter;
                Axis2AxisRotation.Value = (((AxisActions)this.Actions).AxisInverted ? 180 : 0) + (((AxisActions)this.Actions).AxisRotated ? 90 : 0);
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
                        case MouseActionsType.LeftButton:
                        case MouseActionsType.RightButton:
                        case MouseActionsType.MiddleButton:
                        case MouseActionsType.ScrollUp:
                        case MouseActionsType.ScrollDown:
                            continue;
                    }

                    // create a label, store MouseActionsType as Tag and Label as controller specific string
                    Label buttonLabel = new Label() { Tag = mouseType, Content = EnumUtils.GetDescriptionFromEnumValue(mouseType) };
                    TargetComboBox.Items.Add(buttonLabel);

                    if (mouseType.Equals(((MouseActions)this.Actions).MouseType))
                        TargetComboBox.SelectedItem = buttonLabel;
                }

                // settings
                Axis2MousePointerSpeed.Value = ((MouseActions)this.Actions).Sensivity;
                Axis2MouseAcceleration.Value = ((MouseActions)this.Actions).Acceleration;
                Axis2MouseDeadzone.Value = ((MouseActions)this.Actions).Deadzone;
                Axis2MouseFiltering.IsOn = ((MouseActions)this.Actions).Filtering;
                Axis2MouseFilterCutoff.Value = ((MouseActions)this.Actions).FilterCutoff;
                Axis2MouseRotation.Value = (((MouseActions)this.Actions).AxisInverted ? 180 : 0) + (((MouseActions)this.Actions).AxisRotated ? 90 : 0);
            }
            else if (type == ActionType.Special)
            {
                if (this.Actions is null || this.Actions is not SpecialActions)
                    this.Actions = new SpecialActions();

                foreach (SpecialActionsType specialType in Enum.GetValues(typeof(SpecialActionsType)))
                {
                    // create a label, store SpecialActionsType as Tag and Label as controller specific string
                    Label buttonLabel = new Label() { Tag = specialType, Content = EnumUtils.GetDescriptionFromEnumValue(specialType) };
                    TargetComboBox.Items.Add(buttonLabel);

                    if (specialType.Equals(((SpecialActions)this.Actions).SpecialType))
                        TargetComboBox.SelectedItem = buttonLabel;
                }

                // settings
                Axis2SpecialFlick.Value = ((SpecialActions)this.Actions).FlickSensitivity;
                Axis2SpecialSweep.Value = ((SpecialActions)this.Actions).SweepSensitivity;
                Axis2SpecialThreshold.Value = ((SpecialActions)this.Actions).FlickThreshold;
                Axis2SpecialSpeed.Value = ((SpecialActions)this.Actions).FlickSpeed;
                Axis2SpecialFrontAngle.Value = ((SpecialActions)this.Actions).FlickFrontAngleDeadzone;
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
                case ActionType.Joystick:
                    {
                        Label buttonLabel = TargetComboBox.SelectedItem as Label;
                        ((AxisActions)this.Actions).Axis = (AxisLayoutFlags)buttonLabel.Tag;
                    }
                    break;

                case ActionType.Mouse:
                    {
                        Label buttonLabel = TargetComboBox.SelectedItem as Label;
                        ((MouseActions)this.Actions).MouseType = (MouseActionsType)buttonLabel.Tag;
                    }
                    break;

                case ActionType.Special:
                    {
                        Label buttonLabel = TargetComboBox.SelectedItem as Label;
                        ((SpecialActions)this.Actions).SpecialType = (SpecialActionsType)buttonLabel.Tag;
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

        private void Axis2AxisImproveCircularity_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Joystick:
                    ((AxisActions)this.Actions).ImproveCircularity = Axis2AxisImproveCircularity.IsOn;
                    break;
            }

            base.Update();
        }

        private void Axis_AntiDeadZone_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Joystick:
                    ((AxisActions)this.Actions).AxisAntiDeadZone = (int)Axis2AxisAntiDeadzone.Value;
                    break;
            }

            base.Update();
        }

        private void Axis_InnerDeadzone_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Joystick:
                    ((AxisActions)this.Actions).AxisDeadZoneInner = (int)Axis2AxisInnerDeadzone.Value;
                    break;
            }

            base.Update();
        }

        private void Axis_OuterDeadzone_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Joystick:
                    ((AxisActions)this.Actions).AxisDeadZoneOuter = (int)Axis2AxisOuterDeadzone.Value;
                    break;
            }

            base.Update();
        }

        private void Axis_Rotation_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Joystick:
                    ((AxisActions)this.Actions).AxisInverted = (((int)Axis2AxisRotation.Value / 90) & 2) == 2;
                    ((AxisActions)this.Actions).AxisRotated = (((int)Axis2AxisRotation.Value / 90) & 1) == 1;
                    break;
            }

            base.Update();
        }

        private void Axis2MousePointerSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Mouse:
                    ((MouseActions)this.Actions).Sensivity = (int)Axis2MousePointerSpeed.Value;
                    break;
            }

            base.Update();
        }

        private void Axis2MouseAcceleration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Mouse:
                    ((MouseActions)this.Actions).Acceleration = (float)Axis2MouseAcceleration.Value;
                    break;
            }

            base.Update();
        }

        // TODO: artificially convert to something more human readable?
        private void Axis2MouseFiltering_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Mouse:
                    ((MouseActions)this.Actions).Filtering = Axis2MouseFiltering.IsOn;
                    break;
            }

            base.Update();
        }

        private void Axis2MouseFilterCutoff_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Mouse:
                    ((MouseActions)this.Actions).FilterCutoff = (float)Axis2MouseFilterCutoff.Value;
                    break;
            }

            base.Update();
        }

        private void Axis2MouseDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Mouse:
                    ((MouseActions)this.Actions).Deadzone = (int)Axis2MouseDeadzone.Value;
                    break;
            }

            base.Update();
        }

        private void Axis2MouseRotation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Mouse:
                    ((MouseActions)this.Actions).AxisInverted = (((int)Axis2MouseRotation.Value / 90) & 2) == 2;
                    ((MouseActions)this.Actions).AxisRotated = (((int)Axis2MouseRotation.Value / 90) & 1) == 1;
                    break;
            }

            base.Update();
        }

        private void Axis2SpecialFlick_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Special:
                    ((SpecialActions)this.Actions).FlickSensitivity = (float)Axis2SpecialFlick.Value;
                    break;
            }

            base.Update();
        }

        private void Axis2SpecialSweep_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Special:
                    ((SpecialActions)this.Actions).SweepSensitivity = (float)Axis2SpecialSweep.Value;
                    break;
            }

            base.Update();
        }

        private void Axis2SpecialThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Special:
                    ((SpecialActions)this.Actions).FlickThreshold = (float)Axis2SpecialThreshold.Value;
                    break;
            }

            base.Update();
        }

        private void Axis2SpecialSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Special:
                    ((SpecialActions)this.Actions).FlickSpeed = (int)Axis2SpecialSpeed.Value;
                    break;
            }

            base.Update();
        }

        private void Axis2SpecialAngleDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Actions is null)
                return;

            switch (this.Actions.ActionType)
            {
                case ActionType.Special:
                    ((SpecialActions)this.Actions).FlickFrontAngleDeadzone = (int)Axis2SpecialFrontAngle.Value;
                    break;
            }

            base.Update();
        }
    }
}