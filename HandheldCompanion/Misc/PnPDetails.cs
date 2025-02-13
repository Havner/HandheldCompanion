using HandheldCompanion.Managers.Hid;
using System;
using System.Runtime.InteropServices;

namespace HandheldCompanion.Misc
{
    [StructLayout(LayoutKind.Sequential)]
    public class PnPDetails
    {
        public string Name;
        public string Path;
        public string SymLink;

        public bool isVirtual;
        public bool isGaming;
        public bool isHooked;
        public bool isXInput;

        public DateTimeOffset arrivalDate;

        public string deviceInstanceId;
        public string baseContainerDeviceInstanceId;

        public Attributes attributes;
        public Capabilities capabilities;

        public string GetProductID()
        {
            return "0x" + attributes.ProductID.ToString("X4");
        }

        public string GetVendorID()
        {
            return "0x" + attributes.VendorID.ToString("X4");
        }

        public short GetMI()
        {
            string low = SymLink.ToLower();
            int index = low.IndexOf("mi_");
            if (index == -1)
                return -1;
            string mi = low.Substring(index + 3, 2);

            if (short.TryParse(mi, out short number))
                return number;

            return -1;
        }
    }
}
