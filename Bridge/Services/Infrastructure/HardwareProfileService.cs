using System.Runtime.InteropServices;
using Bridge.Models;

namespace Bridge.Services.Infrastructure;

public sealed class HardwareProfileService
{
    public HardwareProfile GetCurrent()
    {
        var memory = new MemoryStatusEx();
        var totalRam = GlobalMemoryStatusEx(memory)
            ? memory.TotalPhysical / 1024d / 1024d / 1024d
            : 0;

        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var root = Path.GetPathRoot(localData);
        var freeDisk = root is null
            ? 0
            : new DriveInfo(root).AvailableFreeSpace / 1024d / 1024d / 1024d;

        return new HardwareProfile(totalRam, Environment.ProcessorCount, freeDisk);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}
