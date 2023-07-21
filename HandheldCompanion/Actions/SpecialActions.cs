using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using HandheldCompanion.Simulators;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;

namespace HandheldCompanion.Actions
{
    [Serializable]
    public enum SpecialActionsType
    {
        [Description("Flick Stick")]
        FlickStick = 0,
    }

    [Serializable]
    public class SpecialActions : IActions
    {
        public SpecialActionsType SpecialType { get; set; }
        private FlickStick flickStick = new();

        // settings
        public float FlickstickDuration = 0.1f;    // 0.1 - 0.8
        public float FlickstickSensivity = 3.0f;   // 0.1 - 10.0

        public SpecialActions()
        {
            this.ActionType = ActionType.Special;
        }

        public SpecialActions(SpecialActionsType type) : this()
        {
            this.SpecialType = type;
        }

        public void Execute(AxisLayout layout)
        {
            if (layout.vector == Vector2.Zero)
                return;

            float flickStickX = flickStick.Handle(layout.vector, FlickstickDuration, FlickstickSensivity);

            flickStickX /= 1000;

            MouseSimulator.MoveBy((int)flickStickX, 0);
        }
    }
}
