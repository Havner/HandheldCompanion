using hidapi;
using steam_hidapi.net.Hid;
using steam_hidapi.net.Util;
using System;
using System.Text;
using System.Threading.Tasks;

namespace steam_hidapi.net
{
    public class SteamController
    {
        protected HidDevice _hidDevice;
        protected ushort _vid, _pid;
        protected short _index;
        // TODO: why task not thread? HID read loop is a thread, rumble is a thread
        protected Task _configureTask;
        protected bool _active = false;
        protected bool _lizard = true;
        protected bool _gyro = false;

        public string SerialNumber { get; private set; }

        public SteamController(ushort vid, ushort pid, short index)
        {
            _vid = vid;
            _pid = pid;
            _index = index;
        }

        internal virtual void OnInputReceived(HidDeviceInputReceivedEventArgs e)
        {
        }

        internal virtual byte[] WriteSingleCmd(SCPacketType cmd)
        {
            return _hidDevice.RequestFeatureReport(new byte[] { (byte)cmd, 0x00 });
        }

        internal virtual byte[] WriteRegister(SCRegister reg, ushort value)
        {
            byte[] req = new byte[] {
                (byte)SCPacketType.WRITE_REGISTER,
                0x03,  // payload size
                (byte)reg,
                (byte)(value & 0xFF),  // lo
                (byte)(value >> 8) };  // hi

            return _hidDevice.RequestFeatureReport(req);
        }

        public virtual byte[] SetHaptic(byte position, ushort amplitude, ushort period, ushort count)
        {
            SCHapticPacket haptic = new SCHapticPacket();

            haptic.packet_type = 0x8f;
            haptic.len = 0x07;
            haptic.position = position;
            haptic.amplitude = amplitude;
            haptic.period = period;
            haptic.count = count;

            byte[] data = haptic.ToBytes();
            return _hidDevice.RequestFeatureReport(data);
        }

        public virtual void SetLizardMode(bool lizard)
        {
            _lizard = lizard;
        }

        public virtual void SetGyroscope(bool gyro)
        {
            _gyro = gyro;
        }

        internal virtual void ConfigureLizardMode(bool lizard)
        {
            if (lizard)
            {
                WriteSingleCmd(SCPacketType.DEFAULT_MAPPINGS);
                WriteSingleCmd(SCPacketType.DEFAULT_MOUSE);
            }
            else
            {
                WriteSingleCmd(SCPacketType.CLEAR_MAPPINGS);
                WriteRegister(SCRegister.RPAD_MODE, (ushort)SCLizardMouse.OFF);
                if (_pid == (ushort)SCPid.STEAMDECK)
                    WriteRegister(SCRegister.LPAD_MODE, (ushort)SCLizardMouse.OFF);
            }
        }

        internal virtual void ConfigureGyroscope(bool gyro)
        {
        }

        internal virtual async void ConfigureLoop()
        {
            while (_active)
            {
                try
                {
                    ConfigureLizardMode(_lizard);
                    ConfigureGyroscope(_gyro);
                }
                catch { }
                await Task.Delay(1000);
            }
        }

        public virtual string ReadSerialNumber()
        {
            byte[] request = new byte[] { 0xAE, 0x15, 0x01 };
            byte[] response = _hidDevice.RequestFeatureReport(request);
            byte[] serial = new byte[response.Length - 5];
            Array.Copy(response, 4, serial, 0, serial.Length);

            return Encoding.ASCII.GetString(serial).TrimEnd((Char)0);
        }

        public virtual void Open()
        {
            if (!_hidDevice.OpenDevice())
                throw new Exception("Could not open device!");
            SerialNumber = ReadSerialNumber();
            _hidDevice.BeginRead();

            _active = true;
            _configureTask = Task.Run(ConfigureLoop);
        }

        public virtual void Close()
        {
            _active = false;
            _configureTask.Wait();

            ConfigureLizardMode(_lizard);
            ConfigureGyroscope(_gyro);

            if (_hidDevice.IsDeviceValid)
                _hidDevice.EndRead();
            _hidDevice.Dispose();
        }
    }
}
