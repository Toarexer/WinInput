using System;
using System.Runtime.InteropServices;

namespace WinAPI_Input;

public static partial class WinInput {
    /// <summary>
    /// Recreation of the MOUSEINPUT structure found in the winuser.h header.
    /// </summary>
    public struct Mouse {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Recreation of the KEYBDINPUT structure found in the winuser.h header.
    /// </summary>
    public struct Keyboard {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Recreation of the HARDWAREINPUT structure found in the winuser.h header.
    /// </summary>
    public struct Hardware {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    /// <summary>
    /// Recreation of the INPUT structure found in the winuser.h header.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Input {
        /// <summary>
        /// Type of the input. This can be INPUT_MOUSE, INPUT_KEYBOARD or INPUT_HARDWARE.
        /// </summary>
        [FieldOffset(0)] public Type type;

        /// <summary>
        /// Sets the values of the <see cref="Mouse"/> struct. Part of the union with <see cref="ki"/> and <see cref="hi"/>.
        /// </summary>
        [FieldOffset(8)] public Mouse mi;

        /// <summary>
        /// Sets the values of the <see cref="Keyboard"/> struct. Part of the union with <see cref="mi"/> and <see cref="hi"/>.
        /// </summary>
        [FieldOffset(8)] public Keyboard ki;

        /// <summary>
        /// Sets the values of the <see cref="Hardware"/> struct. Part of the union with <see cref="mi"/> and <see cref="ki"/>.
        /// </summary>
        [FieldOffset(8)] public Hardware hi;

        /// <summary>
        /// Returns the unmanaged size of the <see cref="Input"/> struct.
        /// </summary>
        public int Size => UnionSize;
    }

    /// <summary>
    /// Returns the unmanaged size of the <see cref="Input"/> struct.
    /// </summary>
    public static int UnionSize => Marshal.SizeOf<Input>();

    /// <summary>
    /// A collection of functions imported from user32.dll.
    /// </summary>
    public static class User32 {
        [DllImport("user32.dll")] extern public static short GetAsyncKeyState(VirtualKey vKey);

        [DllImport("user32.dll")] extern public static short VkKeyScanW(ushort ch);

        [DllImport("user32.dll")] extern public static IntPtr GetKeyboardLayout(int idThread = 0);

        [DllImport("user32.dll")] extern public static uint MapVirtualKeyExW(uint uCode, MapType uMapType, IntPtr dwhkl = (nint)0);

        [DllImport("user32.dll")] extern public static IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")] extern public static uint SendInput(uint cInputs, Input[] pInputs, int cbSize);
    }

    /// <summary>
    /// Checks if the key is pressed or not.
    /// </summary>
    public static bool IsKeyPressed(VirtualKey keycode) => (User32.GetAsyncKeyState(keycode) & 0x8000) != 0;

    /// <summary>
    /// Checks if the key was pressed or not.
    /// </summary>
    public static bool WasKeyPressed(VirtualKey keycode) => (User32.GetAsyncKeyState(keycode) & 0x1) != 0;

    /// <summary>
    /// Sets the state of the specified mapped <see cref="VirtualKey"/> to down.
    /// </summary>
    /// <returns>
    /// True if the key state was successfully set.
    /// </returns>
    public static bool SetKeyDown(VirtualKey keycode) {
        ushort code = (ushort)User32.MapVirtualKeyExW((uint)keycode, MapType.MAPVK_VK_TO_VSC_EX, User32.GetKeyboardLayout(0));
        Input input = new();

        input.type = Type.INPUT_KEYBOARD;
        input.ki.dwExtraInfo = User32.GetMessageExtraInfo();
        input.ki.dwFlags = (uint)(GetFlagIfExtendedKey(code) | KeyEvent.KEYEVENTF_SCANCODE);
        input.ki.time = 0;
        input.ki.wScan = code;

        return User32.SendInput(1, new[] { input }, UnionSize) > 0;
    }

    /// <summary>
    /// Sets the state of the specified mapped <see cref="VirtualKey"/> to up.
    /// </summary>
    /// <returns>
    /// True if the key state was successfully set.
    /// </returns>
    public static bool SetKeyUp(VirtualKey keycode) {
        ushort code = (ushort)User32.MapVirtualKeyExW((uint)keycode, MapType.MAPVK_VK_TO_VSC_EX, User32.GetKeyboardLayout(0));
        Input input = new();

        input.type = Type.INPUT_KEYBOARD;
        input.ki.dwExtraInfo = User32.GetMessageExtraInfo();
        input.ki.dwFlags = (uint)(GetFlagIfExtendedKey(code) | KeyEvent.KEYEVENTF_SCANCODE | KeyEvent.KEYEVENTF_KEYUP);
        input.ki.time = 0;
        input.ki.wScan = code;

        return User32.SendInput(1, new[] { input }, UnionSize) > 0;
    }

    /// <summary>
    /// Presses, then releases the specified mapped <see cref="VirtualKey"/>.
    /// </summary>
    /// <returns>
    /// True if the key state was successfully set to down and then up.
    /// </returns>
    public static bool PressKey(VirtualKey keycode) {
        ushort code = (ushort)User32.MapVirtualKeyExW((uint)keycode, MapType.MAPVK_VK_TO_VSC_EX, User32.GetKeyboardLayout(0));
        Input[] inputs = new Input[] { new(), new() };

        inputs[0].type = Type.INPUT_KEYBOARD;
        inputs[0].ki.dwExtraInfo = User32.GetMessageExtraInfo();
        inputs[0].ki.dwFlags = (uint)(GetFlagIfExtendedKey(code) | KeyEvent.KEYEVENTF_SCANCODE);
        inputs[0].ki.time = 0;
        inputs[0].ki.wScan = code;

        inputs[1].type = Type.INPUT_KEYBOARD;
        inputs[1].ki.dwExtraInfo = User32.GetMessageExtraInfo();
        inputs[1].ki.dwFlags = (uint)(GetFlagIfExtendedKey(code) | KeyEvent.KEYEVENTF_SCANCODE | KeyEvent.KEYEVENTF_KEYUP);
        inputs[1].ki.time = 0;
        inputs[1].ki.wScan = code;

        return User32.SendInput(2, inputs, UnionSize) == 2;
    }

    /// <summary>
    /// Presses, then releases the mapped <see cref="VirtualKey"/> specified by a <see cref="char"/>.
    /// </summary>
    /// <returns>
    /// True if the <see cref="char"/> was successfully mapped and the key state was set to down and then to up.
    /// </returns>
    public static bool PressKey(char c) {
        short key = User32.VkKeyScanW(c);
        if (key == (-1 | (-1 << 8)))
            return false;
        return PressKey((VirtualKey)key);
    }

    /// <summary>
    /// Check if the keycode is an extended key or not.
    /// </summary>
    /// <returns>
    /// <see cref="KeyEvent.KEYEVENTF_EXTENDEDKEY"/> if it is an extended key and 0 if not.
    /// </returns>
    static KeyEvent GetFlagIfExtendedKey(ushort code) => (code & 0xFF00) == 0xE000 || (code & 0xFF00) == 0xE100 ? KeyEvent.KEYEVENTF_EXTENDEDKEY : 0;

    /// <summary>
    /// Wrapper function for setting state of the mouse.
    /// </summary>
    /// <returns>
    /// True if the state was successfully set.
    /// </returns>
    public static bool SetMouseState(int x, int y, MouseEvent flags) {
        Input input = new();

        input.type = (uint)Type.INPUT_MOUSE;
        input.mi.dx = x;
        input.mi.dy = y;
        input.mi.mouseData = 0;
        input.mi.dwFlags = (uint)flags;
        input.mi.time = 0;
        input.mi.dwExtraInfo = User32.GetMessageExtraInfo();

        return User32.SendInput(1, new[] { input }, UnionSize) > 0;
    }

    // Maybe add proper wrapper functions for the mouse later.
    // ...
}
