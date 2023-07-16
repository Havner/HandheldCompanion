using System;

namespace ControllerCommon.Inputs
{
    [Serializable]
    public class GyroState : ICloneable
    {
        // TODO: switch to vectors when service is gone
        //public Vector3 Accelerometer = new();
        //public Vector3 Gyroscope = new();

        public float AccelerometerX;
        public float AccelerometerY;
        public float AccelerometerZ;
        public float GyroscopeX;
        public float GyroscopeY;
        public float GyroscopeZ;

        public GyroState()
        {
        }

        public GyroState(GyroState state)
        {
            AccelerometerX = state.AccelerometerX;
            AccelerometerY = state.AccelerometerY;
            AccelerometerZ = state.AccelerometerZ;
            GyroscopeX = state.GyroscopeX;
            GyroscopeY = state.GyroscopeY;
            GyroscopeZ = state.GyroscopeZ;
        }

        public object Clone()
        {
            return new GyroState(this);
        }
    }
}
