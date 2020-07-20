using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Speech.Recognition;
using System.Text;

namespace VoiceCoderTwo.Definitions
{
    public class GrammarNode
    {
        public string? Text;
        public GrammarNode? Next;
        public readonly List<GrammarNode> Children = new List<GrammarNode>();
        public int RepeatMin = 1;
        public int RepeatMax = 1;
        public bool IsDictation;
        public bool IsWildcard;

        private bool HasEitherTextOrChildren => Text != null ^ Children.Count > 0;
        private bool HasProperRepeatRange => RepeatMin <= RepeatMax && RepeatMin >= 0 && RepeatMax >= 1;
        private bool IsValid => IsDictation || IsWildcard || (HasEitherTextOrChildren && HasProperRepeatRange);

        public GrammarNode()
        {
        }

        /// <summary>
        /// Creates a deep copy.
        /// </summary>
        /// <param name="other">The node to deep copy.</param>
        public GrammarNode(GrammarNode other)
        {
            Text = other.Text;
            if (other.Next != null)
                Next = new GrammarNode(other.Next);
            foreach (GrammarNode node in other.Children)
                Children.Add(new GrammarNode(node));
            RepeatMin = other.RepeatMin;
            RepeatMax = other.RepeatMax;
            IsDictation = other.IsDictation;
            IsWildcard = other.IsWildcard;
        }

        public GrammarBuilder Build()
        {
            GrammarBuilder builder = new GrammarBuilder();

            GrammarNode? node = this;
            while (node != null)
            {
                Debug.Assert(node.IsValid, "Grammar node incorrectly constructed");

                if (node.IsDictation)
                    builder.AppendDictation();
                else if (node.IsWildcard)
                    builder.AppendWildcard();
                else if (node.Text != null)
                    builder.Append(node.Text, node.RepeatMin, node.RepeatMax);
                else
                {
                    Choices choices = new Choices();
                    foreach (GrammarNode child in node.Children)
                        choices.Add(child.Build());
                    builder.Append(choices, node.RepeatMin, node.RepeatMax);
                }

                node = node.Next;
            }

            return builder;
        }

        public string RepresentationString()
        {
            StringBuilder builder = new StringBuilder();

            GrammarNode? node = this;
            while (node != null)
            {
                if (node.Text != null)
                    builder.Append(node.Text);
                else
                {
                    builder.Append(node.RepeatMin == 0 ? '[' : '(');
                    builder.Append(string.Join(" | ", node.Children.Select(c => c.RepresentationString())));
                    builder.Append(node.RepeatMin == 0 ? ']' : ')');
                }

                if (node.Next != null)
                    builder.Append(' ');

                node = node.Next;
            }

            return builder.ToString();
        }

        public override string ToString() => $"Text={Text}, Next={Next != null}, Children={Children.Count}, Min/Max={RepeatMin}/{RepeatMax} IsDictation={IsDictation} IsWildcard={IsWildcard}";
    }
}
