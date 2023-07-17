using HandheldCompanion.Processor.AMD;
using System;
using System.Threading;

namespace HandheldCompanion.Processor
{
    public class AMDProcessor : IProcessor
    {
        public IntPtr ry;
        public RyzenFamily family;

        public AMDProcessor() : base()
        {
            ry = RyzenAdj.init_ryzenadj();

            if (ry == IntPtr.Zero)
                return;

            family = RyzenAdj.get_cpu_family(ry);
            IsInitialized = true;

            switch (family)
            {
                case RyzenFamily.FAM_RENOIR:
                case RyzenFamily.FAM_LUCIENNE:
                case RyzenFamily.FAM_CEZANNE:
                case RyzenFamily.FAM_VANGOGH:
                case RyzenFamily.FAM_REMBRANDT:
                    canChangeGPU = true;
                    break;
            }

            switch (family)
            {
                case RyzenFamily.FAM_RAVEN:
                case RyzenFamily.FAM_PICASSO:
                case RyzenFamily.FAM_DALI:
                case RyzenFamily.FAM_RENOIR:
                case RyzenFamily.FAM_LUCIENNE:
                case RyzenFamily.FAM_CEZANNE:
                case RyzenFamily.FAM_VANGOGH:
                case RyzenFamily.FAM_REMBRANDT:
                    canChangeTDP = true;
                    break;
            }
        }

        public override void SetTDPLimit(uint limit, int result)
        {
            if (!IsInitialized)
                return;

            if (Monitor.TryEnter(base.IsBusy))
            {
                // 15W : 15000
                limit *= 1000;

                var error1 = RyzenAdj.set_stapm_limit(ry, limit / 10);
                var error2 = RyzenAdj.set_slow_limit(ry, limit);
                var error3 = RyzenAdj.set_fast_limit(ry, limit);

                base.SetTDPLimit(limit, error1 + error2 + error3);

                Monitor.Exit(base.IsBusy);
            }
        }

        public override void SetGPUClock(uint clock, int result)
        {
            if (!IsInitialized)
                return;

            if (Monitor.TryEnter(base.IsBusy))
            {
                //var error1 = RyzenAdj.set_gfx_clk(ry, (uint)clock);
                //var error2 = RyzenAdj.set_min_gfxclk_freq(ry, (uint)clock);
                var error3 = RyzenAdj.set_max_gfxclk_freq(ry, (uint)clock);

                base.SetGPUClock(clock, /*error1 + error2 +*/ error3);

                Monitor.Exit(base.IsBusy);
            }
        }
    }
}
