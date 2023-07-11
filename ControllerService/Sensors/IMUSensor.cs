using ControllerCommon.Utils;
using System;
using System.Numerics;

namespace ControllerService.Sensors
{
    [Flags]
    public enum XInputSensorFlags
    {
        Default = 0,
        RawValue = 1,
    }

    public abstract class IMUSensor : IDisposable
    {
        protected Vector3 reading = new();

        protected static SensorSpec sensorSpec;

        protected IMUSensor()
        {
        }

        protected virtual Vector3 GetCurrentReading(XInputSensorFlags flags)
        {
            switch (flags)
            {
                case XInputSensorFlags.RawValue:
                    return this.reading;
                case XInputSensorFlags.Default:
                default:
                    return this.reading;
            }
        }

        public virtual void Dispose()
        {
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}
