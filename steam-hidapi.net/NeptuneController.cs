using hidapi;
using steam_hidapi.net.Hid;
using steam_hidapi.net.Util;
using System;
using System.Threading.Tasks;

namespace steam_hidapi.net
{
    public class NeptuneController : SteamController
    {
        public Func<NeptuneControllerInputEventArgs, Task> OnControllerInputReceived;

        public NeptuneController(ushort vid, ushort pid, short index) : base(vid, pid, index)
        {
            _hidDevice = new HidDevice(_vid, _pid, 64, index);
            _hidDevice.OnInputReceived = input => Task.Run(() => OnInputReceived(input));
        }

        internal override void OnInputReceived(HidDeviceInputReceivedEventArgs e)
        {
            // this should always be so
            if ((e.Buffer[0] != 1) || (e.Buffer[1] != 0))
                return;

            switch (e.Buffer[2])
            {
                case (byte)SCEventType.INPUT_DATA:
                    break;
                case (byte)SCEventType.CONNECT:
                case (byte)SCEventType.BATTERY:
                    // TODO: useful?
                    break;
                case (byte)SCEventType.DECK_INPUT_DATA:
                    {
                        NCInput input = e.Buffer.ToStructure<NCInput>();
                        NeptuneControllerInputState state = new NeptuneControllerInputState(input);
                        if (OnControllerInputReceived != null)
                            OnControllerInputReceived(new NeptuneControllerInputEventArgs(state));
                    }
                    break;
            }
        }

        public byte[] SetHaptic2(SCHapticPad position, NCHapticStyle style, sbyte intensity)
        {
            NCHapticPacket2 haptic = new NCHapticPacket2();

            haptic.packet_type = (byte)SCPacketType.SET_HAPTIC2;
            haptic.len = 0xd;
            haptic.position = position;
            haptic.style = style;
            haptic.unsure3 = 0x4;
            haptic.intensity = intensity;
            var ts = Environment.TickCount;
            haptic.tsA = ts;
            haptic.tsB = ts;

            byte[] data = haptic.ToBytes();
            return _hidDevice.RequestFeatureReport(data);
        }
    }
}
