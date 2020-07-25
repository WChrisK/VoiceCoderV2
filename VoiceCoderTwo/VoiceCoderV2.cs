using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
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
        public static readonly SpeechSynthesizer SpeechSynthesizer = new SpeechSynthesizer();
        public static readonly JoystickToMouse JoystickMouse = new JoystickToMouse(false);
        private static readonly InputSimulator inputSimulator = new InputSimulator();
        private static readonly SpeechRecognitionEngine sre = new SpeechRecognitionEngine();
        private static readonly Stack<Mode> loadedModes = new Stack<Mode>();
        private static readonly HashSet<Mode> pushedModes = new HashSet<Mode>();
        private static List<object> lastActionCommand = new List<object>();
        private static bool initialLoad = true;

        public static Mode RootMode { get; private set; } = null!;
        public static Mode CurrentMode => loadedModes.Count > 0 ? loadedModes.Peek() : RootMode;

        public static void EnterExclusiveMode(Mode mode)
        {
            if (pushedModes.Count > 0)
                throw new Exception("Trying to enter exclusive mode twice");

            pushedModes.Add(CurrentMode);
            if (!ReferenceEquals(CurrentMode, RootMode))
                pushedModes.Add(RootMode);

            foreach (Mode pushedMode in pushedModes)
                DisableSreGrammarFor(pushedMode);
            EnableSreGrammarFor(mode);
        }

        public static void ExitExclusiveMode(Mode mode)
        {
            if (pushedModes.Count == 0)
                throw new Exception("Not in exclusive mode");

            foreach (Mode pushedMode in pushedModes)
                EnableSreGrammarFor(pushedMode);
            DisableSreGrammarFor(mode);

            pushedModes.Clear();
        }

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
            if (ReferenceEquals(CurrentMode, RootMode))
            {
                Console.WriteLine("Cannot exit from global mode");
                return;
            }

            DisableSreGrammarFor(CurrentMode);
            loadedModes.Pop();

            Console.WriteLine("Exited to mode: " + (CurrentMode.Name == "" ? "<global>" : CurrentMode.Name));
            EnableSreGrammarFor(CurrentMode);
        }

        public static void ChangeMode(string name)
        {
            if (!CurrentMode.Modes.TryGetValue(name, out Mode? mode))
            {
                Console.WriteLine($"Cannot find mode to change to: {name}");
                return;
            }

            Console.WriteLine($"Changed to mode: {name}");
            if (!ReferenceEquals(CurrentMode, RootMode))
                DisableSreGrammarFor(CurrentMode);
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

            SpeechSynthesizer.SpeakAsync("Unknown");

            Console.WriteLine($"[Rejected] {e.Result.Text}");
        }

        private static void SetInitialVoice()
        {
            foreach (InstalledVoice voice in SpeechSynthesizer.GetInstalledVoices())
            {
                VoiceInfo info = voice.VoiceInfo;
                if (info.Name.Contains("Microsoft Zira Desktop"))
                {
                    SpeechSynthesizer.SelectVoice(info.Name);
                    break;
                }
            }
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
                RootMode = new Mode("", null, (JObject)data);
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
                {
                    SpeechSynthesizer.SpeakAsync("Error loading definitions");
                    Environment.Exit(1);
                }

                Mode.Defines = previousDefines;
                Console.WriteLine("Rolled back to old definitions");
                return;
            }

            RecursivelyAddModes(RootMode, "");

            // Load all the stuff so they're ready to be toggled, but only make
            // sure the root is ready to go from the start.
            RecursivelyLoadModeGrammars(RootMode);
            EnableSreGrammarFor(RootMode);
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

            try
            {
                SetInitialVoice();
                DefinitionPath = args[0];
                LoadDefinitions(args[0]);
                initialLoad = false;

                sre.SpeechHypothesized += Speech_HandleHypothesized;
                sre.SpeechRecognized += Speech_HandleRecognized;
                sre.SpeechRecognitionRejected += Speech_HandleRejected;
                sre.SetInputToDefaultAudioDevice();

                if (args.Length <= 1)
                {
                    SpeechSynthesizer.Speak("Voice coder active");

                    sre.RecognizeAsync(RecognizeMode.Multiple);
                    Console.ReadLine();
                    sre.RecognizeAsyncStop();

                    SpeechSynthesizer.Speak("Goodbye");
                }
                else
                {
                    foreach (string arg in args.Skip(1))
                    {
                        sre.EmulateRecognize(arg);
                        Thread.Sleep(250);
                    }
                }
            }
            catch
            {
                SpeechSynthesizer.Speak("Unexpected exception, program terminating");
                throw;
            }
            finally
            {
                SpeechSynthesizer.Dispose();
                sre.Dispose();
            }
        }
    }
}
