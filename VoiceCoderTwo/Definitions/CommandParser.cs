using System.Collections.Generic;
using System.Linq;
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
                case '{':
                    i++;
                    ConsumeRepeat(ref i);
                    break;
                case '<':
                    i++;
                    ConsumeMouse(ref i);
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
                    // Number consumption overshoots by one, correct for that.
                    i--;
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
                    throw new ParserException($"Missing closing ` in action (line: \"{text}\")");
            }
        }

        private void ConsumeHeldOrReleaseKey(ref int i, char c)
        {
            InputKeyEvent inputKeyEvent = ConsumeKey(ref i);
            inputKeyEvent.Down = c == '+';
            inputKeyEvent.Up = c == '-';
            elements.Add(inputKeyEvent);
        }

        private void ConsumeRepeat(ref int i)
        {
            int repeat = ConsumeNumber(ref i);
            if (text[i] != '}')
                throw new ParserException($"Expected closing }} after repeat number (line: \"{text}\")");
            i++;

            // We go to `repeat - 1` because it's already in there once.
            object elementToRepeat = elements.Last();
            for (int times = 0; times < repeat - 1; times++)
                elements.Add(elementToRepeat);
        }

        private void ConsumeMouse(ref int i)
        {
            int? x = null;
            int? y = null;
            MouseClick? clickType = null;

            // Would be nice to clean up this abomination at some point.
            char? xType = ConsumeMouseRelativeIfPresent(ref i);
            if (text[i] != ',')
            {
                x = ConsumeNumber(ref i);
                if (xType != null && xType.Value == '-')
                    x = -x;
            }
            ConsumeSpacesIfAny(ref i);

            if (text[i] != ',')
                throw new ParserException($"Expected comma after mouse X coordinate (line: \"{text}\")");
            i++;

            ConsumeSpacesIfAny(ref i);
            char? yType = ConsumeMouseRelativeIfPresent(ref i);
            if (text[i] != ',')
            {
                y = ConsumeNumber(ref i);
                if (yType != null && yType.Value == '-')
                    y = -y;
            }

            ConsumeSpacesIfAny(ref i);
            if (text[i] == ',')
            {
                i++;
                ConsumeSpacesIfAny(ref i);
                clickType = ConsumeClickType(ref i);
            }

            if (text[i] != '>')
                throw new ParserException($"Expected closing angle bracket immediately after mouse Y coordinate (line: \"{text}\")");

            MouseAction mouseAction = new MouseAction
            {
                X = x,
                Y = y,
                AbsoluteX = xType == null,
                AbsoluteY = yType == null,
                MouseClick = clickType
            };

            elements.Add(mouseAction);
        }

        private MouseClick ConsumeClickType(ref int i)
        {
            if (i >= text.Length)
                throw new ParserException("Expecting click type, but ran out of tokens");

            char clickLetter = text[i];
            i++;

            return char.ToUpper(clickLetter) switch
            {
                'L' => MouseClick.Left,
                'M' => MouseClick.Middle,
                'R' => MouseClick.Right,
                _ => throw new ParserException($"Unexpected mouse click character: {clickLetter}")
            };
        }

        private char? ConsumeMouseRelativeIfPresent(ref int i)
        {
            if (i >= text.Length)
                return null;

            char c = text[i];
            if (c != '+' && c != '-')
                return null;

            i++;
            return c;
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

        private InputKeyEvent ConsumeKey(ref int i)
        {
            StringBuilder builder = new StringBuilder();

            while (i < text.Length)
            {
                char c = text[i];

                // We have to support things like F12 so we have to also take
                // immediately adjacent numbers as part of the key.
                if (!char.IsLetter(c) && !char.IsDigit(c))
                    break;

                builder.Append(c);
                i++;
            }

            // We want to roll back and go onto the last letter so that when
            // the main loop increments, it doesn't overshoot by a character.
            i--;

            string keyName = builder.ToString();
            InputKey key = InputKeyHelper.ToKey(keyName) ?? throw new ParserException($"Unexpected key: {keyName} (line: \"{text}\")");
            InputKeyEvent inputKeyEvent = new InputKeyEvent(key);

            // For laziness reasons we assume pressing the key holds it down
            // for the entire duration. The user needs to manually release
            // the key or it stays down until the end.
            if (key == InputKey.Control || key == InputKey.Alt || key == InputKey.Shift)
                inputKeyEvent.Down = true;

            return inputKeyEvent;
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
