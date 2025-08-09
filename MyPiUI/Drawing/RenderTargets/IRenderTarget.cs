namespace MyPiUI.Drawing.RenderTargets;

public interface IRenderTarget
{
    void SwapBuffer(ReadOnlySpan<byte> buffer);
    public MyGraphicsContext CreateGraphicsContext();
}