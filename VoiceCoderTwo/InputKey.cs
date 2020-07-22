using System;
using WindowsInput.Native;

namespace VoiceCoderTwo
{
    public enum InputKey
    {
        Alt,
        Backspace,
        CapsLock,
        Control,
        Delete,
        Down,
        End,
        Enter,
        Escape,
        F1,
        F2,
        F3,
        F4,
        F5,
        F6,
        F7,
        F8,
        F9,
        F10,
        F11,
        F12,
        Home,
        Insert,
        Left,
        PageDown,
        PageUp,
        Right,
        Shift,
        Tab,
        Up,
        Windows
    }

    public class InputKeyEvent
    {
        public InputKey Key;
        public bool Down;
        public bool Up;

        public InputKeyEvent(InputKey key, bool down = false, bool up = false)
        {
            Key = key;
            Down = down;
            Up = up;
        }

        public VirtualKeyCode ToVirtualKeyCode()
        {
            return Key switch
            {
                InputKey.Alt => VirtualKeyCode.MENU,
                InputKey.Backspace => VirtualKeyCode.BACK,
                InputKey.CapsLock => VirtualKeyCode.CAPITAL,
                InputKey.Control => VirtualKeyCode.CONTROL,
                InputKey.Delete => VirtualKeyCode.DELETE,
                InputKey.Down => VirtualKeyCode.DOWN,
                InputKey.End => VirtualKeyCode.END,
                InputKey.Enter => VirtualKeyCode.RETURN,
                InputKey.Escape => VirtualKeyCode.ESCAPE,
                InputKey.F1 => VirtualKeyCode.F1,
                InputKey.F2 => VirtualKeyCode.F2,
                InputKey.F3 => VirtualKeyCode.F3,
                InputKey.F4 => VirtualKeyCode.F4,
                InputKey.F5 => VirtualKeyCode.F5,
                InputKey.F6 => VirtualKeyCode.F6,
                InputKey.F7 => VirtualKeyCode.F7,
                InputKey.F8 => VirtualKeyCode.F8,
                InputKey.F9 => VirtualKeyCode.F9,
                InputKey.F10 => VirtualKeyCode.F10,
                InputKey.F11 => VirtualKeyCode.F11,
                InputKey.F12 => VirtualKeyCode.F12,
                InputKey.Home => VirtualKeyCode.HOME,
                InputKey.Insert => VirtualKeyCode.INSERT,
                InputKey.Left => VirtualKeyCode.LEFT,
                InputKey.PageDown => VirtualKeyCode.NEXT,
                InputKey.PageUp => VirtualKeyCode.PRIOR,
                InputKey.Right => VirtualKeyCode.RIGHT,
                InputKey.Shift => VirtualKeyCode.SHIFT,
                InputKey.Tab => VirtualKeyCode.TAB,
                InputKey.Up => VirtualKeyCode.UP,
                InputKey.Windows => VirtualKeyCode.LWIN,
                _ => throw new Exception($"Unsupported input key type: {Key}")
            };
        }

        public override string ToString() => $"{Key} (press = {Down}, release = {Up})";
    }

    public static class InputKeyHelper
    {
        public static InputKeyEvent ToEvent(this InputKey key)
        {
            bool defaultDown = (key == InputKey.Alt || key == InputKey.Control || key == InputKey.Shift);
            return new InputKeyEvent(key, defaultDown);
        }

        public static InputKey? ToKey(string text)
        {
            return text.ToLower() switch
            {
                "alt" => InputKey.Alt,
                "back" => InputKey.Backspace,
                "backspace" => InputKey.Backspace,
                "bksp" => InputKey.Backspace,
                "capslock" => InputKey.CapsLock,
                "control" => InputKey.Control,
                "ctrl" => InputKey.Control,
                "del" => InputKey.Delete,
                "delete" => InputKey.Delete,
                "down" => InputKey.Down,
                "end" => InputKey.End,
                "enter" => InputKey.Enter,
                "esc" => InputKey.Escape,
                "escape" => InputKey.Escape,
                "f1" => InputKey.F1,
                "f2" => InputKey.F2,
                "f3" => InputKey.F3,
                "f4" => InputKey.F4,
                "f5" => InputKey.F5,
                "f6" => InputKey.F6,
                "f7" => InputKey.F7,
                "f8" => InputKey.F8,
                "f9" => InputKey.F9,
                "f10" => InputKey.F10,
                "f11" => InputKey.F11,
                "f12" => InputKey.F12,
                "home" => InputKey.Home,
                "insert" => InputKey.Insert,
                "left" => InputKey.Left,
                "pagedown" => InputKey.PageDown,
                "pgdn" => InputKey.PageDown,
                "pageup" => InputKey.PageUp,
                "pgup" => InputKey.PageUp,
                "right" => InputKey.Right,
                "shift" => InputKey.Shift,
                "tab" => InputKey.Tab,
                "up" => InputKey.Up,
                "windows" => InputKey.Windows,
                _ => null
            };
        }
    }
}
