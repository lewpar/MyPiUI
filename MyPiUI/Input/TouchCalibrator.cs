using MyPiUI.Configuration;
using MyPiUI.Drawing.Buffers;
using MyPiUI.Drawing.RenderTargets;
using MyPiUI.Primitives;

namespace MyPiUI.Input;

public class TouchCalibrator
{
    private readonly InputManager _inputManager;
    private readonly IDrawBuffer _buffer;
    private readonly IRenderTarget _renderTarget;
    private readonly MyEngineOptions _options;

    public TouchCalibrator(InputManager inputManager, IDrawBuffer buffer, 
        IRenderTarget renderTarget, MyEngineOptions options)
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _renderTarget = renderTarget ?? throw new ArgumentNullException(nameof(renderTarget));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Runs the full calibration (bounding + refined bounds).
    /// </summary>
    public void RunCalibration(int holdMs)
    {
        // Phase 1: bounding box
        var (minX, minY, maxX, maxY) = CalibrateBoundingBox(holdMs);

        // Phase 2: refine bounds based on error
        (minX, minY, maxX, maxY) = RefineBounds(minX, minY, maxX, maxY);

        // Save into config
        var config = RuntimeConfig.Instance ?? throw new InvalidOperationException("Runtime config not loaded.");
        config.MinTouchX = minX;
        config.MinTouchY = minY;
        config.MaxTouchX = maxX;
        config.MaxTouchY = maxY;
        config.Save();
    }

    private (int minX, int minY, int maxX, int maxY) CalibrateBoundingBox(int holdMs)
    {
        var topLeft = WaitForHoldPoint("Top Left", holdMs);
        var bottomRight = WaitForHoldPoint("Bottom Right", holdMs);

        return (
            Math.Min(topLeft.X, bottomRight.X),
            Math.Min(topLeft.Y, bottomRight.Y),
            Math.Max(topLeft.X, bottomRight.X),
            Math.Max(topLeft.Y, bottomRight.Y)
        );
    }

    private (int minX, int minY, int maxX, int maxY) RefineBounds(int minX, int minY, int maxX, int maxY)
    {
        var targets = new[]
        {
            new Point(50, 50),
            new Point(_options.RenderWidth - 50, 50),
            new Point(_options.RenderWidth / 2, _options.RenderHeight / 2),
            new Point(50, _options.RenderHeight - 50),
            new Point(_options.RenderWidth - 50, _options.RenderHeight - 50)
        };

        var xErrors = new List<int>();
        var yErrors = new List<int>();

        foreach (var target in targets)
        {
            DrawCalibrationTarget(target, "Tap the square");
            var reported = WaitForTap();

            // Convert reported touch → screen space based on current min/max
            var mappedX = Map(reported.X, minX, maxX, _options.RenderWidth);
            var mappedY = Map(reported.Y, minY, maxY, _options.RenderHeight);

            xErrors.Add(target.X - mappedX);
            yErrors.Add(target.Y - mappedY);
        }

        // Take average error
        var avgX = (int)xErrors.Average();
        var avgY = (int)yErrors.Average();

        // Adjust bounds: shift min/max so mapping matches better
        if (avgX < 0) maxX += avgX; // pressed right → expand max
        if (avgX > 0) minX -= avgX; // pressed left → reduce min
        if (avgY < 0) maxY += avgY; // pressed below → expand max
        if (avgY > 0) minY -= avgY; // pressed above → reduce min

        return (minX, minY, maxX, maxY);
    }

    private int Map(int raw, int min, int max, int size)
    {
        return (int)((raw - min) / (double)(max - min) * size);
    }

    private void DrawCalibrationTarget(Point target, string message)
    {
        _buffer.Clear();

        int size = 20;
        _buffer.DrawRect(new Rectangle(target.X - size / 2, target.Y - size / 2, size, size), Color.Red);

        var font = "Roboto"; float fontSize = 20;
        var textSize = _buffer.MeasureText(message, font, fontSize);
        _buffer.DrawText(
            new Point((_options.RenderWidth - textSize.Width) / 2, _options.RenderHeight - textSize.Height - 20),
            message, font, fontSize, Color.White);

        _renderTarget.SwapBuffer(_buffer.GetBuffer());
    }

    private Point WaitForHoldPoint(string label, int holdMs = 3500)
    {
        DateTime? holdStart = null;

        while (true)
        {
            var (x, y, isTouching) = _inputManager.GetAbsTouchState();

            if (isTouching)
            {
                holdStart ??= DateTime.UtcNow;
                var held = (DateTime.UtcNow - holdStart.Value).TotalMilliseconds;

                if (held >= holdMs)
                {
                    // ✅ Require release before continuing
                    var confirmed = new Point((int)x, (int)y);
                    WaitForRelease();
                    return confirmed;
                }

                DrawHoldScreen(label, held, holdMs);
            }
            else
            {
                holdStart = null;
                DrawHoldScreen(label, 0, holdMs);
            }

            Thread.Sleep(10);
        }
    }

    private void WaitForRelease()
    {
        // Blocks until the user lifts their finger
        while (true)
        {
            var (_, _, isTouching) = _inputManager.GetAbsTouchState();
            if (!isTouching) break;
            Thread.Sleep(10);
        }
    }

    private void DrawHoldScreen(string label, double heldMs, int holdMs)
    {
        _buffer.Clear();

        var font = "Roboto";
        float fontSize = 25;

        // Main text
        var msg = $"Hold {label} Corner";
        var msgSize = _buffer.MeasureText(msg, font, fontSize);
        _buffer.DrawText(
            new Point((_options.RenderWidth - msgSize.Width) / 2,
                (_options.RenderHeight - msgSize.Height) / 2 - 20),
            msg, font, fontSize, Color.White);

        // Subtext with progress
        var secondsHeld = heldMs / 1000.0;
        var secondsTotal = holdMs / 1000.0;
        var subtext = $"{secondsHeld:F1} / {secondsTotal:F1} sec";
        var subSize = _buffer.MeasureText(subtext, font, fontSize - 5);
        _buffer.DrawText(
            new Point((_options.RenderWidth - subSize.Width) / 2,
                (_options.RenderHeight - subSize.Height) / 2 + 20),
            subtext, font, fontSize - 5, Color.LightGray);

        _renderTarget.SwapBuffer(_buffer.GetBuffer());
    }

    private Point WaitForTap()
    {
        bool touched = false;
        Point result = default;

        while (true)
        {
            var (x, y, isTouching) = _inputManager.GetAbsTouchState();

            if (isTouching && !touched)
            {
                result = new Point((int)x, (int)y);
                touched = true;
            }
            else if (!isTouching && touched)
            {
                return result;
            }

            Thread.Sleep(10);
        }
    }
}
