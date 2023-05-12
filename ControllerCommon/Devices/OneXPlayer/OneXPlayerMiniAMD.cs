using System.Numerics;

namespace ControllerCommon.Devices
{
    public class OneXPlayerMiniAMD : OneXPlayerMini
    {
        public OneXPlayerMiniAMD() : base()
        {
            // https://www.amd.com/fr/products/apu/amd-ryzen-7-5800u
            this.TDP = new uint[] { 10, 20, 25 };
            this.GPU = new uint[] { 100, 2000 };

            this.AccelerationAxis = new Vector3(-1.0f, -1.0f, 1.0f);
        }
    }
}
