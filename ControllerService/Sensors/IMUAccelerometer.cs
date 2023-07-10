using ControllerCommon.Utils;
using System.Numerics;

namespace ControllerService.Sensors
{
    public class IMUAccelerometer : IMUSensor
    {
        public static new SensorSpec sensorSpec = new()
        {
            minIn = -2.0f,
            maxIn = 2.0f,
            minOut = short.MinValue,
            maxOut = short.MaxValue,
        };

        public IMUAccelerometer() : base()
        {
        }

        public void ReadingChanged(float GyroAccelX, float GyroAccelY, float GyroAccelZ)
        {
            this.reading.X = this.reading_fixed.X = GyroAccelX;
            this.reading.Y = this.reading_fixed.Y = GyroAccelY;
            this.reading.Z = this.reading_fixed.Z = GyroAccelZ;

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

            reading *= ControllerService.currentProfile.AccelerometerMultiplier;

            var readingZ = ControllerService.currentProfile.SteeringAxis == 0 ? reading.Z : reading.Y;
            var readingY = ControllerService.currentProfile.SteeringAxis == 0 ? reading.Y : -reading.Z;
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