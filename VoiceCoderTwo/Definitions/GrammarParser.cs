using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceCoderTwo.Definitions
{
    public class GrammarParser
    {
        private readonly List<string> tokens = new List<string>();
        private int index;

        private bool HasTokensLeft => index < tokens.Count;

        private GrammarParser(string text)
        {
            StringBuilder builder = new StringBuilder();

            foreach (char c in text)
            {
                switch (c)
                {
                case '\0':
                case '\r':
                case '\n':
                    throw new ParserException($"Unexpected character '{c}' when parsing grammar");
                case ' ':
                case '\t':
                    AddIfBuildingAndClear();
                    break;
                case '[':
                case ']':
                case '(':
                case ')':
                case '+':
                case '*':
                case '|':
                case '$':
                    AddIfBuildingAndClear();
                    tokens.Add(c.ToString());
                    break;
                default:
                    builder.Append(c);
                    continue;
                }
            }

            AddIfBuildingAndClear();

            void AddIfBuildingAndClear()
            {
                if (builder.Length <= 0)
                    return;

                tokens.Add(builder.ToString());
                builder.Clear();
            }
        }

        private ParserException CreateAndLogException(string reason)
        {
            StringBuilder tokenBuilder = new StringBuilder();
            StringBuilder arrowBuilder = new StringBuilder();

            for (int i = 0; i < tokens.Count; i++)
            {
                tokenBuilder.Append($"{tokens[i]} ");

                if (i == index)
                    arrowBuilder.Append("^");
                else if (i < index)
                {
                    for (int spaces = 0; spaces < tokens[i].Length; spaces++)
                        arrowBuilder.Append(' ');
                    arrowBuilder.Append(' ');
                }
            }

            Console.WriteLine($"Parsing failure: {reason}");
            Console.WriteLine(tokenBuilder.ToString());
            Console.WriteLine(arrowBuilder.ToString());

            return new ParserException(reason);
        }

        private GrammarNode ConsumeSequence()
        {
            List<GrammarNode> nodes = new List<GrammarNode>();

            while (HasTokensLeft)
            {
                string token = tokens[index];

                switch (token[0])
                {
                case '[':
                    index++;
                    nodes.Add(ConsumeOptionalChoices());
                    break;
                case '(':
                    index++;
                    nodes.Add(ConsumeChoices());
                    break;
                case ']':
                case ')':
                case '|':
                    goto ExitSwitch;
                case '+':
                    throw CreateAndLogException("Found + operator that does not come after an element");
                case '*':
                    throw CreateAndLogException("Found * operator that does not come after an element");
                case '$':
                    index++;
                    nodes.Add(LookupDefinition());
                    break;
                default:
                    nodes.Add(ConsumeWord());
                    break;
                }
            }

            ExitSwitch:
            if (nodes.Count == 0)
                throw new ParserException("Cannot have an empty string, choice, or sequence");

            for (int i = 1; i < nodes.Count; i++)
                nodes[i - 1].Next = nodes[i];

            return nodes[0];
        }

        private GrammarNode ConsumeWord()
        {
            GrammarNode node = new GrammarNode { Text = tokens[index] };
            index++;

            if (HasTokensLeft)
            {
                switch (tokens[index])
                {
                case "+":
                    node.RepeatMax = int.MaxValue;
                    index++;
                    break;
                case "*":
                    node.RepeatMin = 0;
                    node.RepeatMax = int.MaxValue;
                    index++;
                    break;
                }
            }

            return node;
        }

        private GrammarNode ConsumeOptionalChoices()
        {
            GrammarNode choices = new GrammarNode { RepeatMin = 0, RepeatMax = 1 };

            if (HasTokensLeft && tokens[index] == "|")
                throw CreateAndLogException("Should not start choices with a '|'");

            while (HasTokensLeft)
            {
                string token = tokens[index];

                if (token == "]")
                {
                    index++;
                    break;
                }

                // The way this is set up, if we run into the option delimiter
                // then we just skip it (because it's expected). This way, when
                // we consume a sequence, anything else will cause it to error
                // out. As such, we remove the only character for a valid grammar
                // that would cause it to error out below, which is what we want.
                if (token == "|")
                    index++;

                GrammarNode seq = ConsumeSequence();
                choices.Children.Add(seq);
            }

            if (HasTokensLeft && (tokens[index] == "+" || tokens[index] == "*"))
                throw CreateAndLogException("Should not be applying + or * to an optional choices");

            return choices;
        }

        private GrammarNode ConsumeChoices()
        {
            GrammarNode choices = new GrammarNode();

            if (HasTokensLeft && tokens[index] == "|")
                throw CreateAndLogException("Should not start choices with a '|'");

            while (HasTokensLeft)
            {
                string token = tokens[index];

                if (token == ")")
                {
                    index++;
                    break;
                }

                if (token == "|")
                    index++;

                GrammarNode seq = ConsumeSequence();
                choices.Children.Add(seq);
            }

            if (HasTokensLeft)
            {
                switch (tokens[index])
                {
                case "+":
                    choices.RepeatMin = 1;
                    choices.RepeatMax = 1000;
                    index++;
                    break;
                case "*":
                    choices.RepeatMin = 0;
                    choices.RepeatMax = 1000;
                    index++;
                    break;
                }
            }

            return choices;
        }

        private GrammarNode LookupDefinition()
        {
            if (!HasTokensLeft)
                throw CreateAndLogException("The '$' identifier has no variable name after it to lookup");

            string lowerName = tokens[index].ToLower();
            if (!Mode.Defines.ContainsKey(lowerName))
                throw CreateAndLogException("Cannot lookup defined variable");
            index++;

            GrammarNode node = new GrammarNode(Mode.Defines[lowerName]);

            if (HasTokensLeft)
            {
                switch (tokens[index])
                {
                case "+":
                    if (node.RepeatMin != 1 || node.RepeatMax != 1)
                        throw CreateAndLogException("Cannot apply + or * to a variable that unpacks to an optional");
                    node.RepeatMin = 1;
                    node.RepeatMax = int.MaxValue;
                    index++;
                    break;
                case "*":
                    if (node.RepeatMin != 1 || node.RepeatMax != 1)
                        throw CreateAndLogException("Cannot apply + or * to a variable that unpacks to an optional");
                    node.RepeatMin = 0;
                    node.RepeatMax = int.MaxValue;
                    index++;
                    break;
                }
            }

            return node;
        }

        private GrammarNode? Parse()
        {
            if (tokens.Count == 0)
                return null;

            GrammarNode node = ConsumeSequence();
            return HasTokensLeft ? null : node;
        }

        public static GrammarNode? Parse(string text)
        {
            return new GrammarParser(text).Parse();
        }
    }
}
