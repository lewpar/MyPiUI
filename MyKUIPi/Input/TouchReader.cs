using System.Runtime.InteropServices;

namespace MyKUIPi.Input;

public class TouchReader : InputDeviceReader<TouchReader.InputEvent>
{
    private const ushort EV_ABS = 0x03;
    private const ushort EV_KEY = 0x01;
    private const ushort EV_SYN = 0x00;

    private const ushort ABS_X = 0x00;
    private const ushort ABS_Y = 0x01;
    private const ushort ABS_MT_POSITION_X = 0x35;
    private const ushort ABS_MT_POSITION_Y = 0x36;

    private const ushort BTN_TOUCH = 0x14a;

    private readonly int _maxTouchX;
    private readonly int _maxTouchY;

    public int TouchX { get; private set; }
    public int TouchY { get; private set; }
    public bool IsTouching { get; private set; }

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

    public TouchReader(string devicePath, int maxTouchX, int maxTouchY) : base(devicePath)
    {
        _maxTouchX = maxTouchX;
        _maxTouchY = maxTouchY;
    }

    protected override void OnInputEvent(InputEvent inputEvent)
    {
        lock (LockObject)
        {
            switch (inputEvent.Type)
            {
                case EV_ABS:
                    if (inputEvent.Code == ABS_X || inputEvent.Code == ABS_MT_POSITION_X)
                        TouchX = inputEvent.Value;
                    else if (inputEvent.Code == ABS_Y || inputEvent.Code == ABS_MT_POSITION_Y)
                        TouchY = inputEvent.Value;
                    break;
                case EV_KEY:
                    if (inputEvent.Code == BTN_TOUCH)
                        IsTouching = inputEvent.Value != 0;
                    break;
                case EV_SYN:
                    // Touch frame complete
                    break;
            }
        }
    }

    public (float normX, float normY, bool isTouching) GetTouchState()
    {
        lock (LockObject)
        {
            float normX = Math.Clamp((float)TouchX / _maxTouchX, 0f, 1f);
            float normY = Math.Clamp((float)TouchY / _maxTouchY, 0f, 1f);
            return (1 - normY, normX, IsTouching);
        }
    }
}