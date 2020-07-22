using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using static VoiceCoderTwo.FunctionsHelper;

namespace VoiceCoderTwo
{
    /// <summary>
    /// A list of functions that can be called. These should all be public and
    /// static to let reflection find them.
    /// </summary>
    public static class Functions
    {
        #region Core

        public static void VoiceCodeMute(string[] words)
        {
            VoiceCoderV2.HaltVoiceCoder = words[2] != "start";
        }

        public static void ChangeMode(string[] words)
        {
            VoiceCoderV2.ChangeMode(words[2]);
        }

        public static void ExitMode(string[] words)
        {
            VoiceCoderV2.ExitMode();
        }

        public static void SwitchSiblingMode(string[] words)
        {
            ExitMode(words);
            ChangeMode(words);
        }

        public static void RepeatAction(string[] words)
        {
            int? start = FindTrailingNumberStartIndex(words);
            if (start == null)
                return;

            // We repeat minus one because we already did the action once.
            int repeatAmount = ReadNumber(words, start.Value, out _);
            for (int repeat = 0; repeat < repeatAmount - 1; repeat++)
                VoiceCoderV2.EmitLastActionKeys();
        }

        #endregion

        #region Mouse

        public static void MouseMove(string[] words)
        {
            bool up = words[1] == "up";
            bool down = words[1] == "down";
            bool right = (words[1] == "right") || (words.Length > 1 && words[2] == "right");
            bool left = (words[1] == "left") || (words.Length > 1 && words[2] == "left");
            bool hadTwoDirections = (up || down) && (left || right);

            int amount = hadTwoDirections ?
                ReadNumber(words, 3, out _) :
                ReadNumber(words, 2, out _);

            if (up || down)
            {
                if (left)
                    Native.MoveMouseOffset(-amount, down ? amount : -amount);
                else if (right)
                    Native.MoveMouseOffset(amount, down ? amount : -amount);
                else
                    Native.MoveMouseOffset(0, down ? amount : -amount);
            }
            else if (left)
                Native.MoveMouseOffset(-amount, 0);
            else
                Native.MoveMouseOffset(amount, 0);
        }

        public static void MouseClick(string[] words)
        {
            if (words.Length > 1)
            {
                if (words[1] == "right")
                    Native.DoMouseClick(false);
                else
                {
                    Native.DoMouseClick(true);
                    Thread.Sleep(100);
                    Native.DoMouseClick(true);
                }
            }
            else
                Native.DoMouseClick(true);
        }

        public static void MouseGrid(string[] words)
        {
            Rectangle box = Native.GetScreenDimensions();

            foreach (string section in words.Skip(1))
            {
                int halfX = box.Width / 2;
                int halfY = box.Height / 2;

                box = section.ToLower() switch
                {
                    "one" => new Rectangle(box.X, box.Y, halfX, halfY),
                    "two" => new Rectangle(box.X + halfX, box.Y, halfX, halfY),
                    "three" => new Rectangle(box.X, box.Y + halfY, halfX, halfY),
                    "four" => new Rectangle(box.X + halfX, box.Y + halfY, halfX, halfY),
                    _ => throw new Exception($"Unknown mouse grid section: {section}")
                };
            }

            int centerX = box.X + (box.Width / 2);
            int centerY = box.Y + (box.Height / 2);
            Native.MoveMouseAbsolute(centerX, centerY);
        }

        public static void PrintMouseCoordinates(string[] words)
        {
            Point point = Native.GetCursorPosition();
            Console.WriteLine($"<{point.X}, {point.Y}>");
        }

        #endregion

        #region Navigation

        public static void GoToLine(string[] words)
        {
            int lineNumber = ReadNumber(words, 3, out _);
            Native.EmitKeys($"^g");
            Thread.Sleep(500);
            Native.EmitKeys($"{lineNumber}{{ENTER}}");
        }

        #endregion

        #region Code

        public static void SymbolEmit(string[] words)
        {
            if (words.Length == 3)
            {
                Native.EmitKeys(words[2] == "and" ? "&" : "|");
                return;
            }

            Native.EmitKeys(words[1] switch
            {
                "array" => "{[}{]}",
                "index" => "{[}{]}{LEFT}",
                "generic" => "<>{LEFT}",
                "paren" => "{(}{)}",
                "braces" => "{{}{}}",
                "and" => "&&",
                "or" => "||",
                "not" => "!",
                "complement" => "{~}",
                "plus" => "{+}",
                "minus" => "{-}",
                "star" => "*",
                "divide" => "/",
                "comma" => ",",
                "percent" => "{%}",
                "backslash" => "\\",
                "colon" => ":",
                "pipe" => "|",
                "caret" => "^",
                _ => throw new Exception($"Unsupported symbol: {words[0]}")
            });
        }

        public static void EmitKeyword(string[] words)
        {
            Native.EmitKeys(words[1]);
        }

        public static void CreateNewFieldOrMethod(string[] words)
        {
            Native.EmitKeys(string.Join(" ", words.Skip(1)) + " ");
        }

        public static void ChooseOffset(string[] words)
        {
            int arrows = ReadNumber(words, 1, out _);
            for (int i = 0; i < arrows; i++)
                Native.EmitKeys("{DOWN}");
            Native.EmitKeys("{ENTER}");
        }

        public static void EmitStringLiteral(string[] words)
        {
            Native.EmitKeys("\"\"{LEFT}");
        }

        #endregion

        public static Action<string[]>? GetCallable(string funcName)
        {
            try
            {
                List<Type> types = Assembly.GetExecutingAssembly().GetTypes().Where(c => c.Name == nameof(Functions)).ToList();
                foreach (Type type in types)
                {
                    MethodInfo info = type.GetMethod(funcName, BindingFlags.Public | BindingFlags.Static);
                    if (info != null && info.Name != nameof(GetCallable))
                        return args => info.Invoke(null, new object[] { args });
                }
            }
            catch
            {
                // Want to fail silently.
            }

            return null;
        }
    }
}
