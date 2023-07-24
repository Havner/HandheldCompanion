using GregsStack.InputSimulatorStandard.Native;
using HandheldCompanion.Inputs;
using HandheldCompanion.Simulators;
using System;

namespace HandheldCompanion.Actions
{
    [Serializable]
    public class KeyboardActions : IActions
    {
        public VirtualKeyCode Key { get; set; }
        private bool IsKeyDown { get; set; }
        private bool IsKeyUp { get; set; }

        public KeyboardActions()
        {
            this.ActionType = ActionType.Keyboard;
            this.IsKeyDown = false;
            this.IsKeyUp = true;

            this.Value = false;
            this.prevValue = false;
        }

        public KeyboardActions(VirtualKeyCode key) : this()
        {
            this.Key = key;
        }

        public override void Execute(ButtonFlags button, bool value)
        {
            base.Execute(button, value);

            switch (this.Value)
            {
                case true:
                    {
                        if (IsKeyDown || !IsKeyUp)
                            return;

                        IsKeyDown = true;
                        IsKeyUp = false;
                        KeyboardSimulator.KeyDown(Key);
                    }
                    break;
                case false:
                    {
                        if (IsKeyUp || !IsKeyDown)
                            return;

                        IsKeyUp = true;
                        IsKeyDown = false;
                        KeyboardSimulator.KeyUp(Key);
                    }
                    break;
            }
        }
    }
}
