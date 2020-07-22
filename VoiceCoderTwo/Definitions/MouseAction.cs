using System;
using System.Drawing;
using System.Threading;

namespace VoiceCoderTwo.Definitions
{
    public class MouseAction
    {
        public int? X;
        public int? Y;
        public bool AbsoluteX;
        public bool AbsoluteY;
        public MouseClick? MouseClick;

        public void Execute()
        {
            if (X != null || Y != null)
            {
                Point point = Native.GetCursorPosition();

                if (X != null)
                {
                    if (AbsoluteX)
                        point.X = X.Value;
                    else
                        point.X += X.Value;
                }

                if (Y != null)
                {
                    if (AbsoluteY)
                        point.Y = Y.Value;
                    else
                        point.Y += Y.Value;
                }

                Native.MoveMouseAbsolute(point.X, point.Y);
            }

            switch (MouseClick)
            {
            case Definitions.MouseClick.Left:
                Native.DoMouseClick(true);
                break;
            case Definitions.MouseClick.Right:
                Native.DoMouseClick(false);
                break;
            case Definitions.MouseClick.Middle:
                goto default;
            case null:
                break;
            default:
                throw new Exception($"Unexpected mouse click type: {MouseClick}");
            }
        }
    }

    public enum MouseClick
    {
        Left,
        Right,
        Middle
    }
}
