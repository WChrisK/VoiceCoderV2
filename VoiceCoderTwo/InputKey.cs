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
        Up
    }

    public class InputKeyEvent
    {
        public readonly InputKey Key;
        public readonly bool Press;
        public readonly bool Release;

        public InputKeyEvent(InputKey key, bool press = false, bool release = false)
        {
            Key = key;
            Press = press;
            Release = release;
        }

        public override string ToString() => $"{Key} (press = {Press}, release = {Release})";
    }

    public static class InputKeyHelper
    {
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
                _ => null
            };
        }
    }
}
