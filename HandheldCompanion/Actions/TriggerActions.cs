using HandheldCompanion.Inputs;
using HandheldCompanion.Utils;
using System;

namespace HandheldCompanion.Actions
{
    [Serializable]
    public class TriggerActions : IActions
    {
        public AxisLayoutFlags Axis;

        // runtime button variables
        private bool IsKeyDown = false;

        // settings
        public int AxisAntiDeadZone = 0;
        public int AxisDeadZoneInner = 0;
        public int AxisDeadZoneOuter = 0;

        public TriggerActions()
        {
            this.ActionType = ActionType.Trigger;
            this.Value = (short)0;
            this.prevValue = false;
        }

        public TriggerActions(AxisLayoutFlags axis) : this()
        {
            this.Axis = axis;
        }

        public void Execute(AxisFlags axis, short value)
        {
            // Apply inner and outer deadzone adjustments
            value = (short)InputUtils.InnerOuterDeadzone(value, AxisDeadZoneInner, AxisDeadZoneOuter, byte.MaxValue);
            value = (short)InputUtils.ApplyAntiDeadzone(value, AxisAntiDeadZone, byte.MaxValue);

            this.Value = value;
        }

        public override void Execute(ButtonFlags button, bool value, int longTime)
        {
            base.Execute(button, value, longTime);

            switch (this.Value)
            {
                case true:
                    {
                        if (IsKeyDown)
                            return;

                        IsKeyDown = true;
                        SetHaptic(button, false);
                    }
                    break;
                case false:
                    {
                        if (!IsKeyDown)
                            return;

                        IsKeyDown = false;
                        SetHaptic(button, true);
                    }
                    break;
            }
        }

        public short GetValue()
        {
            if (this.Value is bool)
                return (bool)this.Value ? (short)byte.MaxValue : (short)0;
            else
                return (short)this.Value;
        }
    }
}
