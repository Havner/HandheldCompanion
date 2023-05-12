namespace ControllerCommon.Devices
{
    public class AYANEOAIRPro : AYANEOAIR
    {
        public AYANEOAIRPro() : base()
        {
            // https://www.amd.com/en/products/apu/amd-ryzen-7-5825u
            this.TDP = new uint[] { 3, 15, 18 };
            this.GPU = new uint[] { 100, 2000 };
        }
    }
}
