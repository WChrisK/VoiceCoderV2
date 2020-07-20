using System.Collections.Generic;
using System.Text;

namespace VoiceCoderTwo.Definitions
{
    public class CommandParser
    {
        private readonly string text;
        private readonly List<object> elements = new List<object>();

        private CommandParser(string text)
        {
            this.text = text;
        }

        private List<object>? Parse()
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                switch (c)
                {
                case '`':
                    i++;
                    ConsumeText(ref i);
                    break;
                case '+':
                case '-':
                    i++;
                    ConsumeHeldOrReleaseKey(ref i, c);
                    break;
                case '<':
                    i++;
                    ConsumeMouseCoordinate(ref i);
                    break;
                case ' ':
                case '\t':
                    continue;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    elements.Add(ConsumeNumber(ref i));
                    break;
                default:
                    elements.Add(ConsumeKey(ref i));
                    break;
                }
            }

            return elements;
        }

        private void ConsumeText(ref int index)
        {
            StringBuilder builder = new StringBuilder();

            while (true)
            {
                char c = text[index];

                if (c == '`')
                {
                    string characters = builder.ToString();
                    if (characters.Length > 0)
                        elements.Add(characters);

                    index++;
                    break;
                }

                builder.Append(c);
                index++;

                if (index >= text.Length)
                    throw new ParserException("Missing closing ` in action");
            }
        }

        private void ConsumeHeldOrReleaseKey(ref int i, char c)
        {
            InputKey inputKey = ConsumeKey(ref i);
            InputKeyEvent inputKeyEvent = new InputKeyEvent(inputKey, c == '+', c == '-');
            elements.Add(inputKeyEvent);
        }

        private void ConsumeMouseCoordinate(ref int i)
        {
            int x = ConsumeNumber(ref i);
            ConsumeSpacesIfAny(ref i);

            if (text[i] != ',')
                throw new ParserException("Expected comma after mouse X coordinate");
            i++;

            ConsumeSpacesIfAny(ref i);
            int y = ConsumeNumber(ref i);

            if (text[i] != '>')
                throw new ParserException("Expected closing angle bracket immediately after mouse Y coordinate");
            i++;

            elements.Add(new Coordinate(x, y));
        }

        private void ConsumeSpacesIfAny(ref int i)
        {
            while (i < text.Length)
            {
                if (text[i] == ' ' || text[i] == '\t')
                    i++;
                else
                    break;
            }
        }

        private InputKey ConsumeKey(ref int i)
        {
            StringBuilder builder = new StringBuilder();

            while (i < text.Length)
            {
                char c = text[i];
                if (!char.IsLetter(c))
                    break;

                builder.Append(c);
                i++;
            }

            string keyName = builder.ToString();
            return InputKeyHelper.ToKey(keyName) ?? throw new ParserException($"Unexpected key: {keyName}");
        }

        private int ConsumeNumber(ref int i)
        {
            StringBuilder numberStr = new StringBuilder();

            while (i < text.Length)
            {
                char c = text[i];

                if (!char.IsDigit(c))
                    break;

                numberStr.Append(c);
                i++;
            }

            // This should never fail since we're only ever adding 1+ numbers.
            int.TryParse(numberStr.ToString(), out int result);
            return result;
        }

        public static List<object>? Parse(string text)
        {
            return new CommandParser(text).Parse();
        }
    }
}
