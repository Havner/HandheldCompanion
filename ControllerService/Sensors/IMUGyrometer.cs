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
            this.reading.X = this.reading_fixed.X = GyroRoll;
            this.reading.Y = this.reading_fixed.Y = GyroPitch;
            this.reading.Z = this.reading_fixed.Z = GyroYaw;

            base.ReadingChanged();
        }

        public new Vector3 GetCurrentReading(bool center = false)
        {
            Vector3 reading = new Vector3()
            {
                X = center ? this.reading_fixed.X : this.reading.X,
                Y = center ? this.reading_fixed.Y : this.reading.Y,
                Z = center ? this.reading_fixed.Z : this.reading.Z
            };

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
