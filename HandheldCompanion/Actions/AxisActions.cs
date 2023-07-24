using HandheldCompanion.Inputs;
using HandheldCompanion.Utils;
using System;
using System.Numerics;

namespace HandheldCompanion.Actions
{
    [Serializable]
    public class AxisActions : IActions
    {
        public AxisLayoutFlags Axis { get; set; }

        // Axis to axis
        public bool ImproveCircularity { get; set; } = false;
        public int AxisAntiDeadZone { get; set; } = 0;
        public int AxisDeadZoneInner { get; set; } = 0;
        public int AxisDeadZoneOuter { get; set; } = 0;
        public bool AxisRotated { get; set; } = false;
        public bool AxisInverted { get; set; } = false;

        public AxisActions()
        {
            this.ActionType = ActionType.Joystick;
            this.Value = new Vector2();
        }

        public AxisActions(AxisLayoutFlags axis) : this()
        {
            this.Axis = axis;
        }

        public void Execute(AxisLayout layout)
        {
            layout.vector = InputUtils.ThumbScaledRadialInnerOuterDeadzone(layout.vector, AxisDeadZoneInner, AxisDeadZoneOuter);
            layout.vector = InputUtils.ApplyAntiDeadzone(layout.vector, AxisAntiDeadZone);

            if (ImproveCircularity)
                layout.vector = InputUtils.ImproveCircularity(layout.vector);

            this.Value = (AxisRotated ? new(layout.vector.Y, -layout.vector.X) : layout.vector) * (AxisInverted ? -1.0f : 1.0f);
        }

        public Vector2 GetValue()
        {
            return (Vector2)this.Value;
        }
    }
}
