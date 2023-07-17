using HandheldCompanion.Managers;
using System.Management;

namespace HandheldCompanion.Processor
{
    public class IProcessor
    {
        private static ManagementClass managClass = new ManagementClass("win32_processor");

        private static IProcessor processor;
        private static string Manufacturer;

        protected string Name, ProcessorID;

        protected bool canChangeTDP = false, canChangeGPU = false;
        protected object IsBusy = new();
        public bool IsInitialized = false;

        public static IProcessor GetCurrent()
        {
            if (processor is not null)
                return processor;

            Manufacturer = GetProcessorDetails("Manufacturer");

            switch (Manufacturer)
            {
                case "GenuineIntel":
                    processor = new IntelProcessor();
                    break;
                case "AuthenticAMD":
                    processor = new AMDProcessor();
                    break;
            }

            return processor;
        }

        private static string GetProcessorDetails(string value)
        {
            var managCollec = managClass.GetInstances();
            foreach (ManagementObject managObj in managCollec)
                return managObj.Properties[value].Value.ToString();

            return string.Empty;
        }

        public IProcessor()
        {
            Name = GetProcessorDetails("Name");
            ProcessorID = GetProcessorDetails("processorID");
        }

        public bool CanChangeTDP()
        {
            return canChangeTDP;
        }

        public bool CanChangeGPU()
        {
            return canChangeGPU;
        }

        public virtual void SetTDPLimit(uint limit, int result = 0)
        {
            LogManager.LogInformation("User requested TDP limit: {0}, error code: {1}", limit, result);
        }

        public virtual void SetGPUClock(uint clock, int result = 0)
        {
            LogManager.LogInformation("User requested GPU clock: {0}, error code: {1}", clock, result);
        }
    }
}
