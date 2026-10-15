using DHelper.Hardware;

namespace DHelper.Mode;

internal sealed class ModeControl
{
    private readonly ILaptopHal _hal;
    private int _applyGeneration;

    public ModeControl(ILaptopHal hal) => _hal = hal;

    public ThermalMode Current { get; private set; } = ThermalMode.Optimized;

    public event Action<ThermalMode, string?>? ModeChanged;

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

    /// <summary>
    /// Instant UI update, firmware write on a background thread.
    /// Rapid clicks: only the latest request is finalized.
    /// </summary>
    public Task<(bool Ok, string Error)> ApplyAsync(ThermalMode mode, bool syncWindowsPower = true)
    {
        if (mode == Current && AppConfig.Get("thermal_mode", "") == mode.ToString())
            return Task.FromResult((true, ""));

        var previous = Current;
        var gen = Interlocked.Increment(ref _applyGeneration);

        // Optimistic — UI updates immediately.
        Current = mode;
        AppConfig.Set("thermal_mode", mode.ToString());
        ModeChanged?.Invoke(mode, null);

        return Task.Run(() =>
        {
            var ok = _hal.SetThermalMode(mode, out var error);

            // Superseded by a newer click — abandon result.
            if (gen != Volatile.Read(ref _applyGeneration))
                return (true, "");

            if (ok)
            {
                if (syncWindowsPower)
                    WindowsSensors.SetWindowsPowerOverlay(mode);
                return (true, "");
            }

            // Revert optimistic state.
            Current = previous;
            AppConfig.Set("thermal_mode", previous.ToString());
            ModeChanged?.Invoke(previous, error);
            return (false, error);
        });
    }

    public bool Apply(ThermalMode mode, out string error, bool syncWindowsPower = true)
    {
        var task = ApplyAsync(mode, syncWindowsPower);
        task.Wait();
        error = task.Result.Error;
        return task.Result.Ok;
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
        _ = ApplyAsync(next);
        return next;
    }

    public void ApplyForPowerState(bool onAc)
    {
        if (!AppConfig.Get("auto_thermal", true)) return;
        var key = onAc ? "thermal_mode_ac" : "thermal_mode_dc";
        var name = AppConfig.Get(key, onAc ? "Optimized" : "Quiet");
        if (!Enum.TryParse<ThermalMode>(name, true, out var mode)) return;
        if (mode == Current) return;
        _ = ApplyAsync(mode);
    }
}
