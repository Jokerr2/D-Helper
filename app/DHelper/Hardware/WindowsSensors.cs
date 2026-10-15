using System.Runtime.InteropServices;

namespace DHelper.Hardware;

/// <summary>
/// Lightweight sensors: SystemInformation only on the hot path.
/// CPU temp uses a rare WMI probe and is cached (never blocks tray idle).
/// </summary>
internal sealed class WindowsSensors : IDisposable
{
    private int? _cachedTempC;
    private long _tempNextTick;
    private bool _tempUnsupported;
    private static readonly long TempIntervalTicks = TimeSpan.FromSeconds(20).Ticks;

    public SensorSnapshot Read()
    {
        var power = SystemInformation.PowerStatus;
        int percent = (int)Math.Round(power.BatteryLifePercent * 100.0);
        if (percent is < 0 or > 100) percent = 0;

        return new SensorSnapshot
        {
            CpuTempC = GetCachedTemp(),
            BatteryPercent = percent,
            OnAc = power.PowerLineStatus == PowerLineStatus.Online,
            BatteryStatus = power.BatteryChargeStatus.ToString(),
            FanRpm = null
        };
    }

    private int? GetCachedTemp()
    {
        if (_tempUnsupported) return null;
        var now = DateTime.UtcNow.Ticks;
        if (now < _tempNextTick) return _cachedTempC;
        _tempNextTick = now + TempIntervalTicks;
        _cachedTempC = TryReadTempOnce();
        if (_cachedTempC == null) _tempUnsupported = true;
        return _cachedTempC;
    }

    private static int? TryReadTempOnce()
    {
        // Avoid System.Management on the hot path — use a one-shot COM-free approach via WMI only when needed.
        // Deferred: return null if unavailable; keeps idle RAM/CPU tiny.
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                @"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            searcher.Options.Timeout = TimeSpan.FromMilliseconds(200);
            foreach (System.Management.ManagementObject obj in searcher.Get())
            {
                var raw = Convert.ToDouble(obj["CurrentTemperature"]);
                var c = (int)Math.Round(raw / 10.0 - 273.15);
                if (c is > 0 and < 120) return c;
            }
        }
        catch { /* unsupported */ }
        return null;
    }

    public static void SetWindowsPowerOverlay(ThermalMode mode)
    {
        // Prefer native API — no powercfg.exe process spawn.
        var guid = mode switch
        {
            ThermalMode.Quiet => new Guid("961cc777-2547-4f9d-8174-7d86181b8a7a"),
            ThermalMode.Ultra => new Guid("ded574b5-45a0-4f42-8737-46345c09c238"),
            _ => Guid.Empty
        };
        try
        {
            PowerSetActiveOverlayScheme(guid);
        }
        catch { /* ignore on older builds */ }
    }

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode)]
    private static extern uint PowerSetActiveOverlayScheme(Guid OverlaySchemeGuid);

    public void Dispose() { }
}
