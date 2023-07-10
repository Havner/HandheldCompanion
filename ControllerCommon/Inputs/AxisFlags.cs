using System;

namespace ControllerCommon.Inputs
{
    [Serializable]
    public enum AxisFlags : byte
    {
        None = 0,
        LeftStickX = 1, LeftStickY = 2,
        RightStickX = 3, RightStickY = 4,
        L2 = 5, R2 = 6,

        // Steam Deck
        LeftPadX = 7, RightPadX = 8,
        LeftPadY = 9, RightPadY = 10,
    }
}
