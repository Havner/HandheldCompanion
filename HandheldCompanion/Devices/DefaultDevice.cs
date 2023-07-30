using HandheldCompanion.Inputs;
using HandheldCompanion.Misc;
using System.Collections.Generic;
using WindowsInput.Events;

namespace HandheldCompanion.Devices
{
    public class DefaultDevice : IDevice
    {
        public DefaultDevice() : base()
        {
            OEMChords.Add(new DeviceChord("F1",
                new List<KeyCode>() { KeyCode.F1 },
                new List<KeyCode>() { KeyCode.F1 },
                false, ButtonFlags.OEM1
                ));

            OEMChords.Add(new DeviceChord("F2",
                new List<KeyCode>() { KeyCode.F2 },
                new List<KeyCode>() { KeyCode.F2 },
                false, ButtonFlags.OEM2
                ));

            OEMChords.Add(new DeviceChord("F3",
                new List<KeyCode>() { KeyCode.F3 },
                new List<KeyCode>() { KeyCode.F3 },
                false, ButtonFlags.OEM3
                ));

            OEMChords.Add(new DeviceChord("F4",
                new List<KeyCode>() { KeyCode.F4 },
                new List<KeyCode>() { KeyCode.F4 },
                false, ButtonFlags.OEM4
                ));

            OEMChords.Add(new DeviceChord("F5",
                new List<KeyCode>() { KeyCode.F5 },
                new List<KeyCode>() { KeyCode.F5 },
                false, ButtonFlags.OEM5
                ));

            OEMChords.Add(new DeviceChord("F6",
                new List<KeyCode>() { KeyCode.F6 },
                new List<KeyCode>() { KeyCode.F6 },
                false, ButtonFlags.OEM6
                ));
        }
    }
}
