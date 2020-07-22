using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VoiceCoderTwo.Definitions;

namespace VoiceCoderTwo
{
    public static class VoiceCoderV2
    {
        public static bool HaltVoiceCoder;
        public static string DefinitionPath = "definitions.json";
        private static readonly InputSimulator inputSimulator = new InputSimulator();
        private static readonly SpeechRecognitionEngine sre = new SpeechRecognitionEngine();
        private static readonly Stack<Mode> loadedModes = new Stack<Mode>();
        private static Mode rootMode = null!;
        private static List<object> lastActionCommand = new List<object>();
        private static bool initialLoad = true;

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
            HashSet<VirtualKeyCode> keysDown = new HashSet<VirtualKeyCode>();
            StringBuilder sendKeysString = new StringBuilder();

            foreach (object actionKey in lastActionCommand)
            {
                switch (actionKey)
                {
                case string str:
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
                            sendKeysString.Append($"{{{c}}}");
                            break;
                        default:
                            sendKeysString.Append(c);
                            break;
                        }
                    }
                    break;

                case InputKeyEvent inputKeyEvent:
                    VirtualKeyCode virtualKeyCode = inputKeyEvent.ToVirtualKeyCode();
                    if (inputKeyEvent.Down)
                    {
                        keysDown.Add(virtualKeyCode);
                        inputSimulator.Keyboard.KeyDown(virtualKeyCode);
                    }
                    else if (inputKeyEvent.Up)
                    {
                        keysDown.Remove(virtualKeyCode);
                        inputSimulator.Keyboard.KeyUp(virtualKeyCode);
                    }
                    else
                        inputSimulator.Keyboard.KeyPress(virtualKeyCode);
                    break;

                case MouseAction mouseAction:
                    mouseAction.Execute();
                    break;

                case int delayMs:
                    DeployAndClearSendKeysBuffer();
                    if (delayMs > 0)
                        Thread.Sleep(delayMs);
                    break;

                default:
                    throw new Exception($"Unsupported action key: {actionKey} {actionKey.GetType().FullName}");
                }
            }

            DeployAndClearSendKeysBuffer();

            // Commands are allowed to have a key stay down to prevent the need
            // of typing -Key for every +Key, so we release them for any that
            // are left down.
            foreach (VirtualKeyCode keyStillDown in keysDown)
                inputSimulator.Keyboard.KeyUp(keyStillDown);

            void DeployAndClearSendKeysBuffer()
            {
                if (sendKeysString.Length == 0)
                    return;

                Native.EmitKeys(sendKeysString.ToString());
                sendKeysString.Clear();
            }
        }

        public static void SendActionKeys(List<object> actionKeys)
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
            Console.WriteLine($">>> {command.Name}");

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
            Console.WriteLine($"Loading definitions from: {path}");
            Dictionary<string, GrammarNode> previousDefines = Mode.Defines;

            try
            {
                string text = File.ReadAllText(path);
                object data = JsonConvert.DeserializeObject(text) ?? throw new NullReferenceException($"Unable to read definitions at: {path}");

                Mode.Defines = Mode.CreateDefaultDefines();
                rootMode = new Mode("", null, (JObject)data);
                loadedModes.Clear();
                lastActionCommand.Clear();
                sre.UnloadAllGrammars();
            }
            catch (Exception e)
            {
                Console.WriteLine("Your definitions are malformed, program execution cannot continue.");
                Console.WriteLine("Resolve the syntax errors or missing functions and re-run.");
                Console.WriteLine($"Reason: {e.Message}");

                if (initialLoad)
                    Environment.Exit(1);

                Mode.Defines = previousDefines;
                Console.WriteLine("Rolled back to old definitions");
                return;
            }

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

            DefinitionPath = args[0];
            LoadDefinitions(args[0]);
            initialLoad = false;

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
                foreach (string arg in args.Skip(1))
                {
                    sre.EmulateRecognize(arg);
                    Thread.Sleep(250);
                }
            }

            sre.Dispose();
        }
    }
}
