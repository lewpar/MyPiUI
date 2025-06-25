using System.Runtime.InteropServices;

namespace MyKUIPi.Input;

public class MouseReader : InputDeviceReader<MouseReader.InputEvent>
{
    private const ushort EV_REL = 0x02;
    private const ushort EV_KEY = 0x01;
    private const ushort EV_SYN = 0x00;

    private const ushort REL_X = 0x00;
    private const ushort REL_Y = 0x01;
    private const ushort REL_WHEEL = 0x08;

    private const ushort BTN_LEFT = 0x110;
    private const ushort BTN_RIGHT = 0x111;
    private const ushort BTN_MIDDLE = 0x112;

    public int DeltaX { get; private set; }
    public int DeltaY { get; private set; }
    public int WheelDelta { get; private set; }
    public bool LeftDown { get; private set; }
    public bool RightDown { get; private set; }
    public bool MiddleDown { get; private set; }

    [StructLayout(LayoutKind.Sequential)]
    public struct InputEvent
    {
        public TimeVal Time;
        public ushort Type;
        public ushort Code;
        public int Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TimeVal
    {
        public long TvSec;
        public long TvUsec;
    }

    public MouseReader(string devicePath) : base(devicePath) { }

    protected override void OnInputEvent(InputEvent inputEvent)
    {
        lock (LockObject)
        {
            switch (inputEvent.Type)
            {
                case EV_REL:
                    if (inputEvent.Code == REL_X)
                        DeltaX += inputEvent.Value;
                    else if (inputEvent.Code == REL_Y)
                        DeltaY += inputEvent.Value;
                    else if (inputEvent.Code == REL_WHEEL)
                        WheelDelta += inputEvent.Value;
                    break;
                case EV_KEY:
                    switch (inputEvent.Code)
                    {
                        case BTN_LEFT:
                            LeftDown = inputEvent.Value != 0;
                            break;
                        case BTN_RIGHT:
                            RightDown = inputEvent.Value != 0;
                            break;
                        case BTN_MIDDLE:
                            MiddleDown = inputEvent.Value != 0;
                            break;
                    }
                    break;
                case EV_SYN:
                    // Sync event - could be used to batch input frame, if needed
                    break;
            }
        }
    }

    public (int dx, int dy, int wheel) GetAndResetDeltas()
    {
        lock (LockObject)
        {
            var result = (DeltaX, DeltaY, WheelDelta);
            DeltaX = 0;
            DeltaY = 0;
            WheelDelta = 0;
            return result;
        }
    }
}