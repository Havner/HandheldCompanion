using hidapi;
using steam_hidapi.net.Hid;
using steam_hidapi.net.Util;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace steam_hidapi.net
{
    public class GordonController
    {
        private HidDevice _hidDevice;
        private ushort _vid = 0x28de, _pid = 0x1142;

        public string SerialNumber { get; private set; }
        public Func<GordonControllerInputEventArgs, Task> OnControllerInputReceived;

        public GordonController(short index)
        {
            _hidDevice = new HidDevice(_vid, _pid, 64, index);
            _hidDevice.OnInputReceived = input => Task.Run(() => OnInputReceived(input));
        }

        private void OnInputReceived(HidDeviceInputReceivedEventArgs e)
        {
            if (e.Buffer[0] == 1)
            {
                GCInput input = e.Buffer.ToStructure<GCInput>();
                GordonControllerInputState state = new GordonControllerInputState(input);
                if (OnControllerInputReceived != null)
                    OnControllerInputReceived(new GordonControllerInputEventArgs(state));
            }
            else
            {

            }
        }

        private double MapValue(double a, double b, double c) => a / b * c;

        public async Task<bool> SetHaptic(byte position, ushort amplitude, ushort period, ushort count)
        {
            NCHapticPacket haptic = new NCHapticPacket();

            haptic.packet_type = 0x8f;
            haptic.len = 0x07;
            haptic.position = position;
            haptic.amplitude = amplitude;
            haptic.period = period;
            haptic.count = count;

            byte[] data = GetHapticDataBytes(haptic);

            await _hidDevice.RequestFeatureReportAsync(data);

            return true;
        }

        private byte[] GetHapticDataBytes(NCHapticPacket packet)
        {
            int size = Marshal.SizeOf(packet);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(packet, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
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

            byte[] data = GetHapticDataBytes(haptic);

            return _hidDevice.RequestFeatureReportAsync(data);
        }

        private byte[] GetHapticDataBytes(NCHapticPacket2 packet)
        {
            int size = Marshal.SizeOf(packet);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(packet, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public bool SetLizardMode(bool lizard)
        {
            byte[] data;
            try
            {
                if (!lizard)
                {
                    data = new byte[] { (byte)GCPacketType.STEAM_CMD_CLEAR_MAPPINGS };
                    _hidDevice.RequestFeatureReport(data);
                    data = new byte[] { (byte)GCPacketType.STEAM_CMD_WRITE_REGISTER, 0x03, 0x08, 0x07 };
                    _hidDevice.RequestFeatureReport(data);
                }
                else
                {
                    data = new byte[] { (byte)GCPacketType.STEAM_CMD_DEFAULT_MAPPINGS };
                    _hidDevice.RequestFeatureReport(data);
                    data = new byte[] { (byte)GCPacketType.STEAM_CMD_DEFAULT_MOUSE };
                    _hidDevice.RequestFeatureReport(data);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public async Task<string> ReadSerialNumberAsync()
        {
            byte[] request = new byte[] { 0xAE, 0x15, 0x01 };
            byte[] response = await _hidDevice.RequestFeatureReportAsync(request);
            byte[] serial = new byte[response.Length - 5];
            Array.Copy(response, 4, serial, 0, serial.Length);

            return Encoding.ASCII.GetString(serial).TrimEnd((Char)0);
        }
        public string ReadSerialNumber()
        {
            byte[] request = new byte[] { 0xAE, 0x15, 0x01 };
            byte[] response = _hidDevice.RequestFeatureReport(request);
            byte[] serial = new byte[response.Length - 5];
            Array.Copy(response, 4, serial, 0, serial.Length);

            return Encoding.ASCII.GetString(serial).TrimEnd((Char)0);
        }

        public async Task OpenAsync()
        {
            if (!await _hidDevice.OpenDeviceAsync())
                throw new Exception("Could not open device!");
            SerialNumber = await ReadSerialNumberAsync();
            _hidDevice.BeginRead();
        }
        public void Open()
        {
            if (!_hidDevice.OpenDevice())
                throw new Exception("Could not open device!");
            SerialNumber = ReadSerialNumber();
            _hidDevice.BeginRead();
        }

        public Task CloseAsync() => Task.Run(() => Close());
        public void Close()
        {
            if (_hidDevice.IsDeviceValid)
                _hidDevice.EndRead();
            _hidDevice.Dispose();
        }
    }
}
