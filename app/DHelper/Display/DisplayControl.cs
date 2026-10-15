using System.Runtime.InteropServices;

namespace DHelper.Display;

internal sealed class DisplayControl
{
    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int CDS_UPDATEREGISTRY = 0x01;
    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private int _cachedHz;
    private long _hzNextTick;
    private static readonly long HzCacheTicks = TimeSpan.FromSeconds(10).Ticks;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

    [DllImport("user32.dll")]
    private static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

    public int GetCurrentRefreshRate()
    {
        var now = DateTime.UtcNow.Ticks;
        if (_cachedHz > 0 && now < _hzNextTick) return _cachedHz;
        var dm = CreateDevMode();
        if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm))
            _cachedHz = dm.dmDisplayFrequency;
        _hzNextTick = now + HzCacheTicks;
        return _cachedHz;
    }

    public int GetMaxRefreshRate()
    {
        int max = 0;
        var dm = CreateDevMode();
        int i = 0;
        while (EnumDisplaySettings(null, i, ref dm))
        {
            if (dm.dmDisplayFrequency > max) max = dm.dmDisplayFrequency;
            i++;
        }
        return max > 0 ? max : GetCurrentRefreshRate();
    }

    public bool SetRefreshRate(int hz)
    {
        if (hz <= 0) return false;
        var dm = CreateDevMode();
        if (!EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm)) return false;
        dm.dmDisplayFrequency = hz;
        dm.dmFields = 0x400000; // DM_DISPLAYFREQUENCY
        var ok = ChangeDisplaySettings(ref dm, CDS_UPDATEREGISTRY) == DISP_CHANGE_SUCCESSFUL;
        if (ok)
        {
            _cachedHz = hz;
            _hzNextTick = DateTime.UtcNow.Ticks + HzCacheTicks;
        }
        return ok;
    }

    public void ApplyForPowerState(bool onAc)
    {
        if (!AppConfig.Get("auto_refresh", true)) return;
        int target;
        if (onAc)
        {
            target = AppConfig.Get("refresh_ac", 0);
            if (target <= 0) target = GetMaxRefreshRate();
        }
        else
        {
            target = AppConfig.Get("refresh_dc", 60);
        }
        if (target > 0 && target == GetCurrentRefreshRate()) return;
        SetRefreshRate(target);
    }

    private static DEVMODE CreateDevMode()
    {
        var dm = new DEVMODE
        {
            dmDeviceName = new string('\0', 32),
            dmFormName = new string('\0', 32)
        };
        dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        return dm;
    }
}
