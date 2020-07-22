using System.Text;

namespace VoiceCoderTwo
{
    public static class FunctionsHelper
    {
        private static bool IsNumber(string word)
        {
            return word switch
            {
                "zero" => true,
                "oh" => true,
                "one" => true,
                "two" => true,
                "three" => true,
                "four" => true,
                "five" => true,
                "six" => true,
                "seven" => true,
                "eight" => true,
                "nine" => true,
                _ => false
            };
        }

        public static int? FindTrailingNumberStartIndex(string[] words)
        {
            for (int i = words.Length - 1; i >= 0; i--)
                if (IsNumber(words[i]))
                    return i;
            return null;
        }

        public static int? ReadTrailingNumber(string[] words)
        {
            int? start = FindTrailingNumberStartIndex(words);
            if (start == null)
                return null;

            return ReadNumber(words, start.Value, out _);
        }

        public static int ReadInteger(string[] words, int startIndex, out int endIndexExclusive)
        {
            bool negative = words[startIndex] == "minus" || words[startIndex] == "negative";
            if (negative)
                startIndex++;

            int number = ReadNumber(words, startIndex, out endIndexExclusive);
            return negative ? -number : number;
        }

        public static int ReadNumber(string[] words, int startIndex, out int endIndexExclusive)
        {
            StringBuilder builder = new StringBuilder();

            int index = startIndex;
            while (index < words.Length)
            {
                switch (words[index])
                {
                case "zero":
                    builder.Append("0");
                    break;
                case "oh":
                    builder.Append("0");
                    break;
                case "one":
                    builder.Append("1");
                    break;
                case "two":
                    builder.Append("2");
                    break;
                case "three":
                    builder.Append("3");
                    break;
                case "four":
                    builder.Append("4");
                    break;
                case "five":
                    builder.Append("5");
                    break;
                case "six":
                    builder.Append("6");
                    break;
                case "seven":
                    builder.Append("7");
                    break;
                case "eight":
                    builder.Append("8");
                    break;
                case "nine":
                    builder.Append("9");
                    break;
                default:
                    goto ExitSwitch;
                }

                index++;
            }

            ExitSwitch:
            endIndexExclusive = index;
            int.TryParse(builder.ToString(), out int number);
            return number;
        }
    }
}
