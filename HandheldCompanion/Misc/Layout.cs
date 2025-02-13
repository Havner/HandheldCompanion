using HandheldCompanion.Actions;
using HandheldCompanion.Controllers;
using HandheldCompanion.Inputs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace HandheldCompanion.Misc
{
    [Serializable]
    public class Layout : ICloneable, IDisposable
    {
        public SortedDictionary<ButtonFlags, List<IActions>> ButtonLayout { get; set; } = new();
        public SortedDictionary<AxisLayoutFlags, IActions> AxisLayout { get; set; } = new();

        #region events
        public event UpdatedEventHandler Updated;
        public delegate void UpdatedEventHandler(Layout layout);
        #endregion

        public Layout() { }

        public Layout(bool fill) : this()
        {
            // generic button mapping
            foreach (ButtonFlags button in Enum.GetValues(typeof(ButtonFlags)))
            {
                if (!IController.TargetButtons.Contains(button))
                    continue;

                ButtonLayout[button] = new List<IActions>() { new ButtonActions() { Button = button } };
            }

            // generic axis mapping
            foreach (AxisLayoutFlags axis in Enum.GetValues(typeof(AxisLayoutFlags)))
            {
                if (!IController.TargetAxis.Contains(axis))
                    continue;

                switch (axis)
                {
                    case AxisLayoutFlags.L2:
                    case AxisLayoutFlags.R2:
                        AxisLayout[axis] = new TriggerActions() { Axis = axis };
                        break;
                    default:
                        AxisLayout[axis] = new AxisActions() { Axis = axis };
                        break;
                }
            }
        }

        public void UpdateLayout()
        {
            Updated?.Invoke(this);
        }

        public void UpdateLayout(ButtonFlags button, List<IActions> actions)
        {
            ButtonLayout[button] = actions;
            Updated?.Invoke(this);
        }

        public void UpdateLayout(AxisLayoutFlags axis, IActions action)
        {
            AxisLayout[axis] = action;
            Updated?.Invoke(this);
        }

        public void RemoveLayout(ButtonFlags button)
        {
            ButtonLayout.Remove(button);
            Updated?.Invoke(this);
        }

        public void RemoveLayout(AxisLayoutFlags axis)
        {
            AxisLayout.Remove(axis);
            Updated?.Invoke(this);
        }

        public object Clone()
        {
            string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            return JsonConvert.DeserializeObject<Layout>(jsonString, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
        }

        public void Dispose()
        {
            ButtonLayout.Clear();
            AxisLayout.Clear();
        }
    }
}
