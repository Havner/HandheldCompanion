using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;

namespace HandheldCompanion.Inputs
{
    [Serializable]
    public struct AxisLayout
    {
        public static AxisLayout None = new AxisLayout(AxisLayoutFlags.None);
        public static AxisLayout LeftStick = new AxisLayout(AxisLayoutFlags.LeftStick, AxisFlags.LeftStickX, AxisFlags.LeftStickY);
        public static AxisLayout RightStick = new AxisLayout(AxisLayoutFlags.RightStick, AxisFlags.RightStickX, AxisFlags.RightStickY);
        public static AxisLayout L2 = new AxisLayout(AxisLayoutFlags.L2, AxisFlags.L2);
        public static AxisLayout R2 = new AxisLayout(AxisLayoutFlags.R2, AxisFlags.R2);
        public static AxisLayout LeftPad = new AxisLayout(AxisLayoutFlags.LeftPad, AxisFlags.LeftPadX, AxisFlags.LeftPadY);
        public static AxisLayout RightPad = new AxisLayout(AxisLayoutFlags.RightPad, AxisFlags.RightPadX, AxisFlags.RightPadY);
        public static AxisLayout Gyroscope = new AxisLayout(AxisLayoutFlags.Gyroscope, AxisFlags.GyroX, AxisFlags.GyroY);

        public static SortedDictionary<AxisLayoutFlags, AxisLayout> Layouts = new()
        {
            { AxisLayoutFlags.None, None },
            { AxisLayoutFlags.LeftStick, LeftStick },
            { AxisLayoutFlags.RightStick, RightStick },
            { AxisLayoutFlags.L2, L2 },
            { AxisLayoutFlags.R2, R2 },
            { AxisLayoutFlags.LeftPad, LeftPad },
            { AxisLayoutFlags.RightPad, RightPad },
            { AxisLayoutFlags.Gyroscope, Gyroscope },
        };

        public AxisLayoutFlags flags = AxisLayoutFlags.None;
        private SortedDictionary<char, AxisFlags> axis = new();
        public Vector2 vector = new();

        public AxisLayout(AxisLayoutFlags flags)
        {
            this.flags = flags;
        }

        public AxisLayout(AxisLayoutFlags flags, AxisFlags axisY) : this(flags)
        {
            axis.Add('Y', axisY);
        }

        public AxisLayout(AxisLayoutFlags flags, AxisFlags axisX, AxisFlags axisY) : this(flags, axisY)
        {
            axis.Add('X', axisX);
        }

        public AxisFlags GetAxisFlags(char axisName)
        {
            if (axis.TryGetValue(axisName, out AxisFlags foundAxis))
                return foundAxis;

            return AxisFlags.None;
        }
    }

    [Serializable]
    public enum AxisLayoutFlags : byte
    {
        None = 0,

        [Description("Left Stick")]
        LeftStick = 1,
        [Description("Right Stick")]
        RightStick = 2,

        L2 = 3,
        R2 = 4,

        // Steam Deck, DS4
        [Description("Left Pad")]
        LeftPad = 5,
        [Description("Right Pad")]
        RightPad = 6,

        [Description("Gyroscope")]
        Gyroscope = 7,
    }
}
