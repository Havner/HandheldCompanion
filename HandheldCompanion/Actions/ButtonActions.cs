using HandheldCompanion.Inputs;
using System;

namespace HandheldCompanion.Actions
{
    [Serializable]
    public class ButtonActions : IActions
    {
        public ButtonFlags Button;

        public ButtonActions()
        {
            this.ActionType = ActionType.Button;

            this.Value = false;
            this.prevValue = false;
        }

        public ButtonActions(ButtonFlags button) : this()
        {
            this.Button = button;
        }

        public bool GetValue()
        {
            return (bool)this.Value;
        }
    }
}
