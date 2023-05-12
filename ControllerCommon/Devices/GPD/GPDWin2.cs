namespace ControllerCommon.Devices
{
    public class GPDWin2 : IDevice
    {
        public GPDWin2() : base()
        {
            // device specific settings
            this.ProductIllustration = "device_gpd_win2";

            // https://www.intel.com/content/www/us/en/products/sku/185282/intel-core-m38100y-processor-4m-cache-up-to-3-40-ghz/specifications.html
            this.TDP = new uint[] { 5, 15, 15 };
            this.GPU = new uint[] { 300, 900 };
        }
    }
}
