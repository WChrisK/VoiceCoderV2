using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VoiceCoderTwo
{
    public class Native
    {
        public const uint MOUSE_LEFT_DOWN = 0x02;
        public const uint MOUSE_LEFT_UP = 0x04;
        public const uint MOUSE_RIGHT_DOWN = 0x08;
        public const uint MOUSE_RIGHT_UP = 0x10;
        public const uint MOUSE_BUTTON_LEFT = MOUSE_LEFT_DOWN | MOUSE_LEFT_UP;
        public const uint MOUSE_BUTTON_RIGHT = MOUSE_RIGHT_DOWN | MOUSE_RIGHT_UP;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, UInt32 dwNewLong);

        // Source: http://stackoverflow.com/questions/1316681/getting-mouse-position-in-c-sharp
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        public static Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
        }

        public static void SetCursorPosition(Point point)
        {
            if (point != null)
            {
                Cursor.Position = point;
            }
        }

        public static Rectangle GetScreenDimensions()
        {
            return Screen.PrimaryScreen.WorkingArea;
        }

        public static void DoMouseClick(bool leftButton)
        {
            mouse_event(leftButton ? MOUSE_BUTTON_LEFT : MOUSE_BUTTON_RIGHT, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
        }

        public static void DoMouseDrag(bool isDragging)
        {
            mouse_event(isDragging ? MOUSE_LEFT_DOWN : MOUSE_LEFT_UP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
        }

        public static void MoveMouseOffset(int xOffset, int yOffset)
        {
            Point currentPoint = GetCursorPosition();
            SetCursorPosition(new Point(currentPoint.X + xOffset, currentPoint.Y + yOffset));
        }

        public static void MoveMouseAbsolute(int xOffset, int yOffset)
        {
            Cursor.Position = new Point(xOffset, yOffset);
        }

        public static void EmitKeys(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            IntPtr p = GetForegroundWindow();
            if (p == IntPtr.Zero)
                return;

            SendKeys.SendWait(data);
            SendKeys.Flush();
        }

        public static void IncreaseVolume()
        {
            keybd_event((byte)Keys.VolumeUp, 0, 0, 0);
        }

        public static void DecreaseVolume()
        {
            keybd_event((byte)Keys.VolumeDown, 0, 0, 0); // decrease volume
        }
    }
}
