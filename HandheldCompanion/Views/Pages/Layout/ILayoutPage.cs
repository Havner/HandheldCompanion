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
        public Dictionary<ButtonFlags, ButtonStack> ButtonStacks = new();
        public Dictionary<AxisLayoutFlags, AxisMapping> AxisMappings = new();
        public Dictionary<AxisLayoutFlags, TriggerMapping> TriggerMappings = new();

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

            foreach (var pair in AxisMappings)
            {
                AxisLayoutFlags flags = pair.Key;
                AxisMapping axisMapping = pair.Value;

                // update mapping visibility
                if (!controller.HasSourceAxis(flags))
                    axisMapping.Visibility = Visibility.Collapsed;
                else
                {
                    axisMapping.Visibility = Visibility.Visible;

                    // update icon
                    FontIcon newIcon = controller.GetFontIcon(flags);
                    string newLabel = controller.GetAxisName(flags);
                    axisMapping.UpdateIcon(newIcon, newLabel);
                }
            }

            foreach (var pair in TriggerMappings)
            {
                AxisLayoutFlags flags = pair.Key;
                TriggerMapping axisMapping = pair.Value;

                // update mapping visibility
                if (!controller.HasSourceAxis(flags))
                    axisMapping.Visibility = Visibility.Collapsed;
                else
                {
                    axisMapping.Visibility = Visibility.Visible;

                    // update icon
                    FontIcon newIcon = controller.GetFontIcon(flags);
                    string newLabel = controller.GetAxisName(flags);
                    axisMapping.UpdateIcon(newIcon, newLabel);
                }
            }
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

            foreach (var pair in AxisMappings)
            {
                AxisLayoutFlags axis = pair.Key;
                AxisMapping mapping = pair.Value;

                if (layout.AxisLayout.TryGetValue(axis, out IActions actions))
                {
                    mapping.SetIActions(actions);
                    continue;
                }

                mapping.Reset();
            }

            foreach (var pair in TriggerMappings)
            {
                AxisLayoutFlags axis = pair.Key;
                TriggerMapping mapping = pair.Value;

                if (layout.AxisLayout.TryGetValue(axis, out IActions actions))
                {
                    mapping.SetIActions(actions);
                    continue;
                }

                mapping.Reset();
            }
        }
    }
}
