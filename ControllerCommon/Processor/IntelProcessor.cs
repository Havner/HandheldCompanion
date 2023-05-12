using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ControllerCommon.Processor
{
    public class IntelProcessor : Processor
    {
        public IntelProcessor() : base()
        {
            string family = ProcessorID.Substring(ProcessorID.Length - 5);

            switch (family)
            {
                default:
                case "206A7": // SandyBridge
                case "306A9": // IvyBridge
                case "40651": // Haswell
                case "306D4": // Broadwell
                case "406E3": // Skylake
                case "906ED": // CoffeeLake
                case "806E9": // AmberLake
                case "706E5": // IceLake
                case "806C1": // TigerLake U
                case "806C2": // TigerLake U Refresh
                case "806D1": // TigerLake H
                case "906A2": // AlderLake-P
                case "906A3": // AlderLake-P
                case "906A4": // AlderLake-P
                case "90672": // AlderLake-S
                case "90675": // AlderLake-S
                    canChangeTDP = true;
                    canChangeGPU = true;
                    break;
            }
        }

        public override void SetTDPLimit(uint limit, int result)
        {
        }

        public override void SetGPUClock(uint clock, int result)
        {
        }
    }
}
