using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using steam_hidapi.net;
using steam_hidapi.net.Hid;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HandheldCompanion.Controllers
{
    public class GordonController : SteamController
    {
        private steam_hidapi.net.GordonController Controller;
        private GordonControllerInputEventArgs input;

        private const short TrackPadInner = 12000;

        public byte FeedbackLargeMotor;
        public byte FeedbackSmallMotor;

        public const ushort MaxIntensity = 2048;

        public GordonController(PnPDetails details) : base()
        {
            if (details is null)
                return;

            Controller = new(details.attributes.VendorID, details.attributes.ProductID, details.GetMI());
            isConnected = true;

            Details = details;
            Details.isHooked = true;

            // UI
            ColoredButtons.Add(ButtonFlags.B1, new SolidColorBrush(Color.FromArgb(255, 81, 191, 61)));
            ColoredButtons.Add(ButtonFlags.B2, new SolidColorBrush(Color.FromArgb(255, 217, 65, 38)));
            ColoredButtons.Add(ButtonFlags.B3, new SolidColorBrush(Color.FromArgb(255, 26, 159, 255)));
            ColoredButtons.Add(ButtonFlags.B4, new SolidColorBrush(Color.FromArgb(255, 255, 200, 44)));

            // UI
            DrawControls();
            RefreshControls();

            // Additional controller specific source buttons/axes
            SourceButtons.AddRange(new List<ButtonFlags>() { ButtonFlags.L4, ButtonFlags.R4 });
            SourceButtons.AddRange(new List<ButtonFlags>() { ButtonFlags.LeftPadClick, ButtonFlags.LeftPadTouch, ButtonFlags.LeftPadClickUp, ButtonFlags.LeftPadClickDown, ButtonFlags.LeftPadClickLeft, ButtonFlags.LeftPadClickRight });
            SourceButtons.AddRange(new List<ButtonFlags>() { ButtonFlags.RightPadClick, ButtonFlags.RightPadTouch, ButtonFlags.RightPadClickUp, ButtonFlags.RightPadClickDown, ButtonFlags.RightPadClickLeft, ButtonFlags.RightPadClickRight });

            SourceAxis.Add(AxisLayoutFlags.LeftPad);
            SourceAxis.Add(AxisLayoutFlags.RightPad);
            SourceAxis.Add(AxisLayoutFlags.Gyroscope);

            // This is a very original controller, it doesn't have few things
            SourceButtons.Remove(ButtonFlags.DPadUp);
            SourceButtons.Remove(ButtonFlags.DPadDown);
            SourceButtons.Remove(ButtonFlags.DPadLeft);
            SourceButtons.Remove(ButtonFlags.DPadRight);
            SourceButtons.Remove(ButtonFlags.RightStickClick);
            SourceButtons.Remove(ButtonFlags.RightStickUp);
            SourceButtons.Remove(ButtonFlags.RightStickDown);
            SourceButtons.Remove(ButtonFlags.RightStickLeft);
            SourceButtons.Remove(ButtonFlags.RightStickRight);
            SourceButtons.Remove(ButtonFlags.RightStickOuterRing);
            SourceButtons.Remove(ButtonFlags.RightStickInnerRing);

            SourceAxis.Remove(AxisLayoutFlags.RightStick);
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

            Inputs.ButtonState[ButtonFlags.Start] = input.State.ButtonState[GordonControllerButton.BtnOptions];
            Inputs.ButtonState[ButtonFlags.Back] = input.State.ButtonState[GordonControllerButton.BtnMenu];
            Inputs.ButtonState[ButtonFlags.Special] = input.State.ButtonState[GordonControllerButton.BtnSteam];

            var L2 = input.State.AxesState[GordonControllerAxis.L2];
            var R2 = input.State.AxesState[GordonControllerAxis.R2];

            Inputs.ButtonState[ButtonFlags.L2Soft] = L2 > Gamepad.TriggerThreshold;
            Inputs.ButtonState[ButtonFlags.R2Soft] = R2 > Gamepad.TriggerThreshold;

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
                Inputs.ButtonState[ButtonFlags.LeftPadClickUp] = Inputs.AxisState[AxisFlags.LeftPadY] >= TrackPadInner;
                Inputs.ButtonState[ButtonFlags.LeftPadClickDown] = Inputs.AxisState[AxisFlags.LeftPadY] <= -TrackPadInner;
                Inputs.ButtonState[ButtonFlags.LeftPadClickRight] = Inputs.AxisState[AxisFlags.LeftPadX] >= TrackPadInner;
                Inputs.ButtonState[ButtonFlags.LeftPadClickLeft] = Inputs.AxisState[AxisFlags.LeftPadX] <= -TrackPadInner;
            }
            else
            {
                Inputs.ButtonState[ButtonFlags.LeftPadClickUp] = false;
                Inputs.ButtonState[ButtonFlags.LeftPadClickDown] = false;
                Inputs.ButtonState[ButtonFlags.LeftPadClickRight] = false;
                Inputs.ButtonState[ButtonFlags.LeftPadClickLeft] = false;
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
                Inputs.ButtonState[ButtonFlags.RightPadClickUp] = Inputs.AxisState[AxisFlags.RightPadY] >= TrackPadInner;
                Inputs.ButtonState[ButtonFlags.RightPadClickDown] = Inputs.AxisState[AxisFlags.RightPadY] <= -TrackPadInner;
                Inputs.ButtonState[ButtonFlags.RightPadClickRight] = Inputs.AxisState[AxisFlags.RightPadX] >= TrackPadInner;
                Inputs.ButtonState[ButtonFlags.RightPadClickLeft] = Inputs.AxisState[AxisFlags.RightPadX] <= -TrackPadInner;
            }
            else
            {
                Inputs.ButtonState[ButtonFlags.RightPadClickUp] = false;
                Inputs.ButtonState[ButtonFlags.RightPadClickDown] = false;
                Inputs.ButtonState[ButtonFlags.RightPadClickRight] = false;
                Inputs.ButtonState[ButtonFlags.RightPadClickLeft] = false;
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

        private void OnControllerInputReceived(GordonControllerInputEventArgs input)
        {
            this.input = input;
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

            Controller.SetLizardMode(false);
            // TODO: using more battery?
            Controller.SetGyroscope(true);

            SetVirtualMuted(SettingsManager.GetBoolean("SteamMuteController"));

            TimerManager.Tick += UpdateInputs;
            base.Plug();
        }

        public override void Unplug()
        {
            TimerManager.Tick -= UpdateInputs;

            Controller.SetLizardMode(true);
            Controller.SetGyroscope(false);

            Controller.Close();

            base.Unplug();
        }

        public override void Cleanup()
        {
            TimerManager.Tick -= UpdateInputs;
        }

        public ushort GetHapticIntensity(byte input, ushort maxIntensity)
        {
            return (ushort)(input * maxIntensity * VibrationStrength / 255);
        }

        public override void SetVibration(byte LargeMotor, byte SmallMotor)
        {
            this.FeedbackLargeMotor = LargeMotor;
            this.FeedbackSmallMotor = SmallMotor;

            SetHaptic();
        }

        public void SetHaptic()
        {
            ushort leftAmplitude = GetHapticIntensity(FeedbackLargeMotor, MaxIntensity);
            Controller.SetHaptic((byte)SCHapticPad.Left, leftAmplitude, 0, 1);

            ushort rightAmplitude = GetHapticIntensity(FeedbackSmallMotor, MaxIntensity);
            Controller.SetHaptic((byte)SCHapticPad.Right, rightAmplitude, 0, 1);
        }
    }
}
