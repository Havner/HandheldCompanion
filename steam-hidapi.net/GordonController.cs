using hidapi;
using steam_hidapi.net.Hid;
using steam_hidapi.net.Util;
using System;
using System.Text;
using System.Threading.Tasks;

namespace steam_hidapi.net
{
    public class GordonController : SteamController
    {
        public Func<GordonControllerInputEventArgs, Task> OnControllerInputReceived;

        public GordonController(short version, short index) : base(version, index)
        {
            switch (version)
            {
                // TODO: verify, what is wired's index?
                case (short)SCVersion.WIRED:
                    _vid = 0x28de;
                    _pid = 0x1102;
                    break;
                case (short)SCVersion.WIRELESS:
                    _vid = 0x28de;
                    _pid = 0x1142;
                    break;
            }

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
                    {
                        GCInput input = e.Buffer.ToStructure<GCInput>();
                        GordonControllerInputState state = new GordonControllerInputState(input);
                        if (OnControllerInputReceived != null)
                            OnControllerInputReceived(new GordonControllerInputEventArgs(state));
                    }
                    break;
                case (byte)SCEventType.CONNECT:
                case (byte)SCEventType.BATTERY:
                    // TODO: useful?
                    break;
                case (byte)SCEventType.DECK_INPUT_DATA:
                    break;
            }
        }

        public void SetGyroscope(bool gyro)
        {
            if (gyro)
            {
                WriteRegister(SCRegister.GYRO_MODE,
                    (ushort)((byte)GCGyroMode.ACCEL | (byte)GCGyroMode.GYRO));
            }
            else
            {
                WriteRegister(SCRegister.GYRO_MODE, 0x00);
            }
        }
    }
}
