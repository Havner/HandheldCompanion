using System.Numerics;

namespace ControllerCommon.Devices
{
    public class OneXPlayerMiniIntel : OneXPlayerMini
    {
        public OneXPlayerMiniIntel() : base()
        {
            // https://ark.intel.com/content/www/us/en/ark/products/226254/intel-core-i71260p-processor-18m-cache-up-to-4-70-ghz.html
            this.TDP = new uint[] { 20, 64, 64 };
            this.GPU = new uint[] { 100, 1400 };

            this.AngularVelocityAxis = new Vector3(1.0f, 1.0f, -1.0f);
            this.AngularVelocityAxisSwap = new()
            {
                { 'X', 'Y' },
                { 'Y', 'Z' },
                { 'Z', 'X' },
            };

            this.AccelerationAxis = new Vector3(-1.0f, 1.0f, -1.0f);
        }
    }
}
