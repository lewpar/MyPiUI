namespace MyPiUI.Drawing.RenderTargets;

public interface IRenderTarget
{
    void SwapBuffer(byte[] buffer);
    public MyGraphicsContext CreateGraphicsContext();
}