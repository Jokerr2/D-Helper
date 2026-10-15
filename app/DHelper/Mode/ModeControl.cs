using DHelper.Hardware;

namespace DHelper.Mode;

internal sealed class ModeControl
{
    private readonly ILaptopHal _hal;

    public ModeControl(ILaptopHal hal) => _hal = hal;

    public ThermalMode Current { get; private set; } = ThermalMode.Optimized;

    public event Action<ThermalMode, string?>? ModeChanged;

    /// <summary>Config-only load — no CLI. Use at startup for a light idle footprint.</summary>
    public void LoadFromConfig()
    {
        var saved = AppConfig.Get("thermal_mode", "Optimized");
        Current = Enum.TryParse<ThermalMode>(saved, true, out var m) ? m : ThermalMode.Optimized;
        if (_hal is CompositeLaptopHal composite)
            composite.SeedCache(Current, null, null, null);
    }

    public void RefreshFromDevice()
    {
        var mode = _hal.GetThermalMode();
        if (mode != null) Current = mode.Value;
        else LoadFromConfig();
    }

    public bool Apply(ThermalMode mode, out string error, bool syncWindowsPower = true)
    {
        // Skip redundant firmware writes.
        if (mode == Current && AppConfig.Get("thermal_mode", "") == mode.ToString())
        {
            error = "";
            return true;
        }

        var ok = _hal.SetThermalMode(mode, out error);
        if (ok)
        {
            Current = mode;
            AppConfig.Set("thermal_mode", mode.ToString());
            if (syncWindowsPower)
                WindowsSensors.SetWindowsPowerOverlay(mode);
            ModeChanged?.Invoke(mode, null);
        }
        else
        {
            ModeChanged?.Invoke(Current, error);
        }
        return ok;
    }

    public ThermalMode Cycle()
    {
        var next = Current switch
        {
            ThermalMode.Quiet => ThermalMode.Optimized,
            ThermalMode.Optimized => ThermalMode.Cool,
            ThermalMode.Cool => ThermalMode.Ultra,
            _ => ThermalMode.Quiet
        };
        Apply(next, out _);
        return next;
    }

    public void ApplyForPowerState(bool onAc)
    {
        if (!AppConfig.Get("auto_thermal", true)) return;
        var key = onAc ? "thermal_mode_ac" : "thermal_mode_dc";
        var name = AppConfig.Get(key, onAc ? "Optimized" : "Quiet");
        if (!Enum.TryParse<ThermalMode>(name, true, out var mode)) return;
        if (mode == Current) return;
        Apply(mode, out _);
    }
}
