using System;
using System.Collections.Generic;
using System.Speech.Recognition;
using Newtonsoft.Json.Linq;

namespace VoiceCoderTwo.Definitions
{
    public class Command
    {
        public readonly Mode Mode;
        public readonly string Name = "";
        public readonly Action<string[]>? Function;
        public readonly List<object> ActionKeys = new List<object>();
        public readonly GrammarNode GrammarNode;

        public Command(Mode mode, JObject jObject)
        {
            Mode = mode;

            dynamic data = jObject;

            if (data.name != null)
                Name = data.name;

            string grammarStr = (string)data.grammar ?? throw new Exception($"Missing 'grammar' field");
            GrammarNode = GrammarParser.Parse(grammarStr) ?? throw new Exception($"Malformed grammar: {grammarStr}");

            if (data.function != null)
            {
                string functionName = (string)data.function;
                if (functionName != null)
                {
                    Function = Functions.GetCallable(functionName);
                    if (Function == null)
                        Console.WriteLine($"Could not fund function named: {functionName} (in: '{(Name != "" ? Name : grammarStr)})'");
                }
            }

            string actionStr = (string)data.action;
            if (actionStr == null)
            {
                if (Function == null)
                    throw new ParserException($"Function cannot be found and no action field found for (in: '{(Name != "" ? Name : grammarStr)})'");
            }
            else
                ActionKeys = CommandParser.Parse(actionStr) ?? throw new ParserException($"Malformed action: {actionStr}");

            Name ??= grammarStr;
        }

        public VCGrammar Compile()
        {
            GrammarBuilder builder = GrammarNode.Build();
            return new VCGrammar(Mode, this, builder);
        }
    }
}
