using ControllerCommon.Controllers;
using ControllerCommon.Managers;
using ControllerCommon.Utils;
using Nefarius.ViGEm.Client;
using System;

namespace HandheldCompanion.Targets
{
    public abstract class ViGEmTarget : IDisposable
    {
        protected ControllerState Inputs = new();

        public HIDmode HID = HIDmode.NoController;

        protected IVirtualGamepad virtualController;

        public event ConnectedEventHandler Connected;
        public delegate void ConnectedEventHandler(ViGEmTarget target);

        public event DisconnectedEventHandler Disconnected;
        public delegate void DisconnectedEventHandler(ViGEmTarget target);

        public bool IsConnected = false;

        protected ViGEmTarget()
        {
        }

        public override string ToString()
        {
            return EnumUtils.GetDescriptionFromEnumValue(HID);
        }

        public virtual void Connect()
        {
            IsConnected = true;
            Connected?.Invoke(this);
            LogManager.LogInformation("{0} connected", ToString());
        }

        public virtual void Disconnect()
        {
            IsConnected = false;
            Disconnected?.Invoke(this);
            LogManager.LogInformation("{0} disconnected", ToString());
        }

        public void UpdateInputs(ControllerState inputs)
        {
            Inputs = inputs;
        }

        public virtual unsafe void UpdateReport(long ticks)
        {
        }

        public virtual void Dispose()
        {
            this.Disconnect();
            GC.SuppressFinalize(this);
        }
    }
}