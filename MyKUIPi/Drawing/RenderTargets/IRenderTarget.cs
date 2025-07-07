namespace MyKUIPi.Drawing.RenderTargets;

public interface IRenderTarget
{
    void SwapBuffer(byte[] buffer);
}