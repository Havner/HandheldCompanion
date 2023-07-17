using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HandheldCompanion.Misc
{
    public class InpOut : IDisposable
    {
        // Author: Kamil Trzciński, 2022 (https://github.com/ayufan/steam-deck-tools/)

        public const string LibraryName = "inpoutx64.dll";

        private nint libraryHandle;
        public MapPhysToLinDelegate MapPhysToLin;
        public UnmapPhysicalMemoryDelegate UnmapPhysicalMemory;
        public DlPortReadPortUcharDelegate DlPortReadPortUchar;
        public DlPortWritePortUcharDelegate DlPortWritePortUchar;

        public InpOut()
        {
            string fileName = Path.GetFullPath(Path.Combine("Resources", LibraryName));
            libraryHandle = LoadLibrary(fileName);

            try
            {
                var addr = GetProcAddress(libraryHandle, "MapPhysToLin");
                if (addr == nint.Zero)
                    throw new ArgumentException("Missing MapPhysToLin");
                MapPhysToLin = Marshal.GetDelegateForFunctionPointer<MapPhysToLinDelegate>(addr);

                addr = GetProcAddress(libraryHandle, "UnmapPhysicalMemory");
                if (addr == nint.Zero)
                    throw new ArgumentException("Missing UnmapPhysicalMemory");
                UnmapPhysicalMemory = Marshal.GetDelegateForFunctionPointer<UnmapPhysicalMemoryDelegate>(addr);

                addr = GetProcAddress(libraryHandle, "UnmapPhysicalMemory");
                if (addr == nint.Zero)
                    throw new ArgumentException("Missing UnmapPhysicalMemory");
                UnmapPhysicalMemory = Marshal.GetDelegateForFunctionPointer<UnmapPhysicalMemoryDelegate>(addr);

                addr = GetProcAddress(libraryHandle, "DlPortReadPortUchar");
                if (addr == nint.Zero)
                    throw new ArgumentException("Missing DlPortReadPortUchar");
                DlPortReadPortUchar = Marshal.GetDelegateForFunctionPointer<DlPortReadPortUcharDelegate>(addr);

                addr = GetProcAddress(libraryHandle, "DlPortWritePortUchar");
                if (addr == nint.Zero)
                    throw new ArgumentException("Missing DlPortWritePortUchar");
                DlPortWritePortUchar = Marshal.GetDelegateForFunctionPointer<DlPortWritePortUcharDelegate>(addr);
            }
            catch
            {
                FreeLibrary(libraryHandle);
                libraryHandle = nint.Zero;
                throw;
            }
        }

        ~InpOut()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            FreeLibrary(libraryHandle);
            libraryHandle = nint.Zero;
        }

        public byte[] ReadMemory(nint baseAddress, uint size)
        {
            nint pdwLinAddr = MapPhysToLin(baseAddress, size, out nint pPhysicalMemoryHandle);
            if (pdwLinAddr != nint.Zero)
            {
                byte[] bytes = new byte[size];
                Marshal.Copy(pdwLinAddr, bytes, 0, bytes.Length);
                UnmapPhysicalMemory(pPhysicalMemoryHandle, pdwLinAddr);
                return bytes;
            }
            return null;
        }

        public bool WriteMemory(nint baseAddress, byte[] data)
        {
            nint pdwLinAddr = MapPhysToLin(baseAddress, (uint)data.Length, out nint pPhysicalMemoryHandle);
            if (pdwLinAddr != nint.Zero)
            {
                Marshal.Copy(data, 0, pdwLinAddr, data.Length);
                UnmapPhysicalMemory(pPhysicalMemoryHandle, pdwLinAddr);
                return true;
            }

            return false;
        }

        public delegate nint MapPhysToLinDelegate(nint pbPhysAddr, uint dwPhysSize, out nint pPhysicalMemoryHandle);
        public delegate bool UnmapPhysicalMemoryDelegate(nint PhysicalMemoryHandle, nint pbLinAddr);
        public delegate byte DlPortReadPortUcharDelegate(ushort port);
        public delegate byte DlPortWritePortUcharDelegate(ushort port, byte value);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern nint GetProcAddress(nint module, string methodName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(nint module);
    }
}
