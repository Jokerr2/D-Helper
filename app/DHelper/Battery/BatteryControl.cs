using DHelper.Hardware;

namespace DHelper.Battery;

internal sealed class BatteryControl
{
    private readonly ILaptopHal _hal;
    private int _applyGeneration;

    public BatteryControl(ILaptopHal hal) => _hal = hal;

    public ChargeMode CurrentMode { get; private set; } = ChargeMode.Custom;
    public int CustomStart { get; private set; } = 50;
    public int CustomStop { get; private set; } = 80;

    public event Action? Changed;

    public void LoadFromConfig()
    {
        CurrentMode = Enum.TryParse<ChargeMode>(AppConfig.Get("charge_mode", "Custom"), true, out var m)
            ? m : ChargeMode.Custom;
        CustomStart = AppConfig.Get("charge_start", 50);
        CustomStop = AppConfig.Get("charge_stop", 80);
        if (_hal is CompositeLaptopHal composite)
            composite.SeedCache(null, CurrentMode, CustomStart, CustomStop);
    }

    public void RefreshFromDevice()
    {
        LoadFromConfig();
        try
        {
            var device = _hal.GetChargeMode();
            if (device != null) CurrentMode = device.Value;
            if (_hal is CompositeLaptopHal composite)
            {
                var (s, e) = composite.GetCustomChargeBounds();
                if (s != null) CustomStart = s.Value;
                if (e != null) CustomStop = e.Value;
            }
        }
        catch { /* ignore */ }
    }

    public Task<(bool Ok, string Error)> ApplyAsync(ChargeMode mode, int? start, int? stop)
    {
        var s = start ?? CustomStart;
        var e = stop ?? CustomStop;

        if (mode == CurrentMode && s == CustomStart && e == CustomStop &&
            AppConfig.Get("charge_mode", "") == mode.ToString())
            return Task.FromResult((true, ""));

        var prevMode = CurrentMode;
        var prevStart = CustomStart;
        var prevStop = CustomStop;
        var gen = Interlocked.Increment(ref _applyGeneration);

        CurrentMode = mode;
        CustomStart = s;
        CustomStop = e;
        AppConfig.Set("charge_mode", mode.ToString());
        AppConfig.Set("charge_start", s);
        AppConfig.Set("charge_stop", e);
        Changed?.Invoke();

        return Task.Run(() =>
        {
            var ok = _hal.SetChargeMode(mode, s, e, out var error);
            if (gen != Volatile.Read(ref _applyGeneration))
                return (true, "");

            if (ok) return (true, "");

            CurrentMode = prevMode;
            CustomStart = prevStart;
            CustomStop = prevStop;
            AppConfig.Set("charge_mode", prevMode.ToString());
            AppConfig.Set("charge_start", prevStart);
            AppConfig.Set("charge_stop", prevStop);
            Changed?.Invoke();
            return (false, error);
        });
    }

    public bool Apply(ChargeMode mode, int? start, int? stop, out string error)
    {
        var task = ApplyAsync(mode, start, stop);
        task.Wait();
        error = task.Result.Error;
        return task.Result.Ok;
    }

    public Task<(bool Ok, string Error)> ApplySavedAsync() =>
        ApplyAsync(CurrentMode, CustomStart, CustomStop);

    public bool ApplySaved(out string error) =>
        Apply(CurrentMode, CustomStart, CustomStop, out error);
}
