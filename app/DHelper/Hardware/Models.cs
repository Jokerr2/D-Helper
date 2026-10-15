namespace DHelper.Hardware;

public enum ThermalMode
{
    Quiet,
    Optimized,
    Cool,
    Ultra
}

public enum ChargeMode
{
    Standard,
    ExpressCharge,
    PrimarilyAC,
    Adaptive,
    Custom
}

public sealed class HalCapabilities
{
    public bool OptimizerAvailable { get; init; }
    public bool CctkAvailable { get; init; }
    public bool ThermalControl { get; init; }
    public bool BatteryControl { get; init; }
    public bool FnLockControl { get; init; }
    public string PrimaryBackend { get; init; } = "None";
    public string OptimizerPath { get; init; } = "";
    public string CctkPath { get; init; } = "";
    public string LastError { get; set; } = "";
}

public sealed class SensorSnapshot
{
    public int? CpuTempC { get; init; }
    public int BatteryPercent { get; init; }
    public bool OnAc { get; init; }
    public string BatteryStatus { get; init; } = "";
    public int? FanRpm { get; init; }
}

public interface ILaptopHal : IDisposable
{
    HalCapabilities Capabilities { get; }
    void Probe();
    ThermalMode? GetThermalMode();
    bool SetThermalMode(ThermalMode mode, out string error);
    ChargeMode? GetChargeMode();
    bool SetChargeMode(ChargeMode mode, int? customStart, int? customStop, out string error);
    bool? GetFnLock();
    bool SetFnLock(bool enabled, out string error);
}
