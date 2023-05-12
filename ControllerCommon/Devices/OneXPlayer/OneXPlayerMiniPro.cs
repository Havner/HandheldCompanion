using System.Numerics;

namespace ControllerCommon.Devices
{
    public class OneXPlayerMiniPro : OneXPlayerMini
    {
        public OneXPlayerMiniPro() : base()
        {
            // https://www.amd.com/en/products/apu/amd-ryzen-7-6800u
            this.TDP = new uint[] { 4, 20, 28 };
            this.GPU = new uint[] { 100, 2200 };

            this.AccelerationAxis = new Vector3(-1.0f, 1.0f, 1.0f);
        }
    }
}
