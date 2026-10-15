namespace GHelper.Dell;

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

/// <summary>One-shot Extra CCTK read used by the Extra UI (cached to avoid stacked loads).</summary>
public sealed class DellExtraSnapshot
{
    public DellExtraSnapshot(
        string? illum, ChargeMode? charge, string? wol,
        bool? usb, bool? lid, bool? cam, bool? autoOn, bool? block,
        bool needAdmin)
    {
        Illum = illum;
        Charge = charge;
        Wol = wol;
        Usb = usb;
        Lid = lid;
        Cam = cam;
        AutoOn = autoOn;
        Block = block;
        NeedAdmin = needAdmin;
    }

    public string? Illum { get; }
    public ChargeMode? Charge { get; }
    public string? Wol { get; }
    public bool? Usb { get; }
    public bool? Lid { get; }
    public bool? Cam { get; }
    public bool? AutoOn { get; }
    public bool? Block { get; }
    public bool NeedAdmin { get; }

    public DellExtraSnapshot WithOption(string option, string value) => option.ToLowerInvariant() switch
    {
        "keyboardillumination" => new DellExtraSnapshot(value, Charge, Wol, Usb, Lid, Cam, AutoOn, Block, NeedAdmin),
        "wakeonlan" => new DellExtraSnapshot(Illum, Charge, value, Usb, Lid, Cam, AutoOn, Block, NeedAdmin),
        _ => this
    };

    public DellExtraSnapshot WithEnabled(string option, bool enabled) => option.ToLowerInvariant() switch
    {
        "usbpowershare" => new DellExtraSnapshot(Illum, Charge, Wol, enabled, Lid, Cam, AutoOn, Block, NeedAdmin),
        "lidswitch" => new DellExtraSnapshot(Illum, Charge, Wol, Usb, enabled, Cam, AutoOn, Block, NeedAdmin),
        "camera" => new DellExtraSnapshot(Illum, Charge, Wol, Usb, Lid, enabled, AutoOn, Block, NeedAdmin),
        "autoon" => new DellExtraSnapshot(Illum, Charge, Wol, Usb, Lid, Cam, enabled, Block, NeedAdmin),
        "blocksleep" => new DellExtraSnapshot(Illum, Charge, Wol, Usb, Lid, Cam, AutoOn, enabled, NeedAdmin),
        _ => this
    };
}
