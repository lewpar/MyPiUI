using System.Runtime.InteropServices;
using MyKUIPi.Configuration;

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

    public TouchReader(string devicePath) : base(devicePath)
    {
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
        var config = RuntimeConfig.Instance;
        if (config is null)
        {
            throw new NullReferenceException("Failed to get touch state, runtime config is not loaded.");
        }

        if (config is { MinTouchX: 0, MinTouchY: 0, MaxTouchX: 0, MaxTouchY: 0 })
        {
            throw new  NullReferenceException("Failed to get touch state, touch is not calibrated.");
        }

        lock (LockObject)
        {
            // Prevent divide-by-zero in case of calibration error
            float rangeX = config.MaxTouchX - config.MinTouchX;
            float rangeY = config.MaxTouchY - config.MinTouchY;

            if (rangeX <= 0 || rangeY <= 0)
            {
                return (0f, 0f, IsTouching); // fallback if calibration is invalid
            }

            float normX = Math.Clamp((float)(TouchX - config.MinTouchX) / rangeX, 0f, 1f);
            float normY = Math.Clamp((float)(TouchY - config.MinTouchY) / rangeY, 0f, 1f);

            return (normX, normY, IsTouching);
        }
    }


    public (float x, float y, bool isTouching) GetAbsTouchState()
    {
        lock (LockObject)
        {
            return (TouchX, TouchY, IsTouching);
        }
    }
}