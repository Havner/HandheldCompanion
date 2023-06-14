using ControllerCommon.Inputs;
using ControllerCommon.Utils;
using System;
using System.Numerics;

namespace ControllerCommon.Actions
{
    [Serializable]
    public class AxisActions : IActions
    {
        public AxisLayoutFlags Axis { get; set; }
        private Vector2 Vector;

        // Axis to axis
        public bool AxisInverted { get; set; } = false;
        public bool AxisRotated { get; set; } = false;
        public int AxisDeadZoneInner { get; set; } = 0;
        public int AxisDeadZoneOuter { get; set; } = 0;
        public int AxisAntiDeadZone { get; set; } = 0;
        public bool ImproveCircularity { get; set; } = false;

        public AxisActions()
        {
            this.ActionType = ActionType.Joystick;
            this.Vector = new();
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

            this.Vector = (AxisRotated ? new(layout.vector.Y, -layout.vector.X) : layout.vector) * (AxisInverted ? -1.0f : 1.0f);
        }

        public Vector2 GetValue()
        {
            return this.Vector;
        }
    }
}
