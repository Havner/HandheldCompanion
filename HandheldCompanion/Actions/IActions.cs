using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using System;

namespace HandheldCompanion.Actions
{
    [Serializable]
    public enum ActionType
    {
        Disabled = 0,
        Button = 1,
        Joystick = 2,
        Keyboard = 3,
        Mouse = 4,
        Trigger = 5,
        Special = 6,
    }

    [Serializable]
    public abstract class IActions : ICloneable
    {
        public ActionType ActionType { get; set; } = ActionType.Disabled;

        protected object Value;
        protected object prevValue;

        protected int Period;

        public bool Turbo { get; set; }
        public byte TurboDelay { get; set; } = 90;
        protected int TurboIdx;
        protected bool IsTurboed;

        public bool Toggle { get; set; }
        protected bool IsToggled;

        public IActions()
        {
            Period = TimerManager.GetPeriod();
        }

        public virtual void Execute(ButtonFlags button, bool value)
        {
            if (Toggle)
            {
                if ((bool)prevValue != value && value)
                    IsToggled = !IsToggled;
            }
            else
                IsToggled = false;

            if (Turbo)
            {
                if (value || IsToggled)
                {
                    if (TurboIdx % TurboDelay == 0)
                        IsTurboed = !IsTurboed;

                    TurboIdx += Period;
                }
                else
                {
                    IsTurboed = false;
                    TurboIdx = 0;
                }
            }
            else
                IsTurboed = false;

            // update previous value
            prevValue = value;

            // update value
            if (Toggle && Turbo)
                this.Value = IsToggled && IsTurboed;
            else if (Toggle)
                this.Value = IsToggled;
            else if (Turbo)
                this.Value = IsTurboed;
            else
                this.Value = value;
        }

        public virtual void Execute(AxisFlags axis, bool value)
        {
        }

        public virtual void Execute(AxisFlags axis, short value)
        {
        }

        // Improve me !
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
