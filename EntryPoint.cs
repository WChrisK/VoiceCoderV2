using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VoiceCoderTwo.Definitions;

namespace VoiceCoderTwo
{
    public static class EntryPoint
    {
        public static bool HaltVoiceCoder;
        private static readonly SpeechRecognitionEngine sre = new SpeechRecognitionEngine();
        private static readonly Stack<Mode> loadedModes = new Stack<Mode>();
        private static Mode rootMode = null!;
        private static List<object> lastActionCommand = new List<object>();

        private static Mode currentMode => loadedModes.Count > 0 ? loadedModes.Peek() : rootMode;

        private static void RecursivelyAddModes(Mode mode, string path)
        {
            string lowerPath = path.ToLower();

            foreach (KeyValuePair<string, Mode> pair in mode.Modes)
                RecursivelyAddModes(pair.Value, $"{lowerPath} {pair.Key}");
        }

        private static void RecursivelyLoadModeGrammars(Mode mode)
        {
            foreach (VCGrammar grammar in mode.Grammar)
            {
                sre.LoadGrammar(grammar);
                grammar.Enabled = false;
            }

            foreach (Mode childMode in mode.Modes.Values)
                RecursivelyLoadModeGrammars(childMode);
        }

        public static void ExitMode()
        {
            if (ReferenceEquals(currentMode, rootMode))
            {
                Console.WriteLine("Cannot exit from global mode");
                return;
            }

            DisableSreGrammarFor(currentMode);
            loadedModes.Pop();

            Console.WriteLine("Exited to mode: " + (currentMode.Name == "" ? "<global>" : currentMode.Name));
            EnableSreGrammarFor(currentMode);
        }

        public static void ChangeMode(string name)
        {
            if (!currentMode.Modes.TryGetValue(name, out Mode? mode))
            {
                Console.WriteLine($"Cannot find mode to change to: {name}");
                return;
            }

            Console.WriteLine($"Changed to mode: {name}");
            if (!ReferenceEquals(currentMode, rootMode))
                DisableSreGrammarFor(currentMode);
            loadedModes.Push(mode);
            EnableSreGrammarFor(mode);
        }

        private static void EnableSreGrammarFor(Mode mode)
        {
            foreach (VCGrammar grammar in mode.Grammar)
                grammar.Enabled = true;
        }

        private static void DisableSreGrammarFor(Mode mode)
        {
            foreach (VCGrammar grammar in mode.Grammar)
                grammar.Enabled = false;
        }

        public static void EmitLastActionKeys()
        {
            foreach (object actionKey in lastActionCommand)
            {
                switch (actionKey)
                {
                case string str:
                    StringBuilder strBuilder = new StringBuilder();
                    foreach (char c in str)
                    {
                        switch (c)
                        {
                        case '(':
                        case ')':
                        case '{':
                        case '}':
                        case '[':
                        case ']':
                        case '+':
                        case '^':
                        case '%':
                        case '~':
                            strBuilder.Append($"{{{c}}}");
                            break;
                        default:
                            strBuilder.Append(c);
                            break;
                        }
                    }
                    Native.EmitKeys(strBuilder.ToString());
                    break;

                case InputKey inputKey:
                    Native.EmitKeys(inputKey switch
                    {
                        InputKey.Tab => "{TAB}",
                        InputKey.Shift => "+",
                        InputKey.Control => "^",
                        InputKey.Alt => "%",
                        InputKey.Enter => "{ENTER}",
                        InputKey.Insert => "{INSERT}",
                        InputKey.Backspace => "{BACKSPACE}",
                        InputKey.Delete => "{DELETE}",
                        InputKey.Home => "{HOME}",
                        InputKey.End => "{END}",
                        InputKey.PageUp => "{PGUP}",
                        InputKey.PageDown => "{PGDN}",
                        InputKey.Left => "{LEFT}",
                        InputKey.Right => "{RIGHT}",
                        InputKey.Up => "{UP}",
                        InputKey.Down => "{DOWN}",
                        InputKey.Escape => "{ESCAPE}",
                        InputKey.F1 => "{F1}",
                        InputKey.F2 => "{F2}",
                        InputKey.F3 => "{F3}",
                        InputKey.F4 => "{F4}",
                        InputKey.F5 => "{F5}",
                        InputKey.F6 => "{F6}",
                        InputKey.F7 => "{F7}",
                        InputKey.F8 => "{F8}",
                        InputKey.F9 => "{F9}",
                        InputKey.F10 => "{F10}",
                        InputKey.F11 => "{F11}",
                        InputKey.F12 => "{F12}",
                        InputKey.CapsLock => "{CAPSLOCK}",
                        _ => throw new Exception($"Unknown input key: {inputKey}")
                    });

                    break;

                case int delayMs:
                    if (delayMs > 0)
                        Thread.Sleep(delayMs);
                    break;

                default:
                    throw new Exception($"Unsupported action key: {actionKey} {actionKey.GetType().FullName}");
                }
            }
        }

        private static void SendActionKeys(List<object> actionKeys)
        {
            lastActionCommand = actionKeys;
            EmitLastActionKeys();
        }

        private static void Speech_HandleHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (HaltVoiceCoder)
                return;

            Console.WriteLine("[Hypothesized] " + e.Result.Text);
        }

        private static void Speech_HandleRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (HaltVoiceCoder && !e.Result.Text.Equals("voice code start"))
                return;

            Console.WriteLine("[Recognized text] " + e.Result.Text);
            VCGrammar grammar = (VCGrammar)e.Result.Grammar;
            Command command = grammar.Command;
            Console.WriteLine(command.Name != null ? $">>> {command.Name}" : ">>> <unnamed>");

            if (command.ActionKeys.Count > 0)
                SendActionKeys(command.ActionKeys);

            string[] lowerText = e.Result.Text.Split(' ').Select(s => s.ToLower()).ToArray();
            command?.Function?.Invoke(lowerText);
        }

        private static void Speech_HandleRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (HaltVoiceCoder)
                return;

            Console.WriteLine($"[Rejected] {e.Result.Text}");
        }

        public static void LoadDefinitions(string path)
        {
            string text = File.ReadAllText(path);
            object data = JsonConvert.DeserializeObject(text) ?? throw new NullReferenceException($"Unable to read definitions at: {path}");

            rootMode = new Mode("", (JObject)data);
            RecursivelyAddModes(rootMode, "");

            // Load all the stuff so they're ready to be toggled, but only make
            // sure the root is ready to go from the start.
            RecursivelyLoadModeGrammars(rootMode);
            EnableSreGrammarFor(rootMode);
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: <definition file> <debug testing phrases...>");
                Console.WriteLine("    <definition file>:");
                Console.WriteLine("        The path to the definition file.");
                Console.WriteLine("        The source code has an 'Examples/' folder that has");
                Console.WriteLine("        examples (and fully working ones!) to use/build on.");
                Console.WriteLine("    <debug testing phrases>:");
                Console.WriteLine("        A series of debug phrases to run, testing whether");
                Console.WriteLine("        the definitions do as desired. These should ideally");
                Console.WriteLine("        be in quotes so that multiple words can be used at");
                Console.WriteLine("        once. This should be blank if not testing.");
                Console.WriteLine("        Example: ./vc.exe \"change mode program\" \"dot\" \"dot\"");
                return;
            }

            LoadDefinitions(args[0]);

            sre.SpeechHypothesized += Speech_HandleHypothesized;
            sre.SpeechRecognized += Speech_HandleRecognized;
            sre.SpeechRecognitionRejected += Speech_HandleRejected;
            sre.SetInputToDefaultAudioDevice();

            if (args.Length <= 1)
            {
                sre.RecognizeAsync(RecognizeMode.Multiple);
                Console.ReadLine();
                sre.RecognizeAsyncStop();
            }
            else
            {
                // Specifically for debugging.
                foreach (string arg in args.Skip(1))
                    sre.EmulateRecognize(arg);
            }

            sre.Dispose();
        }
    }
}
