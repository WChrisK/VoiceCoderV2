using System;
using System.Threading;
using OpenTK.Input;

namespace VoiceCoderTwo
{
    public class JoystickToMouse
    {
        private const int MaxInputs = 4;
        private const int FastToggleButtonIndex = 5;
        private const double DeadZoneCutoff = 0.1;
        private const double FastModeScale = 0.0005;
        private const double SlowModeScale = FastModeScale / 6;

        public bool IsFastMove;
        private readonly bool useButton;
        private double accumulationX;
        private double accumulationY;

        private double SpeedFactor => IsFastMove ? FastModeScale : SlowModeScale;

        public JoystickToMouse(bool useButton = true)
        {
            Thread thread = new Thread(Poll) { IsBackground = true };
            thread.Start();

            this.useButton = useButton;
        }

        private void Poll()
        {
            DateTime lastRegisteredToggleTime = DateTime.Now;

            while (true)
            {
                for (int i = 0; i < MaxInputs; i++)
                {
                    JoystickCapabilities j = Joystick.GetCapabilities(i);
                    if (!j.IsConnected)
                        continue;

                    JoystickState state = Joystick.GetState(i);

                    if (useButton && state.GetButton(FastToggleButtonIndex) == ButtonState.Pressed)
                    {
                        TimeSpan timeSince = DateTime.Now - lastRegisteredToggleTime;
                        if (timeSince.Milliseconds > 600)
                        {
                            IsFastMove = !IsFastMove;
                            lastRegisteredToggleTime = DateTime.Now;
                            Console.WriteLine($"Mouse now in {(IsFastMove ? "FAST" : "slow")} mode");
                        }
                    }

                    for (int a = 0; a < j.AxisCount; a++)
                    {
                        double normalizedInput = state.GetAxis(a);
                        if (Math.Abs(normalizedInput) < DeadZoneCutoff)
                            continue;

                        switch (a)
                        {
                        case 0:
                            HandleAxisMovement(ref accumulationX, normalizedInput, true);
                            break;
                        case 1:
                            HandleAxisMovement(ref accumulationY, normalizedInput, false);
                            break;
                        }
                    }
                }
            }
        }

        private void HandleAxisMovement(ref double field, double normalizedInput, bool isAxisX)
        {
            field += normalizedInput * SpeedFactor;

            int pixelsToMove = 0;

            while (field >= 1.0)
            {
                field -= 1.0;
                pixelsToMove++;
            }

            while (field <= -1.0)
            {
                field += 1.0;
                pixelsToMove--;
            }

            if (pixelsToMove == 0)
                return;

            if (isAxisX)
                Native.MoveMouseOffset(pixelsToMove, 0);
            else
                Native.MoveMouseOffset(0, pixelsToMove);
        }
    }
}
