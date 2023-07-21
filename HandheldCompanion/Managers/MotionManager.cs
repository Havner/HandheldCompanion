﻿using HandheldCompanion.Actions;
using HandheldCompanion.Controllers;
using HandheldCompanion.Inputs;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
using HandheldCompanion.Views;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Windows;

namespace HandheldCompanion.Managers
{
    public static class MotionManager
    {
        [Flags]
        private enum SensorIndex
        {
            Raw = 0,
            Default = 1,
        }

        public static readonly SensorSpec accelerometerSpec = new()
        {
            minIn = -2.0f,
            maxIn = 2.0f,
            minOut = short.MinValue,
            maxOut = short.MaxValue,
        };

        public static readonly SensorSpec gyroscopeSpec = new()
        {
            minIn = -128.0f,
            maxIn = 128.0f,
            minOut = -2048.0f,
            maxOut = 2048.0f,
        };

        private static Vector3[] accelerometer = new Vector3[2];
        private static Vector3[] gyroscope = new Vector3[2];

        private static SensorFusion sensorFusion = new();
        private static MadgwickAHRS madgwickAHRS = new(0.01f, 0.1f);
        private static Inclination inclination = new();

        private static double PreviousTotalMilliseconds;

        public static event SettingsMode0EventHandler SettingsMode0Update;
        public delegate void SettingsMode0EventHandler(Vector3 gyrometer);

        public static event SettingsMode1EventHandler SettingsMode1Update;
        public delegate void SettingsMode1EventHandler(Vector2 deviceAngle);

        public static event OverlayModelEventHandler OverlayModelUpdate;
        public delegate void OverlayModelEventHandler(Vector3 euler, Quaternion quaternion);

        public static bool IsInitialized;

        public static event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();

        static MotionManager()
        {
        }

        public static void Start()
        {
            IsInitialized = true;
            Initialized?.Invoke();
        }

        public static void Stop()
        {
            IsInitialized = false;
        }

        public static void UpdateReport(ControllerState controllerState)
        {
            if (!MainWindow.CurrentDevice.HasMotionSensor())
                return;

            SetupMotion(controllerState);
            CalculateMotion(controllerState);
        }

        // this function sets some basic motion settings, sensitivity and inverts
        // and is enough for DS4/DSU gyroscope handling
        private static void SetupMotion(ControllerState controllerState)
        {
            // store raw values for later use
            accelerometer[(int)SensorIndex.Raw] = controllerState.GyroState.Accelerometer;
            gyroscope[(int)SensorIndex.Raw] = controllerState.GyroState.Gyroscope;

            Profile current = ProfileManager.GetCurrent();

            accelerometer[(int)SensorIndex.Default].Z = current.SteeringAxis == 0 ? controllerState.GyroState.Accelerometer.Z : controllerState.GyroState.Accelerometer.Y;
            accelerometer[(int)SensorIndex.Default].Y = current.SteeringAxis == 0 ? controllerState.GyroState.Accelerometer.Y : -controllerState.GyroState.Accelerometer.Z;
            accelerometer[(int)SensorIndex.Default].X = current.SteeringAxis == 0 ? controllerState.GyroState.Accelerometer.X : controllerState.GyroState.Accelerometer.X;

            gyroscope[(int)SensorIndex.Default].Z = current.SteeringAxis == 0 ? controllerState.GyroState.Gyroscope.Z : controllerState.GyroState.Gyroscope.Y;
            gyroscope[(int)SensorIndex.Default].Y = current.SteeringAxis == 0 ? controllerState.GyroState.Gyroscope.Y : controllerState.GyroState.Gyroscope.Z;
            gyroscope[(int)SensorIndex.Default].X = current.SteeringAxis == 0 ? controllerState.GyroState.Gyroscope.X : controllerState.GyroState.Gyroscope.X;

            if (current.MotionInvertHorizontal)
            {
                accelerometer[(int)SensorIndex.Default].Y *= -1.0f;
                accelerometer[(int)SensorIndex.Default].Z *= -1.0f;
                gyroscope[(int)SensorIndex.Default].Y *= -1.0f;
                gyroscope[(int)SensorIndex.Default].Z *= -1.0f;
            }

            if (current.MotionInvertVertical)
            {
                accelerometer[(int)SensorIndex.Default].Y *= -1.0f;
                accelerometer[(int)SensorIndex.Default].X *= -1.0f;
                gyroscope[(int)SensorIndex.Default].Y *= -1.0f;
                gyroscope[(int)SensorIndex.Default].X *= -1.0f;
            }

            // store modified values, they are used by DS4 and DSU, raws are only used later on in MotionManager
            controllerState.GyroState.Accelerometer = accelerometer[(int)SensorIndex.Default];
            controllerState.GyroState.Gyroscope = gyroscope[(int)SensorIndex.Default];
        }

        // this function is used for advanced motion calculations used by
        // gyro to joy/mouse mappings, by UI that configures them and by 3D overlay
        private static void CalculateMotion(ControllerState controllerState)
        {
            Profile current = ProfileManager.GetCurrent();

            // update timestamp
            double TotalMilliseconds = TimerManager.Stopwatch.Elapsed.TotalMilliseconds;
            double DeltaSeconds = (TotalMilliseconds - PreviousTotalMilliseconds) / 1000L;
            PreviousTotalMilliseconds = TotalMilliseconds;

            // check if motion trigger is pressed
            bool MotionTriggered =
                (current.MotionMode == MotionMode.Off && controllerState.ButtonState.ContainsTrue(current.MotionTrigger)) ||
                (current.MotionMode == MotionMode.On && !controllerState.ButtonState.ContainsTrue(current.MotionTrigger));

            bool MotionMapped = false;
            if (current.Layout.AxisLayout.TryGetValue(AxisLayoutFlags.Gyroscope, out IActions action))
                if (action.ActionType != ActionType.Disabled)
                    MotionMapped = true;

            // update sensorFusion, only when needed
            if (MotionMapped && MotionTriggered && (current.MotionInput == MotionInput.PlayerSpace ||
                                                    current.MotionInput == MotionInput.AutoRollYawSwap))
            {
                sensorFusion.UpdateReport(DeltaSeconds, gyroscope[(int)SensorIndex.Default], accelerometer[(int)SensorIndex.Default]);
            }

            if ((MotionMapped && MotionTriggered && current.MotionInput == MotionInput.JoystickSteering)
                || MainWindow.CurrentPageName == "SettingsMode1")
            {
                inclination.UpdateReport(accelerometer[(int)SensorIndex.Default]);
            }

            switch (MainWindow.CurrentPageName)
            {
                case "SettingsMode0":
                    SettingsMode0Update?.Invoke(gyroscope[(int)SensorIndex.Default]);
                    break;
                case "SettingsMode1":
                    SettingsMode1Update?.Invoke(inclination.Angles);
                    break;
            }

            if (MainWindow.overlayModel.Visibility == Visibility.Visible && MainWindow.overlayModel.MotionActivated)
            {
                Vector3 AngularVelocityRad = new(
                    -InputUtils.deg2rad(gyroscope[(int)SensorIndex.Raw].X),
                    -InputUtils.deg2rad(gyroscope[(int)SensorIndex.Raw].Y),
                    -InputUtils.deg2rad(gyroscope[(int)SensorIndex.Raw].Z));
                madgwickAHRS.UpdateReport(
                    AngularVelocityRad.X,
                    AngularVelocityRad.Y,
                    AngularVelocityRad.Z,
                    -accelerometer[(int)SensorIndex.Raw].X,
                    accelerometer[(int)SensorIndex.Raw].Y,
                    accelerometer[(int)SensorIndex.Raw].Z,
                    DeltaSeconds);

                OverlayModelUpdate?.Invoke(madgwickAHRS.GetEuler(), madgwickAHRS.GetQuaternion());
            }

            // after this point the code only makes sense if we're actively using mapped gyro
            // if we are not, nullify the last state to remove drift
            if (!MotionTriggered || !MotionMapped)
            {
                controllerState.AxisState[AxisFlags.GyroX] = 0;
                controllerState.AxisState[AxisFlags.GyroY] = 0;
                return;
            }

            Vector2 output;

            switch (current.MotionInput)
            {
                case MotionInput.PlayerSpace:
                case MotionInput.AutoRollYawSwap:
                case MotionInput.JoystickCamera:
                default:
                    switch (current.MotionInput)
                    {
                        case MotionInput.PlayerSpace:
                            output = new Vector2((float)sensorFusion.CameraYawDelta, (float)sensorFusion.CameraPitchDelta);
                            break;
                        case MotionInput.AutoRollYawSwap:
                            output = InputUtils.AutoRollYawSwap(sensorFusion.GravityVectorSimple, gyroscope[(int)SensorIndex.Default]);
                            break;
                        case MotionInput.JoystickCamera:
                        default:
                            output = new Vector2(gyroscope[(int)SensorIndex.Default].Z, gyroscope[(int)SensorIndex.Default].X);
                            break;
                    }

                    // apply sensivity curve
                    if (current.MotionSensivityArrayEnabled)
                    {
                        output.X *= InputUtils.ApplyCustomSensitivity(output.X, gyroscopeSpec.maxIn, current.MotionSensivityArray);
                        output.Y *= InputUtils.ApplyCustomSensitivity(output.Y, gyroscopeSpec.maxIn, current.MotionSensivityArray);
                    }

                    // apply aiming down scopes multiplier if activated
                    if (controllerState.ButtonState.Contains(current.AimingSightsTrigger))
                        output *= current.AimingSightsMultiplier;

                    // apply sensivity
                    output = new Vector2(output.X * current.GetSensitivityX(), output.Y * current.GetSensitivityY());

                    break;

                // TODO: merge this somehow with joy Y as it was previously?
                case MotionInput.JoystickSteering:
                    {
                        output.X = InputUtils.Steering(inclination.Angles.Y, current.SteeringMaxAngle, current.SteeringPower, current.SteeringDeadzone);
                        output.Y = 0.0f;
                    }
                    break;
            }

            // fill the final calculated state for further use in the remapper
            controllerState.AxisState[AxisFlags.GyroX] = (short)Math.Clamp(output.X, short.MinValue, short.MaxValue);
            controllerState.AxisState[AxisFlags.GyroY] = (short)Math.Clamp(output.Y, short.MinValue, short.MaxValue);
        }
    }
}
