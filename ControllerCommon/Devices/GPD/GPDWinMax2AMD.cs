using System.Numerics;

namespace ControllerCommon.Devices
{
    public class GPDWinMax2AMD : GPDWinMax2
    {
        public GPDWinMax2AMD() : base()
        {
            // https://www.amd.com/fr/products/apu/amd-ryzen-7-6800u
            this.TDP = new uint[] { 15, 20, 28 };
            this.GPU = new uint[] { 100, 2200 };

            this.AngularVelocityAxis = new Vector3(1.0f, 1.0f, -1.0f);
            this.AngularVelocityAxisSwap = new()
            {
                { 'X', 'Y' },
                { 'Y', 'Z' },
                { 'Z', 'X' },
            };

            this.AccelerationAxis = new Vector3(1.0f, -1.0f, 1.0f);
            this.AccelerationAxisSwap = new()
            {
                { 'X', 'X' },
                { 'Y', 'Z' },
                { 'Z', 'Y' },
            };
        }
    }
}
