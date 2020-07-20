using System;

namespace VoiceCoderTwo.Definitions
{
    public class ParserException : Exception
    {
        public ParserException(string reason) : base(reason)
        {
        }
    }
}
