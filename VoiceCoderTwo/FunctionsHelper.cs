using System;
using System.Text;

namespace VoiceCoderTwo
{
    public static class FunctionsHelper
    {
        public static int ReadInteger(string[] words, int startIndex, out int endIndexExclusive)
        {
            bool negative = words[startIndex] == "minus" || words[startIndex] == "negative";
            if (negative)
                startIndex++;

            return ReadNumber(words, startIndex, out endIndexExclusive);
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
