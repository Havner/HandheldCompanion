using hidapi;
using steam_hidapi.net.Hid;
using steam_hidapi.net.Util;
using System;
using System.Text;
using System.Threading.Tasks;

namespace steam_hidapi.net
{
    // TODO: create abstract SteamController with common methods
    public class GordonController
    {
        private HidDevice _hidDevice;
        private ushort _vid = 0x28de, _pid = 0x1142;

        public string SerialNumber { get; private set; }
        public Func<GordonControllerInputEventArgs, Task> OnControllerInputReceived;

        public GordonController(short version, short index)
        {
            // TODO: verify, utilize
            switch (version)
            {
                case (short)SCVersion.WIRED:
                    break;
                case (short)SCVersion.WIRELESS:
                    break;
                case (short)SCVersion.STEAMDECK:
                    break;
            }

            _hidDevice = new HidDevice(_vid, _pid, 64, index);
            _hidDevice.OnInputReceived = input => Task.Run(() => OnInputReceived(input));
        }

        private void OnInputReceived(HidDeviceInputReceivedEventArgs e)
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
                    // TODO: verify, utilize
                    break;
            }
        }

        private byte[] WriteSingleCmd(GCPacketType cmd)
        {
            return _hidDevice.RequestFeatureReport(new byte[] { (byte)cmd, 0x00 });
        }

        private byte[] WriteRegister(GCRegister reg, ushort value)
        {
            byte[] req = new byte[] {
                (byte)GCPacketType.WRITE_REGISTER,
                0x03,  // payload size
                (byte)reg,
                (byte)(value & 0xFF),  // lo
                (byte)(value >> 8) };  // hi

            return _hidDevice.RequestFeatureReport(req);
        }

        public bool SetHaptic(byte position, ushort amplitude, ushort period, ushort count)
        {
            NCHapticPacket haptic = new NCHapticPacket();

            haptic.packet_type = 0x8f;
            haptic.len = 0x07;
            haptic.position = position;
            haptic.amplitude = amplitude;
            haptic.period = period;
            haptic.count = count;

            byte[] data = haptic.ToBytes();

            _hidDevice.RequestFeatureReport(data);

            return true;
        }

        public Task<byte[]> SetHaptic2(HapticPad position, HapticStyle style, sbyte intensity)
        {
            NCHapticPacket2 haptic = new NCHapticPacket2();

            haptic.packet_type = 0xea;
            haptic.len = 0xd;
            haptic.position = position;
            haptic.style = style;
            haptic.unsure3 = 0x4;
            haptic.intensity = intensity;

            var ts = Environment.TickCount;
            haptic.tsA = ts;
            haptic.tsB = ts;

            byte[] data = haptic.ToBytes();

            return _hidDevice.RequestFeatureReportAsync(data);
        }

        public void SetGyroscope(bool gyro)
        {
            if (gyro)
            {
                WriteRegister(GCRegister.GYRO_MODE,
                    (ushort)((byte)GCGyroMode.ACCEL | (byte)GCGyroMode.GYRO));
            }
            else
            {
                WriteRegister(GCRegister.GYRO_MODE, 0x00);
            }
        }

        public void SetLizardMode(bool lizard)
        {
            if (lizard)
            {
                WriteSingleCmd(GCPacketType.DEFAULT_MAPPINGS);
                WriteSingleCmd(GCPacketType.DEFAULT_MOUSE);
            }
            else
            {
                WriteSingleCmd(GCPacketType.CLEAR_MAPPINGS);
                WriteRegister(GCRegister.LIZARD_MOUSE, (ushort)GCLizardMouse.OFF);
            }
        }

        public string ReadSerialNumber()
        {
            byte[] request = new byte[] { 0xAE, 0x15, 0x01 };
            byte[] response = _hidDevice.RequestFeatureReport(request);
            byte[] serial = new byte[response.Length - 5];
            Array.Copy(response, 4, serial, 0, serial.Length);

            return Encoding.ASCII.GetString(serial).TrimEnd((Char)0);
        }

        public void Open()
        {
            if (!_hidDevice.OpenDevice())
                throw new Exception("Could not open device!");
            SerialNumber = ReadSerialNumber();
            _hidDevice.BeginRead();
        }

        public void Close()
        {
            if (_hidDevice.IsDeviceValid)
                _hidDevice.EndRead();
            _hidDevice.Dispose();
        }
    }
}
