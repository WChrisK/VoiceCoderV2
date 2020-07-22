using System;
using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;

namespace VoiceCoderTwo.Definitions
{
    public class Mode
    {
        private const string DictationVariableName = "dictate";
        private const string WildcardVariableName = "wildcard";
        public static Dictionary<string, GrammarNode> Defines = CreateDefaultDefines();

        public readonly string Name;
        public readonly Mode? Parent;
        public readonly List<Command> Commands = new List<Command>();
        public readonly Dictionary<string, Mode> Modes = new Dictionary<string, Mode>();
        public readonly List<VCGrammar> Grammar = new List<VCGrammar>();

        public Mode(string name, Mode? parent, JObject jsonData)
        {
            Name = name.ToLower();
            Parent = parent;

            dynamic data = jsonData!;

            try
            {
                if (data.defines != null)
                {
                    foreach (KeyValuePair<string, JToken?> entry in (JObject)data.defines)
                    {
                        string defineNameLower = entry.Key.ToLower();
                        if (Defines.ContainsKey(defineNameLower))
                            Console.WriteLine($"Error: Overwriting define {entry.Key}");
                        if (defineNameLower == DictationVariableName)
                            throw new ParserException($"Not allowed to redefine {DictationVariableName}");

                        GrammarNode? node = GrammarParser.Parse((string)entry.Value!);
                        Defines[defineNameLower] = node ?? throw new ParserException($"Expected text for grammar action (is it an empty string?) for: '{defineNameLower}'");
                    }
                }
            }
            catch (RuntimeBinderException)
            {
            }

            try
            {
                if (data.commands != null)
                {
                    foreach (var element in (JArray)data.commands)
                    {
                        Command command = new Command(this, (JObject)element);
                        Commands.Add(command);
                    }
                }
            }
            catch (RuntimeBinderException)
            {
            }

            try
            {
                if (data.modes != null)
                    foreach (KeyValuePair<string, JToken?> entry in (JObject)data.modes)
                        Modes[entry.Key] = new Mode(entry.Key, this, (JObject)entry.Value!);
            }
            catch (RuntimeBinderException)
            {
            }

            foreach (Command command in Commands)
                Grammar.Add(command.Compile());
        }

        public static Dictionary<string, GrammarNode> CreateDefaultDefines()
        {
            return new Dictionary<string, GrammarNode>
            {
                [DictationVariableName] = new GrammarNode { IsDictation = true },
                [WildcardVariableName] = new GrammarNode { IsWildcard = true }
            };
        }

        public override string ToString() => Name;
    }
}
