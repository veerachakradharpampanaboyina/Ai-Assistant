using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AIAssistant;

public static class WindowAutomation
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public InputUnion u;
        public static int Size => Marshal.SizeOf(typeof(INPUT));
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    const uint INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_KEYUP = 0x0002;
    const ushort VK_CONTROL = 0x11;
    const ushort VK_V = 0x56;

    public static async Task PasteTextIntoWindowAsync(IntPtr targetWindow, string text)
    {
        string? oldText = null;

        // Save current clipboard
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Clipboard.ContainsText())
                oldText = Clipboard.GetText();

            // Set new clipboard text
            Clipboard.SetText(text);
        });

        // Bring target window to foreground
        SetForegroundWindow(targetWindow);

        // Wait a little bit for the window to gain focus
        await Task.Delay(300);

        // Send Ctrl (Down)
        var inputs = new INPUT[4];
        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = 0 } }
        };

        // Send V (Down)
        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_V, dwFlags = 0 } }
        };

        // Send V (Up)
        inputs[2] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_V, dwFlags = KEYEVENTF_KEYUP } }
        };

        // Send Ctrl (Up)
        inputs[3] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = KEYEVENTF_KEYUP } }
        };

        SendInput((uint)inputs.Length, inputs, INPUT.Size);

        // Wait a bit and restore clipboard
        await Task.Delay(300);
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (oldText != null)
            {
                Clipboard.SetText(oldText);
            }
            else
            {
                Clipboard.Clear();
            }
        });
    }

    const ushort VK_C = 0x43;

    public static async Task<string> CopyTextFromWindowAsync(IntPtr targetWindow)
    {
        string copiedText = string.Empty;

        // Bring target window to foreground
        SetForegroundWindow(targetWindow);

        // Wait a little bit for the window to gain focus
        await Task.Delay(300);

        // Clear clipboard first
        Application.Current.Dispatcher.Invoke(() =>
        {
            Clipboard.Clear();
        });

        // Send Ctrl (Down)
        var inputs = new INPUT[4];
        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = 0 } }
        };

        // Send C (Down)
        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_C, dwFlags = 0 } }
        };

        // Send C (Up)
        inputs[2] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_C, dwFlags = KEYEVENTF_KEYUP } }
        };

        // Send Ctrl (Up)
        inputs[3] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = KEYEVENTF_KEYUP } }
        };

        SendInput((uint)inputs.Length, inputs, INPUT.Size);

        // Wait a bit for clipboard to be updated by the OS
        await Task.Delay(500);

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Clipboard.ContainsText())
            {
                copiedText = Clipboard.GetText();
            }
        });

        return copiedText;
    }
}
