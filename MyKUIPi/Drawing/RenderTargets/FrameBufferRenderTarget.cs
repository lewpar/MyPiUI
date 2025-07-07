using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text.RegularExpressions;

namespace MyKUIPi.Drawing.RenderTargets;

public class FrameBufferRenderTarget : IRenderTarget, IDisposable
{
    private readonly FileStream _frameBufferStream;
    private readonly MemoryMappedFile _frameBufferMemoryMap;
    private readonly MemoryMappedViewAccessor _frameBufferAccessor;
    
    public FrameBufferRenderTarget(string frameBufferDevicePath)
    {
        if (string.IsNullOrWhiteSpace(frameBufferDevicePath))
        {
            throw new Exception("FrameBufferDevice is not set.");
        }

        if (!File.Exists(frameBufferDevicePath))
        {
            throw new FileNotFoundException($"No frame buffer device at path '{frameBufferDevicePath}' exists.");
        }
        
        var frameBufferInfo = GetFrameBufferInfo();
        if (frameBufferInfo is null)
        {
            throw new Exception("Failed to get frame buffer info. Is `fbset` installed?");
        }

        var bytesPerPixel = frameBufferInfo.Depth / 8;
        var frameBufferSize = frameBufferInfo.Width * frameBufferInfo.VirtualHeight * bytesPerPixel;
        
        _frameBufferStream = new FileStream(frameBufferDevicePath, 
            FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        
        _frameBufferMemoryMap = MemoryMappedFile.CreateFromFile(_frameBufferStream, null, frameBufferSize,
            MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
        
        _frameBufferAccessor = _frameBufferMemoryMap.CreateViewAccessor(0, frameBufferSize, MemoryMappedFileAccess.Write);
    }
    
    public void SwapBuffer(byte[] buffer)
    {
        unsafe
        {
            fixed (byte* src = buffer)
            {
                byte* dst = null;
                _frameBufferAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref dst);
                try
                {
                    Buffer.MemoryCopy(src, dst, buffer.Length, buffer.Length);
                }
                finally
                {
                    _frameBufferAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }
    }
    
    private FrameBufferInfo? GetFrameBufferInfo()
    {
        try
        {
            var startInfo = new ProcessStartInfo("fbset")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return ParseFrameBufferInfo(output);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get frame buffer information from 'fbset': {ex.Message}");
        }
    }
    
    private FrameBufferInfo ParseFrameBufferInfo(string input)
    {
        var info = new FrameBufferInfo();

        var modeMatch = Regex.Match(input, @"mode\s+""(\d+x\d+)""");
        if (modeMatch.Success)
            info.Mode = modeMatch.Groups[1].Value;

        var geometryMatch = Regex.Match(input, @"geometry\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)");
        if (geometryMatch.Success)
        {
            info.Width = int.Parse(geometryMatch.Groups[1].Value);
            info.Height = int.Parse(geometryMatch.Groups[2].Value);
            info.VirtualWidth = int.Parse(geometryMatch.Groups[3].Value);
            info.VirtualHeight = int.Parse(geometryMatch.Groups[4].Value);
            info.Depth = int.Parse(geometryMatch.Groups[5].Value);
        }

        var timingsMatch = Regex.Match(input, @"timings\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)");
        if (timingsMatch.Success)
        {
            for (int i = 0; i < 7; i++)
            {
                info.Timings[i] = int.Parse(timingsMatch.Groups[i + 1].Value);
            }
        }

        var rgbaMatch = Regex.Match(input, @"rgba\s+(\d+)/(\d+),(\d+)/(\d+),(\d+)/(\d+),(\d+)/(\d+)");
        if (rgbaMatch.Success)
        {
            info.Rgba = new FrameBufferInfo.RgbaInfo
            {
                RedLength = int.Parse(rgbaMatch.Groups[1].Value),
                RedOffset = int.Parse(rgbaMatch.Groups[2].Value),

                GreenLength = int.Parse(rgbaMatch.Groups[3].Value),
                GreenOffset = int.Parse(rgbaMatch.Groups[4].Value),

                BlueLength = int.Parse(rgbaMatch.Groups[5].Value),
                BlueOffset = int.Parse(rgbaMatch.Groups[6].Value),

                AlphaLength = int.Parse(rgbaMatch.Groups[7].Value),
                AlphaOffset = int.Parse(rgbaMatch.Groups[8].Value),
            };
        }

        return info;
    }
    
    public void Dispose()
    {
        _frameBufferAccessor.Dispose();
        _frameBufferMemoryMap.Dispose();
        _frameBufferStream.Dispose();
    }
}