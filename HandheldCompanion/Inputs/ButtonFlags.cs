using System;
using System.ComponentModel;

namespace HandheldCompanion.Inputs
{
    [Serializable]
    public enum ButtonFlags : byte
    {
        None = 0,

        [Description("DPad Up")]
        DPadUp = 1,
        [Description("DPad Down")]
        DPadDown = 2,
        [Description("DPad Left")]
        DPadLeft = 3,
        [Description("DPad Right")]
        DPadRight = 4,

        [Description("Start")]
        Start = 5,
        [Description("Back")]
        Back = 6,

        [Description("Left Stick Click")]
        LeftStickClick = 7,
        [Description("Right Stick Click")]
        RightStickClick = 8,

        L1 = 9,  // Left Shoulder
        R1 = 10, // Right Shoulder

        [Description("Soft pull")]
        L2Soft = 11,
        [Description("Soft pull")]
        R2Soft = 12,
        [Description("Full pull")]
        L2Full = 13,
        [Description("Full pull")]
        R2Full = 14,

        B1 = 15,  // A / Cross
        B2 = 16,  // B / Circle
        B3 = 17,  // X / Square
        B4 = 18,  // Y / Triangle

        [Description("Up")]
        LeftStickUp = 19,
        [Description("Down")]
        LeftStickDown = 20,
        [Description("Left")]
        LeftStickLeft = 21,
        [Description("Right")]
        LeftStickRight = 22,

        [Description("Up")]
        RightStickUp = 23,
        [Description("Down")]
        RightStickDown = 24,
        [Description("Left")]
        RightStickLeft = 25,
        [Description("Right")]
        RightStickRight = 26,

        Special = 27,
        Quick = 28,   // Steam Deck

        // Steam, DS4
        [Description("Left Pad Touch")]
        LeftPadTouch = 29,
        [Description("Right Pad Touch")]
        RightPadTouch = 30,

        [Description("Left Pad Click")]
        LeftPadClick = 31,
        [Description("Right Pad Click")]
        RightPadClick = 32,

        // Steam
        L4 = 33, R4 = 34,
        L5 = 35, R5 = 36,

        [Description("Left Stick Touch")]
        LeftStickTouch = 37,
        [Description("Right Stick Touch")]
        RightStickTouch = 38,

        [Description("Up")]
        LeftPadClickUp = 39,
        [Description("Down")]
        LeftPadClickDown = 40,
        [Description("Left")]
        LeftPadClickLeft = 41,
        [Description("Right")]
        LeftPadClickRight = 42,

        [Description("Up")]
        RightPadClickUp = 43,
        [Description("Down")]
        RightPadClickDown = 44,
        [Description("Left")]
        RightPadClickLeft = 45,
        [Description("Right")]
        RightPadClickRight = 46,

        [Description("Outer Ring")]
        LeftStickOuterRing = 47,
        [Description("Inner Ring")]
        LeftStickInnerRing = 48,
        [Description("Outer Ring")]
        RightStickOuterRing = 49,
        [Description("Inner Ring")]
        RightStickInnerRing = 50,

        [Description("Volume Up")]
        VolumeUp = 51,
        [Description("Volume Down")]
        VolumeDown = 52,

        OEM1 = 53, OEM2 = 54, OEM3 = 55, OEM4 = 56, OEM5 = 57,
        OEM6 = 58, OEM7 = 59, OEM8 = 60, OEM9 = 61, OEM10 = 62,
    }
}
