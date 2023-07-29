using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using steam_hidapi.net;
using steam_hidapi.net.Hid;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace HandheldCompanion.Controllers
{
    public class GordonController : IController
    {
        private steam_hidapi.net.GordonController Controller;
        private GordonControllerInputEventArgs input;

        private bool isConnected = false;
        private bool isVirtualMuted = false;

        private const short TrackPadInner = 12000;

        public byte FeedbackLargeMotor;
        public byte FeedbackSmallMotor;

        public const sbyte MinIntensity = 5;
        public const sbyte MaxIntensity = 15;
        private readonly ushort RumblePeriod = 80;

        // TODO: why not use TimerManager.Tick?
        private Thread rumbleThread;
        private bool rumbleThreadRunning;

        private Task<byte[]> lastLeftHapticOn;
        private Task<byte[]> lastRightHapticOn;

        public GordonController(PnPDetails details, short index)
        {
            if (details is null)
                return;

            Controller = new(details.attributes.VersionNumber, index);
            isConnected = true;

            Details = details;
            Details.isHooked = true;

            Capabilities |= ControllerCapabilities.MotionSensor;
            Capabilities |= ControllerCapabilities.Trackpads;

            // TODO: generalize for steam controller and steam deck
            bool Muted = SettingsManager.GetBoolean("SteamDeckMuteController");
            SetVirtualMuted(Muted);

            // UI
            DrawControls();
            RefreshControls();

            // Additional controller specific source buttons/axes
            SourceButtons.AddRange(new List<ButtonFlags>() { ButtonFlags.L4, ButtonFlags.R4, ButtonFlags.L5, ButtonFlags.R5 });
            SourceButtons.AddRange(new List<ButtonFlags>() { ButtonFlags.LeftStickTouch, ButtonFlags.RightStickTouch });
            SourceButtons.AddRange(new List<ButtonFlags>() { ButtonFlags.LeftPadClick, ButtonFlags.LeftPadTouch, ButtonFlags.LeftPadClickUp, ButtonFlags.LeftPadClickDown, ButtonFlags.LeftPadClickLeft, ButtonFlags.LeftPadClickRight });
            SourceButtons.AddRange(new List<ButtonFlags>() { ButtonFlags.RightPadClick, ButtonFlags.RightPadTouch, ButtonFlags.RightPadClickUp, ButtonFlags.RightPadClickDown, ButtonFlags.RightPadClickLeft, ButtonFlags.RightPadClickRight });

            SourceAxis.Add(AxisLayoutFlags.LeftPad);
            SourceAxis.Add(AxisLayoutFlags.RightPad);
            SourceAxis.Add(AxisLayoutFlags.Gyroscope);
        }

        private async void RumbleThreadLoop(object? obj)
        {
            while (rumbleThreadRunning)
            {
                if (GetHapticIntensity(FeedbackLargeMotor, MinIntensity, MaxIntensity, out var leftIntensity))
                    lastLeftHapticOn = Controller.SetHaptic2(HapticPad.Left, HapticStyle.Weak, leftIntensity);

                if (GetHapticIntensity(FeedbackSmallMotor, MinIntensity, MaxIntensity, out var rightIntensity))
                    lastRightHapticOn = Controller.SetHaptic2(HapticPad.Right, HapticStyle.Weak, rightIntensity);

                await Task.Delay(TimerManager.GetPeriod() * 2);

                if (lastLeftHapticOn is not null)
                    await lastLeftHapticOn;
                if (lastRightHapticOn is not null)
                    await lastRightHapticOn;
            }
        }

        public override string ToString()
        {
            string baseName = base.ToString();
            if (!string.IsNullOrEmpty(baseName))
                return baseName;
            return "Steam Controller Gordon";
        }

        public override void UpdateInputs(long ticks)
        {
            if (input is null)
                return;

            Inputs.ButtonState = InjectedButtons.Clone() as ButtonState;

            Inputs.ButtonState[ButtonFlags.B1] = input.State.ButtonState[GordonControllerButton.BtnA];
            Inputs.ButtonState[ButtonFlags.B2] = input.State.ButtonState[GordonControllerButton.BtnB];
            Inputs.ButtonState[ButtonFlags.B3] = input.State.ButtonState[GordonControllerButton.BtnX];
            Inputs.ButtonState[ButtonFlags.B4] = input.State.ButtonState[GordonControllerButton.BtnY];

            Inputs.ButtonState[ButtonFlags.DPadUp] = input.State.ButtonState[GordonControllerButton.BtnDpadUp];
            Inputs.ButtonState[ButtonFlags.DPadDown] = input.State.ButtonState[GordonControllerButton.BtnDpadDown];
            Inputs.ButtonState[ButtonFlags.DPadLeft] = input.State.ButtonState[GordonControllerButton.BtnDpadLeft];
            Inputs.ButtonState[ButtonFlags.DPadRight] = input.State.ButtonState[GordonControllerButton.BtnDpadRight];

            Inputs.ButtonState[ButtonFlags.Start] = input.State.ButtonState[GordonControllerButton.BtnOptions];
            Inputs.ButtonState[ButtonFlags.Back] = input.State.ButtonState[GordonControllerButton.BtnMenu];
            Inputs.ButtonState[ButtonFlags.Special] = input.State.ButtonState[GordonControllerButton.BtnSteam];

            var L2 = input.State.AxesState[GordonControllerAxis.L2];
            var R2 = input.State.AxesState[GordonControllerAxis.R2];

            Inputs.ButtonState[ButtonFlags.L2Soft] = L2 > Gamepad.TriggerThreshold * 2;
            Inputs.ButtonState[ButtonFlags.R2Soft] = R2 > Gamepad.TriggerThreshold * 2;

            Inputs.ButtonState[ButtonFlags.L2Full] = L2 > Gamepad.TriggerThreshold * 8;
            Inputs.ButtonState[ButtonFlags.R2Full] = R2 > Gamepad.TriggerThreshold * 8;

            Inputs.AxisState[AxisFlags.L2] = (short)L2;
            Inputs.AxisState[AxisFlags.R2] = (short)R2;

            Inputs.ButtonState[ButtonFlags.L1] = input.State.ButtonState[GordonControllerButton.BtnL1];
            Inputs.ButtonState[ButtonFlags.R1] = input.State.ButtonState[GordonControllerButton.BtnR1];
            Inputs.ButtonState[ButtonFlags.L4] = input.State.ButtonState[GordonControllerButton.BtnL4];
            Inputs.ButtonState[ButtonFlags.R4] = input.State.ButtonState[GordonControllerButton.BtnR4];

            // Left Stick
            Inputs.ButtonState[ButtonFlags.LeftStickClick] = input.State.ButtonState[GordonControllerButton.BtnLStickPress];

            Inputs.AxisState[AxisFlags.LeftStickX] = input.State.AxesState[GordonControllerAxis.LeftStickX];
            Inputs.AxisState[AxisFlags.LeftStickY] = input.State.AxesState[GordonControllerAxis.LeftStickY];

            Inputs.ButtonState[ButtonFlags.LeftStickLeft] = Inputs.AxisState[AxisFlags.LeftStickX] < -Gamepad.LeftThumbDeadZone;
            Inputs.ButtonState[ButtonFlags.LeftStickRight] = Inputs.AxisState[AxisFlags.LeftStickX] > Gamepad.LeftThumbDeadZone;
            Inputs.ButtonState[ButtonFlags.LeftStickDown] = Inputs.AxisState[AxisFlags.LeftStickY] < -Gamepad.LeftThumbDeadZone;
            Inputs.ButtonState[ButtonFlags.LeftStickUp] = Inputs.AxisState[AxisFlags.LeftStickY] > Gamepad.LeftThumbDeadZone;

            float leftLength = new Vector2(Inputs.AxisState[AxisFlags.LeftStickX], Inputs.AxisState[AxisFlags.LeftStickY]).Length();
            Inputs.ButtonState[ButtonFlags.LeftStickOuterRing] = leftLength >= (RingThreshold * short.MaxValue);
            Inputs.ButtonState[ButtonFlags.LeftStickInnerRing] = leftLength >= Gamepad.LeftThumbDeadZone && leftLength < (RingThreshold * short.MaxValue);

            // Left Pad
            Inputs.ButtonState[ButtonFlags.LeftPadTouch] = input.State.ButtonState[GordonControllerButton.BtnLPadTouch];
            Inputs.ButtonState[ButtonFlags.LeftPadClick] = input.State.ButtonState[GordonControllerButton.BtnLPadPress];

            if (Inputs.ButtonState[ButtonFlags.LeftPadTouch])
            {
                Inputs.AxisState[AxisFlags.LeftPadX] = input.State.AxesState[GordonControllerAxis.LeftPadX];
                Inputs.AxisState[AxisFlags.LeftPadY] = input.State.AxesState[GordonControllerAxis.LeftPadY];
            }
            else
            {
                Inputs.AxisState[AxisFlags.LeftPadX] = 0;
                Inputs.AxisState[AxisFlags.LeftPadY] = 0;
            }

            if (Inputs.ButtonState[ButtonFlags.LeftPadClick])
            {
                if (Inputs.AxisState[AxisFlags.LeftPadY] >= TrackPadInner)
                    Inputs.ButtonState[ButtonFlags.LeftPadClickUp] = true;
                else if (Inputs.AxisState[AxisFlags.LeftPadY] <= -TrackPadInner)
                    Inputs.ButtonState[ButtonFlags.LeftPadClickDown] = true;

                if (Inputs.AxisState[AxisFlags.LeftPadX] >= TrackPadInner)
                    Inputs.ButtonState[ButtonFlags.LeftPadClickRight] = true;
                else if (Inputs.AxisState[AxisFlags.LeftPadX] <= -TrackPadInner)
                    Inputs.ButtonState[ButtonFlags.LeftPadClickLeft] = true;
            }

            // Right Pad
            Inputs.ButtonState[ButtonFlags.RightPadTouch] = input.State.ButtonState[GordonControllerButton.BtnRPadTouch];
            Inputs.ButtonState[ButtonFlags.RightPadClick] = input.State.ButtonState[GordonControllerButton.BtnRPadPress];

            if (Inputs.ButtonState[ButtonFlags.RightPadTouch])
            {
                Inputs.AxisState[AxisFlags.RightPadX] = input.State.AxesState[GordonControllerAxis.RightPadX];
                Inputs.AxisState[AxisFlags.RightPadY] = input.State.AxesState[GordonControllerAxis.RightPadY];
            }
            else
            {
                Inputs.AxisState[AxisFlags.RightPadX] = 0;
                Inputs.AxisState[AxisFlags.RightPadY] = 0;
            }

            if (Inputs.ButtonState[ButtonFlags.RightPadClick])
            {
                if (Inputs.AxisState[AxisFlags.RightPadY] >= TrackPadInner)
                    Inputs.ButtonState[ButtonFlags.RightPadClickUp] = true;
                else if (Inputs.AxisState[AxisFlags.RightPadY] <= -TrackPadInner)
                    Inputs.ButtonState[ButtonFlags.RightPadClickDown] = true;

                if (Inputs.AxisState[AxisFlags.RightPadX] >= TrackPadInner)
                    Inputs.ButtonState[ButtonFlags.RightPadClickRight] = true;
                else if (Inputs.AxisState[AxisFlags.RightPadX] <= -TrackPadInner)
                    Inputs.ButtonState[ButtonFlags.RightPadClickLeft] = true;
            }

            // TODO: why Z/Y swapped?
            Inputs.GyroState.Accelerometer.X = -(float)input.State.AxesState[GordonControllerAxis.GyroAccelX] / short.MaxValue * 2.0f;
            Inputs.GyroState.Accelerometer.Y = -(float)input.State.AxesState[GordonControllerAxis.GyroAccelZ] / short.MaxValue * 2.0f;
            Inputs.GyroState.Accelerometer.Z = -(float)input.State.AxesState[GordonControllerAxis.GyroAccelY] / short.MaxValue * 2.0f;

            // TODO: why Roll/Pitch swapped?
            Inputs.GyroState.Gyroscope.X =  (float)input.State.AxesState[GordonControllerAxis.GyroPitch] / short.MaxValue * 2000.0f;  // Roll
            Inputs.GyroState.Gyroscope.Y = -(float)input.State.AxesState[GordonControllerAxis.GyroRoll] / short.MaxValue * 2000.0f;   // Pitch
            Inputs.GyroState.Gyroscope.Z =  (float)input.State.AxesState[GordonControllerAxis.GyroYaw] / short.MaxValue * 2000.0f;    // Yaw

            base.UpdateInputs(ticks);
        }

        public override bool IsConnected()
        {
            return isConnected;
        }

        public virtual bool IsVirtualMuted()
        {
            return isVirtualMuted;
        }

        public override void Plug()
        {
            try
            {
                Controller.Open();
                Controller.OnControllerInputReceived = input => Task.Run(() => OnControllerInputReceived(input));
            }
            catch (Exception ex)
            {
                LogManager.LogError("Couldn't initialize GordonController. Exception: {0}", ex.Message);
                return;
            }

            // disable lizard state
            SetLizardMode(false);
            SetGyroscope(true);

            // manage rumble thread
            //rumbleThreadRunning = true;
            //rumbleThread = new Thread(RumbleThreadLoop);
            //rumbleThread.IsBackground = true;
            //rumbleThread.Start();

            TimerManager.Tick += UpdateInputs;
            base.Plug();
        }

        private void OnControllerInputReceived(GordonControllerInputEventArgs input)
        {
            this.input = input;
        }

        public override void Unplug()
        {
            TimerManager.Tick -= UpdateInputs;

            // restore lizard state
            SetLizardMode(true);
            SetGyroscope(false);

            // kill rumble thread
            //rumbleThreadRunning = false;
            //rumbleThread.Join();

            Controller.Close();

            base.Unplug();
        }

        public bool GetHapticIntensity(byte? input, sbyte minIntensity, sbyte maxIntensity, out sbyte output)
        {
            output = default;
            if (input is null || input == 0)
                return false;

            double value = minIntensity + (maxIntensity - minIntensity) * input.Value * VibrationStrength / 255;
            output = (sbyte)(value - 5); // convert from dB to values
            return true;
        }

        public override void SetVibration(byte LargeMotor, byte SmallMotor)
        {
            this.FeedbackLargeMotor = LargeMotor;
            this.FeedbackSmallMotor = SmallMotor;

            SetHaptic();
        }

        public void SetHaptic()
        {
            GetHapticIntensity(FeedbackLargeMotor, MinIntensity, MaxIntensity, out var leftIntensity);
            Controller.SetHaptic((byte)HapticPad.Left, (ushort)leftIntensity, RumblePeriod, 1);

            GetHapticIntensity(FeedbackSmallMotor, MinIntensity, MaxIntensity, out var rightIntensity);
            Controller.SetHaptic((byte)HapticPad.Right, (ushort)rightIntensity, RumblePeriod, 1);
        }

        public void SetLizardMode(bool lizardMode)
        {
            Controller.SetLizardMode(lizardMode);
        }

        public void SetGyroscope(bool gyroMode)
        {
            Controller.SetGyroscope(gyroMode);
        }

        public void SetVirtualMuted(bool mute)
        {
            isVirtualMuted = mute;
        }

        public override string GetGlyph(ButtonFlags button)
        {
            switch (button)
            {
                case ButtonFlags.B1:
                    return "\u21D3"; // Button A
                case ButtonFlags.B2:
                    return "\u21D2"; // Button B
                case ButtonFlags.B3:
                    return "\u21D0"; // Button X
                case ButtonFlags.B4:
                    return "\u21D1"; // Button Y
                case ButtonFlags.L1:
                    return "\u21B0";
                case ButtonFlags.R1:
                    return "\u21B1";
                case ButtonFlags.Back:
                    return "\u21FA";
                case ButtonFlags.Start:
                    return "\u21FB";
                case ButtonFlags.L2Soft:
                case ButtonFlags.L2Full:
                    return "\u21B2";
                case ButtonFlags.R2Soft:
                case ButtonFlags.R2Full:
                    return "\u21B3";
                case ButtonFlags.L4:
                    return "\u219c\u24f8";
                case ButtonFlags.L5:
                    return "\u219c\u24f9";
                case ButtonFlags.R4:
                    return "\u219d\u24f8";
                case ButtonFlags.R5:
                    return "\u219d\u24f9";
                case ButtonFlags.Special:
                    return "\u21E4";
                case ButtonFlags.OEM1:
                    return "\u21E5";
                case ButtonFlags.LeftStickTouch:
                    return "\u21DA";
                case ButtonFlags.RightStickTouch:
                    return "\u21DB";
                case ButtonFlags.LeftPadTouch:
                    return "\u2268";
                case ButtonFlags.RightPadTouch:
                    return "\u2269";
                case ButtonFlags.LeftPadClick:
                    return "\u2266";
                case ButtonFlags.RightPadClick:
                    return "\u2267";
                case ButtonFlags.LeftPadClickUp:
                    return "\u2270";
                case ButtonFlags.LeftPadClickDown:
                    return "\u2274";
                case ButtonFlags.LeftPadClickLeft:
                    return "\u226E";
                case ButtonFlags.LeftPadClickRight:
                    return "\u2272";
                case ButtonFlags.RightPadClickUp:
                    return "\u2271";
                case ButtonFlags.RightPadClickDown:
                    return "\u2275";
                case ButtonFlags.RightPadClickLeft:
                    return "\u226F";
                case ButtonFlags.RightPadClickRight:
                    return "\u2273";
            }

            return base.GetGlyph(button);
        }

        public override string GetGlyph(AxisFlags axis)
        {
            switch (axis)
            {
                case AxisFlags.L2:
                    return "\u2196";
                case AxisFlags.R2:
                    return "\u2197";
            }

            return base.GetGlyph(axis);
        }

        public override string GetGlyph(AxisLayoutFlags axis)
        {
            switch (axis)
            {
                case AxisLayoutFlags.L2:
                    return "\u2196";
                case AxisLayoutFlags.R2:
                    return "\u2197";
                case AxisLayoutFlags.LeftPad:
                    return "\u2264";
                case AxisLayoutFlags.RightPad:
                    return "\u2265";
                case AxisLayoutFlags.Gyroscope:
                    return "\u2B94";
            }

            return base.GetGlyph(axis);
        }
    }
}
