using HandheldCompanion.Actions;
using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Inputs;
using ModernWpf.Controls;
using System.Collections.Generic;
using System.Windows;
using Layout = HandheldCompanion.Misc.Layout;

namespace HandheldCompanion.Views.Pages
{
    public class ILayoutPage : Page
    {
        protected bool enabled = false;

        public Dictionary<ButtonFlags, ButtonStack> ButtonStacks = new();
        public Dictionary<AxisLayoutFlags, AxisStack> AxisStacks = new();
        public Dictionary<AxisLayoutFlags, TriggerStack> TriggerStacks = new();

        protected bool CheckController(IController controller, List<ButtonFlags> buttons)
        {
            foreach (ButtonFlags button in buttons)
                if (controller.HasSourceButton(button))
                    return true;
            return false;
        }

        protected bool CheckController(IController controller, List<AxisLayoutFlags> axes)
        {
            foreach (AxisLayoutFlags axis in axes)
                if (controller.HasSourceAxis(axis))
                    return true;
            return false;
        }

        public bool IsEnabled()
        {
            return enabled;
        }

        public virtual void UpdateController(IController controller)
        {
            // controller based
            foreach (var pair in ButtonStacks)
            {
                ButtonFlags button = pair.Key;
                ButtonStack buttonStack = pair.Value;

                // update mapping visibility
                if (!controller.HasSourceButton(button))
                    buttonStack.Visibility = Visibility.Collapsed;
                else
                {
                    buttonStack.Visibility = Visibility.Visible;

                    // update icon
                    FontIcon newIcon = controller.GetFontIcon(button);
                    string newLabel = controller.GetButtonName(button);
                    buttonStack.UpdateIcon(newIcon, newLabel);
                }
            }

            foreach (var pair in AxisStacks)
            {
                AxisLayoutFlags flags = pair.Key;
                AxisStack axisStack = pair.Value;

                // update mapping visibility
                if (!controller.HasSourceAxis(flags))
                    axisStack.Visibility = Visibility.Collapsed;
                else
                {
                    axisStack.Visibility = Visibility.Visible;

                    // update icon
                    FontIcon newIcon = controller.GetFontIcon(flags);
                    string newLabel = controller.GetAxisName(flags);
                    axisStack.UpdateIcon(newIcon, newLabel);
                }
            }

            foreach (var pair in TriggerStacks)
            {
                AxisLayoutFlags flags = pair.Key;
                TriggerStack axisStack = pair.Value;

                // update mapping visibility
                if (!controller.HasSourceAxis(flags))
                    axisStack.Visibility = Visibility.Collapsed;
                else
                {
                    axisStack.Visibility = Visibility.Visible;

                    // update icon
                    FontIcon newIcon = controller.GetFontIcon(flags);
                    string newLabel = controller.GetAxisName(flags);
                    axisStack.UpdateIcon(newIcon, newLabel);
                }
            }
        }

        public virtual void UpdateSelections()
        {
            foreach (var pair in ButtonStacks)
                pair.Value.UpdateSelections();

            foreach (var pair in AxisStacks)
                pair.Value.UpdateSelections();

            foreach (var pair in TriggerStacks)
                pair.Value.UpdateSelections();
        }

        public void Update(Layout layout)
        {
            foreach (var pair in ButtonStacks)
            {
                ButtonFlags button = pair.Key;
                ButtonStack mappings = pair.Value;

                if (layout.ButtonLayout.TryGetValue(button, out List<IActions> actions))
                {
                    mappings.SetActions(actions);
                    continue;
                }

                mappings.Reset();
            }

            foreach (var pair in AxisStacks)
            {
                AxisLayoutFlags axis = pair.Key;
                AxisStack mappings = pair.Value;

                if (layout.AxisLayout.TryGetValue(axis, out List<IActions> actions))
                {
                    mappings.SetActions(actions);
                    continue;
                }

                mappings.Reset();
            }

            foreach (var pair in TriggerStacks)
            {
                AxisLayoutFlags axis = pair.Key;
                TriggerStack mappings = pair.Value;

                if (layout.AxisLayout.TryGetValue(axis, out List<IActions> actions))
                {
                    mappings.SetActions(actions);
                    continue;
                }

                mappings.Reset();
            }
        }
    }
}
