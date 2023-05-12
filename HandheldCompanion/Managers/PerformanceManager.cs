using ControllerCommon.Devices;
using ControllerCommon.Managers;
using ControllerCommon.Processor;
using ControllerCommon.Utils;
using HandheldCompanion.Views;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace HandheldCompanion.Managers
{
    public static class PowerMode
    {
        /// <summary>
        /// Better Battery mode.
        /// </summary>
        public static Guid BetterBattery = new Guid("961cc777-2547-4f9d-8174-7d86181b8a7a");

        /// <summary>
        /// Better Performance mode.
        /// </summary>
        // public static Guid BetterPerformance = new Guid("3af9B8d9-7c97-431d-ad78-34a8bfea439f");
        public static Guid BetterPerformance = new Guid("00000000-0000-0000-0000-000000000000");

        /// <summary>
        /// Best Performance mode.
        /// </summary>
        public static Guid BestPerformance = new Guid("ded574b5-45a0-4f42-8737-46345c09c238");

        public static Guid[] PowerModes = new Guid[3] { BetterBattery, BetterPerformance, BestPerformance };
    }

    public class PerformanceManager : Manager
    {
        #region imports
        /// <summary>
        /// Retrieves the active overlay power scheme and returns a GUID that identifies the scheme.
        /// </summary>
        /// <param name="EffectiveOverlayPolicyGuid">A pointer to a GUID structure.</param>
        /// <returns>Returns zero if the call was successful, and a nonzero value if the call failed.</returns>
        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerGetEffectiveOverlayScheme")]
        private static extern uint PowerGetEffectiveOverlayScheme(out Guid EffectiveOverlayPolicyGuid);

        /// <summary>
        /// Sets the active power overlay power scheme.
        /// </summary>
        /// <param name="OverlaySchemeGuid">The identifier of the overlay power scheme.</param>
        /// <returns>Returns zero if the call was successful, and a nonzero value if the call failed.</returns>
        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerSetActiveOverlayScheme")]
        private static extern uint PowerSetActiveOverlayScheme(Guid OverlaySchemeGuid);
        #endregion

        private Processor processor;
        public static int MaxDegreeOfParallelism = 4;

        // timers
        private Timer powerWatchdog;

        private uint tdpRequest;
        private readonly Timer tdpWatchdog;
        protected object tdpLock = new();

        private uint gpuRequest;
        private readonly Timer gpuWatchdog;
        protected object gpuLock = new();

        private const short INTERVAL_DEFAULT = 2000;            // default interval between value scans

        // Power modes
        private Guid RequestedPowerMode;

        public PerformanceManager() : base()
        {
            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;

            HotkeysManager.CommandExecuted += HotkeysManager_CommandExecuted;

            processor = Processor.GetCurrent();

            powerWatchdog = new Timer() { Interval = INTERVAL_DEFAULT, AutoReset = true, Enabled = false };
            powerWatchdog.Elapsed += PowerWatchdog_Elapsed;

            tdpWatchdog = new Timer() { Interval = INTERVAL_DEFAULT, AutoReset = false, Enabled = false };
            tdpWatchdog.Elapsed += TDPWatchdog_Elapsed;

            gpuWatchdog = new Timer() { Interval = INTERVAL_DEFAULT, AutoReset = false, Enabled = false };
            gpuWatchdog.Elapsed += GPUWatchdog_Elapsed;

            MaxDegreeOfParallelism = Convert.ToInt32(Environment.ProcessorCount / 2);
        }

        private void SettingsManager_SettingValueChanged(string name, object value)
        {
            switch (name)
            {
                case "PerformanceTDPEnabled":
                    if (!CanChangeTDP())
                        return;

                    if (Convert.ToBoolean(value))
                        RequestTDP(SettingsManager.GetUInt("PerformanceTDPValue"));
                    else if (SettingsManager.IsInitialized)
                        RequestTDP(MainWindow.CurrentDevice.TDP[1]);
                    break;

                case "PerformanceTDPValue":
                    if (!CanChangeTDP())
                        return;

                    if (SettingsManager.GetBoolean("PerformanceTDPEnabled"))
                        RequestTDP(Convert.ToUInt32(value));
                    break;

                case "PerformanceGPUEnabled":
                    if (!CanChangeGPU())
                        return;

                    if (Convert.ToBoolean(value))
                        RequestGPU(SettingsManager.GetUInt("PerformanceGPUValue"));
                    else if (SettingsManager.IsInitialized)
                        RequestGPU(MainWindow.CurrentDevice.GPU[1]);
                    break;

                case "PerformanceGPUValue":
                    if (!CanChangeGPU())
                        return;

                    if (SettingsManager.GetBoolean("PerformanceGPUEnabled"))
                        RequestGPU(Convert.ToUInt32(value));
                    break;
            }
        }

        private void HotkeysManager_CommandExecuted(string listener)
        {
            switch (listener)
            {
                case "increaseTDP":
                case "decreaseTDP":
                    {
                        if (!SettingsManager.GetBoolean("PerformanceTDPEnabled"))
                            return;

                        uint currentTDP = SettingsManager.GetUInt("PerformanceTDPValue");
                        uint newTDP;
                        if (listener == "increaseTDP")
                            newTDP = currentTDP + 1;
                        else
                            newTDP = currentTDP - 1;

                        newTDP = Math.Clamp(newTDP, MainWindow.CurrentDevice.TDP[0], MainWindow.CurrentDevice.TDP[2]);
                        if (newTDP != currentTDP)
                            SettingsManager.SetProperty("PerformanceTDPValue", newTDP);
                    }
                    break;
            }
        }

        private void PowerWatchdog_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // Checking if active power shceme has changed
            if (PowerGetEffectiveOverlayScheme(out Guid activeScheme) == 0)
                if (activeScheme != RequestedPowerMode)
                    PowerSetActiveOverlayScheme(RequestedPowerMode);
        }

        public bool CanChangeTDP()
        {
            if (processor is null || !processor.IsInitialized)
                return false;

            return processor.CanChangeTDP();
        }

        public bool CanChangeGPU()
        {
            if (processor is null || !processor.IsInitialized)
                return false;

            return processor.CanChangeGPU();
        }

        private void TDPWatchdog_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (processor is null || !processor.IsInitialized)
                return;

            if (Monitor.TryEnter(tdpLock))
            {
                processor.SetTDPLimit(tdpRequest);
                Monitor.Exit(tdpLock);
            }
        }

        private void GPUWatchdog_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (processor is null || !processor.IsInitialized)
                return;

            if (Monitor.TryEnter(gpuLock))
            {
                if (processor.GetType() == typeof(AMDProcessor))
                    goto notsupported;
                else if (processor.GetType() == typeof(IntelProcessor))
                    goto notsupported;

                processor.SetGPUClock(gpuRequest);
            notsupported:
                Monitor.Exit(gpuLock);
            }
        }

        public void RequestTDP(uint value)
        {
            // update value read by timer
            if (Monitor.TryEnter(tdpLock))
            {
                tdpRequest = Math.Clamp(value, MainWindow.CurrentDevice.TDP[0], MainWindow.CurrentDevice.TDP[2]);
                tdpWatchdog.Stop();
                tdpWatchdog.Start();
                Monitor.Exit(tdpLock);
            }
        }

        public void RequestGPU(uint value)
        {
            // update value read by timer
            if (Monitor.TryEnter(gpuLock))
            {
                gpuRequest = Math.Clamp(value, MainWindow.CurrentDevice.GPU[0], MainWindow.CurrentDevice.GPU[1]);
                gpuWatchdog.Stop();
                gpuWatchdog.Start();
                Monitor.Exit(gpuLock);
            }
        }

        public void RequestPowerMode(int idx)
        {
            RequestedPowerMode = PowerMode.PowerModes[idx];
            LogManager.LogInformation("User requested power scheme: {0}", RequestedPowerMode);

            PowerSetActiveOverlayScheme(RequestedPowerMode);
        }

        public override void Start()
        {
            // initialize watchdog(s)
            powerWatchdog.Start();

            // just in case
            bool HypervisorEnforcedCodeIntegrityEnabled = RegistryUtils.GetBoolean(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled");
            bool VulnerableDriverBlocklistEnabled = RegistryUtils.GetBoolean(@"SYSTEM\CurrentControlSet\Control\CI\Config", "VulnerableDriverBlocklistEnabled");

            if (VulnerableDriverBlocklistEnabled || HypervisorEnforcedCodeIntegrityEnabled)
                LogManager.LogWarning("Core isolation settings are turned on. TDP read/write is disabled");

            base.Start();
        }

        public override void Stop()
        {
            if (!IsInitialized)
                return;

            powerWatchdog.Stop();
            tdpWatchdog.Stop();
            gpuWatchdog.Stop();

            base.Stop();
        }
    }
}
