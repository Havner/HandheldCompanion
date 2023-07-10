using ControllerCommon;
using ControllerCommon.Controllers;
using ControllerCommon.Managers;
using ControllerCommon.Pipes;
using ControllerCommon.Utils;
using ControllerService.Sensors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace ControllerService
{
    public static class IMU
    {
        public static SortedDictionary<XInputSensorFlags, Vector3> Acceleration = new();
        public static SortedDictionary<XInputSensorFlags, Vector3> AngularVelocity = new();

        public static IMUGyrometer Gyrometer;
        public static IMUAccelerometer Accelerometer;

        public static SensorFusion sensorFusion;
        public static MadgwickAHRS madgwickAHRS;

        public static Stopwatch stopwatch;
        public static long CurrentMicroseconds;

        public static double TotalMilliseconds;
        public static double UpdateTimePreviousMilliseconds;
        public static double DeltaSeconds = 100.0d;

        public static event UpdatedEventHandler Updated;
        public delegate void UpdatedEventHandler();

        private static object updateLock = new();

        public static bool IsInitialized;

        public static event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();

        static IMU()
        {
            Gyrometer = new IMUGyrometer();
            Accelerometer = new IMUAccelerometer();

            // initialize sensorfusion and madgwick
            sensorFusion = new SensorFusion();
            madgwickAHRS = new MadgwickAHRS(0.01f, 0.1f);

            // initialize stopwatch
            stopwatch = new Stopwatch();
        }

        public static void Start()
        {
            stopwatch.Start();

            TimerManager.Tick += Tick;

            IsInitialized = true;
            Initialized?.Invoke();
        }

        public static void Stop()
        {
            TimerManager.Tick -= Tick;

            // halt sensors
            Gyrometer?.Stop();
            Accelerometer?.Stop();

            stopwatch.Stop();

            IsInitialized = false;
        }

        public static void UpdateMovements(ControllerMovements movements)
        {
            Gyrometer.ReadingChanged(movements.GyroRoll, movements.GyroPitch, movements.GyroYaw);
            Accelerometer.ReadingChanged(movements.GyroAccelX, movements.GyroAccelY, movements.GyroAccelZ);
        }

        private static void Tick(long ticks)
        {
            if (Monitor.TryEnter(updateLock))
            {
                // update timestamp
                CurrentMicroseconds = stopwatch.ElapsedMilliseconds * 1000L;
                TotalMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                DeltaSeconds = (TotalMilliseconds - UpdateTimePreviousMilliseconds) / 1000L;
                UpdateTimePreviousMilliseconds = TotalMilliseconds;

                // update reading(s)
                foreach (XInputSensorFlags flags in (XInputSensorFlags[])Enum.GetValues(typeof(XInputSensorFlags)))
                {
                    switch (flags)
                    {
                        case XInputSensorFlags.Default:
                            AngularVelocity[flags] = Gyrometer.GetCurrentReading();
                            Acceleration[flags] = Accelerometer.GetCurrentReading();
                            break;

                        case XInputSensorFlags.RawValue:
                            AngularVelocity[flags] = Gyrometer.GetCurrentReadingRaw();
                            Acceleration[flags] = Accelerometer.GetCurrentReadingRaw();
                            break;

                        case XInputSensorFlags.Centered:
                            AngularVelocity[flags] = Gyrometer.GetCurrentReading(true);
                            Acceleration[flags] = Accelerometer.GetCurrentReading(true);
                            break;

                        case XInputSensorFlags.CenteredRaw:
                            AngularVelocity[flags] = Gyrometer.GetCurrentReadingRaw(true);
                            Acceleration[flags] = Accelerometer.GetCurrentReadingRaw(true);
                            break;
                    }
                }

                // update sensorFusion
                if (ControllerService.currentProfile.MotionInput == MotionInput.PlayerSpace ||
                    ControllerService.currentProfile.MotionInput == MotionInput.AutoRollYawSwap ||
                    ControllerService.currentProfile.MotionInput == MotionInput.JoystickSteering ||
                    ControllerService.CurrentTag == "SettingsMode1")
                {
                    sensorFusion.UpdateReport(TotalMilliseconds, DeltaSeconds, AngularVelocity[XInputSensorFlags.Centered], Acceleration[XInputSensorFlags.Default]);
                }

                switch (ControllerService.CurrentTag)
                {
                    case "SettingsMode0":
                        PipeServer.SendMessage(new PipeSensor(AngularVelocity[XInputSensorFlags.Centered], SensorType.Girometer));
                        break;

                    case "SettingsMode1":
                        PipeServer.SendMessage(new PipeSensor(sensorFusion.DeviceAngle, SensorType.Inclinometer));
                        break;
                }

                switch (ControllerService.CurrentOverlayStatus)
                {
                    case 0: // Visible
                        var AngularVelocityRad = new Vector3();
                        AngularVelocityRad.X = -InputUtils.deg2rad(AngularVelocity[XInputSensorFlags.CenteredRaw].X);
                        AngularVelocityRad.Y = -InputUtils.deg2rad(AngularVelocity[XInputSensorFlags.CenteredRaw].Y);
                        AngularVelocityRad.Z = -InputUtils.deg2rad(AngularVelocity[XInputSensorFlags.CenteredRaw].Z);
                        madgwickAHRS.UpdateReport(AngularVelocityRad.X, AngularVelocityRad.Y, AngularVelocityRad.Z, -Acceleration[XInputSensorFlags.RawValue].X, Acceleration[XInputSensorFlags.RawValue].Y, Acceleration[XInputSensorFlags.RawValue].Z, DeltaSeconds);

                        PipeServer.SendMessage(new PipeSensor(madgwickAHRS.GetEuler(), madgwickAHRS.GetQuaternion(), SensorType.Quaternion));
                        break;
                }

                Updated?.Invoke();

                Monitor.Exit(updateLock);
            }
        }
    }
}