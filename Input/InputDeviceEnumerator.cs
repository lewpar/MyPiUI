using System.Diagnostics;

namespace MyKUIPi.Input;

public static class InputDeviceEnumerator
{
    public class InputDeviceInfo
    {
        public string? Path { get; set; }
        public bool IsKeyboard { get; set; }
        public bool IsTouchpad { get; set; }
        public bool IsMouse { get; set; }
    }

    public static List<InputDeviceInfo> EnumerateDevices()
    {
        var devices = new List<InputDeviceInfo>();
        var inputDir = "/dev/input";

        if (!Directory.Exists(inputDir))
        {
            return devices;
        }

        foreach (var device in Directory.GetFiles(inputDir, "event*"))
        {
            var info = GetDeviceInfo(device);

            if (info is null)
            {
                continue;
            }

            devices.Add(info);
        }

        return devices;
    }

    public static InputDeviceInfo? GetDeviceInfo(string devicePath)
    {
        try
        {
            var psi = new ProcessStartInfo("udevadm", $"info --query=all --name=\"{devicePath}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            var info = new InputDeviceInfo
            {
                Path = devicePath
            };

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("E: ID_INPUT_TOUCHPAD=1"))
                    info.IsTouchpad = true;
                if (line.Contains("E: ID_INPUT_KEYBOARD=1"))
                    info.IsKeyboard = true;
                if (line.Contains("E: ID_INPUT_MOUSE=1"))
                    info.IsMouse = true;
            }

            return info;
        }
        catch
        {
            return null;
        }
    }

    public static string? AutoDetectKeyboardDevice()
    {
        var devices = EnumerateDevices();
        return devices.FirstOrDefault(d => d.IsKeyboard)?.Path;
    }

    public static string? AutoDetectMouseDevice()
    {
        var devices = EnumerateDevices();
        return devices.FirstOrDefault(d => d.IsMouse)?.Path;
    }

    public static string? AutoDetectTouchDevice()
    {
        var devices = EnumerateDevices();
        return devices.FirstOrDefault(d => d.IsTouchpad)?.Path;
    }
} 