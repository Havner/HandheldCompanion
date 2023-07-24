using GregsStack.InputSimulatorStandard.Native;
using HandheldCompanion.Inputs;
using HandheldCompanion.Simulators;
using System;
using WindowsInput.Events;

namespace HandheldCompanion.Actions
{
    [Serializable]
    public class KeyboardActions : IActions
    {
        public VirtualKeyCode Key;
        private bool IsKeyDown;
        private KeyCode[] pressed;

        // settings
        public ModifierSet Modifiers = ModifierSet.None;

        public KeyboardActions()
        {
            this.ActionType = ActionType.Keyboard;
            this.IsKeyDown = false;

            this.Value = false;
            this.prevValue = false;
        }

        public KeyboardActions(VirtualKeyCode key) : this()
        {
            this.Key = key;
        }

        public override void Execute(ButtonFlags button, bool value, int longTime)
        {
            base.Execute(button, value, longTime);

            switch (this.Value)
            {
                case true:
                    {
                        if (IsKeyDown)
                            return;

                        IsKeyDown = true;
                        pressed = ModifierMap[Modifiers];
                        KeyboardSimulator.KeyDown(pressed);
                        KeyboardSimulator.KeyDown(Key);
                    }
                    break;
                case false:
                    {
                        if (!IsKeyDown)
                            return;

                        IsKeyDown = false;
                        KeyboardSimulator.KeyUp(Key);
                        KeyboardSimulator.KeyUp(pressed);
                    }
                    break;
            }
        }
    }
}
