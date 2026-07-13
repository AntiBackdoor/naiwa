using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace naiwa
{
    public class KeyboardHookService : IDisposable
    {
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc? _hookProc;
        private IntPtr _hookId = IntPtr.Zero;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public void InstallHook()
        {
            if (_hookId != IntPtr.Zero) return;

            _hookProc = HookCallback;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule!)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public void UninstallHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (IsBlockedKey(vkCode))
                {
                    return (IntPtr)1;
                }
                if (vkCode == 0x09 && (GetAsyncKeyState(0x12) & 0x8000) != 0)
                {
                    return (IntPtr)1;
                }
                if (vkCode == 0x1B && (GetAsyncKeyState(0x11) & 0x8000) != 0)
                {
                    return (IntPtr)1;
                }
                if (vkCode == 0x73 && ((GetAsyncKeyState(0x12) & 0x8000) != 0 || (GetAsyncKeyState(0x11) & 0x8000) != 0))
                {
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private bool IsBlockedKey(int vkCode)
        {
            return vkCode == 0x5B ||
                   vkCode == 0x5C;
        }

        public bool HandleSpecialKey(Key key, Key systemKey, ModifierKeys modifiers)
        {
            if (key == Key.F4)
            {
                if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt ||
                    (modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    return true;
                }
            }
            if (key == Key.System && systemKey == Key.F4)
            {
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            UninstallHook();
        }
    }
}
