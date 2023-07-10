using ControllerCommon.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Timers;
using static ControllerCommon.Utils.CommonUtils;

namespace ControllerService.Sensors
{
    [Flags]
    public enum XInputSensorFlags
    {
        Default = 0,
        RawValue = 1,
        Centered = 2,
        CenteredRaw = RawValue | Centered,
    }

    public abstract class IMUSensor : IDisposable
    {
        protected Vector3 reading = new();
        protected Vector3 reading_fixed = new();

        protected static SensorSpec sensorSpec;

        protected Timer centerTimer;
        public OneEuroFilter3D filter = new();

        protected bool disposed;

        protected Dictionary<char, double> reading_axis = new Dictionary<char, double>()
        {
            { 'X', 0.0d },
            { 'Y', 0.0d },
            { 'Z', 0.0d },
        };

        protected IMUSensor()
        {
            this.centerTimer = new Timer(100);
            this.centerTimer.AutoReset = false;
            this.centerTimer.Elapsed += CenterTimer_Elapsed;
        }

        private void CenterTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.reading_fixed.X = this.reading_fixed.Y = this.reading_fixed.Z = 0;
        }

        protected virtual void ReadingChanged()
        {
            if (centerTimer is null)
                return;

            // reset reading after inactivity
            this.centerTimer.Stop();
            this.centerTimer.Start();
        }

        public virtual void Stop()
        {
            if (centerTimer is null)
                return;

            this.centerTimer.Stop();
            this.centerTimer.Dispose();
            this.centerTimer = null;
        }

        protected virtual Vector3 GetCurrentReading(bool center = false)
        {
            return center ? this.reading_fixed : this.reading;
        }

        public Vector3 GetCurrentReadingRaw(bool center = false)
        {
            return center ? this.reading_fixed : this.reading;
        }

        public virtual void Dispose()
        {
            Stop();
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}
