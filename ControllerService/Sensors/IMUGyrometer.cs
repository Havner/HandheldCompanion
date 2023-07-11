using ControllerCommon.Utils;
using System.Numerics;

namespace ControllerService.Sensors
{
    public class IMUGyrometer : IMUSensor
    {
        public static new SensorSpec sensorSpec = new()
        {
            minIn = -128.0f,
            maxIn = 128.0f,
            minOut = -2048.0f,
            maxOut = 2048.0f,
        };

        public IMUGyrometer() : base()
        {
        }

        public void ReadingChanged(float GyroRoll, float GyroPitch, float GyroYaw)
        {
            this.reading.X = GyroRoll;
            this.reading.Y = GyroPitch;
            this.reading.Z = GyroYaw;
        }

        public new Vector3 GetCurrentReading(XInputSensorFlags flags)
        {
            Vector3 reading;

            switch (flags)
            {
                case XInputSensorFlags.RawValue:
                    return this.reading;
                default:
                case XInputSensorFlags.Default:
                    reading = this.reading;
                    break;
            }

            reading *= ControllerService.currentProfile.GyrometerMultiplier;

            var readingZ = ControllerService.currentProfile.SteeringAxis == 0 ? reading.Z : reading.Y;
            var readingY = ControllerService.currentProfile.SteeringAxis == 0 ? reading.Y : reading.Z;
            var readingX = ControllerService.currentProfile.SteeringAxis == 0 ? reading.X : reading.X;

            if (ControllerService.currentProfile.MotionInvertHorizontal)
            {
                readingY *= -1.0f;
                readingZ *= -1.0f;
            }

            if (ControllerService.currentProfile.MotionInvertVertical)
            {
                readingY *= -1.0f;
                readingX *= -1.0f;
            }

            reading.X = readingX;
            reading.Y = readingY;
            reading.Z = readingZ;

            return reading;
        }
    }
}
