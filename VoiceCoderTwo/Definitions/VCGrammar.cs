using System.Speech.Recognition;

namespace VoiceCoderTwo.Definitions
{
    /// <summary>
    /// A grammar object that can be loaded into the speech recognition engine,
    /// while also allows its recognition to be attached to data for executing
    /// actions.
    /// </summary>
    public class VCGrammar : Grammar
    {
        public readonly Mode Mode;
        public readonly Command Command;

        public VCGrammar(Mode mode, Command command, GrammarBuilder builder) : base(builder)
        {
            Mode = mode;
            Command = command;
        }
    }
}
