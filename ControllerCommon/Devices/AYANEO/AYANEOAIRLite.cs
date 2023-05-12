namespace ControllerCommon.Devices
{
    public class AYANEOAIRLite : AYANEOAIR
    {
        public AYANEOAIRLite() : base()
        {
            // https://www.amd.com/en/products/apu/amd-ryzen-5-5560u
            this.TDP = new uint[] { 3, 12, 12 };
            this.GPU = new uint[] { 100, 1600 };
        }
    }
}
