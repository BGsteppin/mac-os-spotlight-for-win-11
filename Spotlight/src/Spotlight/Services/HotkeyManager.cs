using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace WinSpotlight.Services
{
    public sealed class HotkeyManager : IDisposable
    {
        private readonly Action _callback;
        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc? _proc;

        public HotkeyManager(Action callback)
        {
            _callback = callback;
        }

        public void Register()
        {
            _proc = HookCallback;
            _hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = KeyInterop.KeyFromVirtualKey(vkCode);
                bool isKeyDown = wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN;
                if (isKeyDown && key == Key.Space)
                {
                    bool winPressed = NativeMethods.IsKeyPressed(NativeMethods.VK_LWIN) || NativeMethods.IsKeyPressed(NativeMethods.VK_RWIN);
                    if (winPressed)
                    {
                        _callback();
                        return (IntPtr)1;
                    }
                }
            }

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static class NativeMethods
        {
            internal const int WH_KEYBOARD_LL = 13;
            internal const int WM_KEYDOWN = 0x0100;
            internal const int WM_SYSKEYDOWN = 0x0104;
            internal const int VK_LWIN = 0x5B;
            internal const int VK_RWIN = 0x5C;

            internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            internal static extern short GetAsyncKeyState(int vKey);

            internal static bool IsKeyPressed(int vKey)
            {
                return (GetAsyncKeyState(vKey) & 0x8000) != 0;
            }
        }
    }
}
