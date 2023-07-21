using System;

namespace HandheldCompanion.Misc
{
    public class OneEuroFilterPair
    {
        public const double DEFAULT_WHEEL_CUTOFF = 0.005;
        public const double DEFAULT_WHEEL_BETA = 0.004;

        public OneEuroFilter axis1Filter = new(DEFAULT_WHEEL_CUTOFF, DEFAULT_WHEEL_BETA);
        public OneEuroFilter axis2Filter = new(DEFAULT_WHEEL_CUTOFF, DEFAULT_WHEEL_BETA);
    }

    public class OneEuroFilter3D
    {
        public const double DEFAULT_WHEEL_CUTOFF = 0.4;
        public const double DEFAULT_WHEEL_BETA = 0.2;

        public OneEuroFilter axis1Filter = new(DEFAULT_WHEEL_CUTOFF, DEFAULT_WHEEL_BETA);
        public OneEuroFilter axis2Filter = new(DEFAULT_WHEEL_CUTOFF, DEFAULT_WHEEL_BETA);
        public OneEuroFilter axis3Filter = new(DEFAULT_WHEEL_CUTOFF, DEFAULT_WHEEL_BETA);

        public void SetFilterAttrs(double minCutoff, double beta)
        {
            axis1Filter.MinCutoff = axis2Filter.MinCutoff = axis3Filter.MinCutoff = minCutoff;
            axis1Filter.Beta = axis2Filter.Beta = axis3Filter.Beta = beta;
        }
    }

    public class OneEuroFilter
    {
        protected double dcutoff;
        protected LowpassFilter dxFilt;

        protected bool firstTime;
        protected OneEuroSettings settings;
        protected LowpassFilter xFilt;

        public OneEuroFilter(double minCutoff, double beta)
        {
            settings = new OneEuroSettings(minCutoff, beta);
            firstTime = true;

            xFilt = new LowpassFilter();
            dxFilt = new LowpassFilter();
            dcutoff = 1;
        }

        public double MinCutoff
        {
            get => settings.minCutoff;
            set => settings.minCutoff = value;
        }

        public double Beta
        {
            get => settings.beta;
            set => settings.beta = value;
        }

        public double Filter(double x, double rate)
        {
            var dx = firstTime ? 0 : (x - xFilt.Last()) * rate;
            if (firstTime) firstTime = false;

            var edx = dxFilt.Filter(dx, Alpha(rate, dcutoff));
            var cutoff = settings.minCutoff + settings.beta * Math.Abs(edx);

            return xFilt.Filter(x, Alpha(rate, cutoff));
        }

        protected double Alpha(double rate, double cutoff)
        {
            var tau = 1.0 / (2 * Math.PI * cutoff);
            var te = 1.0 / rate;
            return 1.0 / (1.0 + tau / te);
        }

        public class OneEuroSettings
        {
            public double beta;
            public double minCutoff;

            public OneEuroSettings(double minCutoff, double beta)
            {
                this.minCutoff = minCutoff;
                this.beta = beta;
            }
        }
    }

    public class LowpassFilter
    {
        protected bool firstTime;
        protected double hatXPrev;

        public LowpassFilter()
        {
            firstTime = true;
        }

        public double Last()
        {
            return hatXPrev;
        }

        public double Filter(double x, double alpha)
        {
            double hatX = 0;
            if (firstTime)
            {
                firstTime = false;
                hatX = x;
            }
            else
            {
                hatX = alpha * x + (1 - alpha) * hatXPrev;
            }

            hatXPrev = hatX;

            return hatX;
        }
    }
}