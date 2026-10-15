namespace DHelper.Hardware;

internal sealed class CompositeLaptopHal : ILaptopHal
{
    private readonly DellOptimizerBackend _optimizer = new();
    private readonly DellCctkBackend _cctk = new();
    private HalCapabilities _caps = new();
    private bool _probed;
    private ThermalMode? _cachedThermal;
    private ChargeMode? _cachedCharge;
    private int? _cachedStart;
    private int? _cachedStop;

    public HalCapabilities Capabilities => _caps;

    public void Probe()
    {
        if (_probed) return;
        _optimizer.Probe();
        _cctk.Probe();

        var thermal = _optimizer.Available || _cctk.Available;
        _caps = new HalCapabilities
        {
            OptimizerAvailable = _optimizer.Available,
            CctkAvailable = _cctk.Available,
            ThermalControl = thermal,
            BatteryControl = thermal,
            FnLockControl = _cctk.Available,
            PrimaryBackend = _optimizer.Available ? "Dell Optimizer" : _cctk.Available ? "Dell Command Configure" : "None",
            OptimizerPath = _optimizer.Path ?? "",
            CctkPath = _cctk.Path ?? "",
            LastError = thermal
                ? ""
                : "Install Dell Optimizer and/or Dell Command | Configure, then restart D-Helper as Administrator."
        };
        _probed = true;
    }

    public void ForceReprobe()
    {
        _probed = false;
        Probe();
    }

    public ThermalMode? GetThermalMode()
    {
        if (_cachedThermal != null) return _cachedThermal;
        _cachedThermal = _optimizer.Available
            ? _optimizer.GetThermalMode()
            : _cctk.GetThermalMode();
        return _cachedThermal;
    }

    public bool SetThermalMode(ThermalMode mode, out string error)
    {
        error = "";
        // One backend only — Optimizer preferred, no double process.
        if (_optimizer.Available)
        {
            if (_optimizer.SetThermalMode(mode, out error))
            {
                _cachedThermal = mode;
                return true;
            }
        }
        if (_cctk.Available)
        {
            if (_cctk.SetThermalMode(mode, out error))
            {
                _cachedThermal = mode;
                return true;
            }
        }
        if (string.IsNullOrEmpty(error))
            error = "No Dell thermal backend available";
        _caps.LastError = error;
        return false;
    }

    public ChargeMode? GetChargeMode()
    {
        if (_cachedCharge != null) return _cachedCharge;
        if (_cctk.Available)
        {
            var c = _cctk.GetCharge();
            _cachedCharge = c.Mode;
            _cachedStart = c.Start;
            _cachedStop = c.Stop;
            return _cachedCharge;
        }
        _cachedCharge = _optimizer.GetChargeMode();
        return _cachedCharge;
    }

    public bool SetChargeMode(ChargeMode mode, int? customStart, int? customStop, out string error)
    {
        error = "";

        // Prefer CCTK for Custom thresholds; otherwise single Optimizer call.
        if (mode == ChargeMode.Custom && _cctk.Available)
        {
            if (_cctk.SetChargeMode(mode, customStart, customStop, out error))
            {
                CacheCharge(mode, customStart, customStop);
                return true;
            }
        }

        if (_optimizer.Available)
        {
            if (_optimizer.SetChargeMode(mode, customStart, customStop, out error))
            {
                // Custom bounds still need CCTK when Optimizer can't set them.
                if (mode == ChargeMode.Custom && _cctk.Available)
                    _cctk.SetChargeMode(mode, customStart, customStop, out _);
                CacheCharge(mode, customStart, customStop);
                return true;
            }
        }

        if (_cctk.Available && _cctk.SetChargeMode(mode, customStart, customStop, out error))
        {
            CacheCharge(mode, customStart, customStop);
            return true;
        }

        if (string.IsNullOrEmpty(error))
            error = "No Dell battery backend available";
        _caps.LastError = error;
        return false;
    }

    private void CacheCharge(ChargeMode mode, int? start, int? stop)
    {
        _cachedCharge = mode;
        if (start != null) _cachedStart = start;
        if (stop != null) _cachedStop = stop;
    }

    public bool? GetFnLock() => _cctk.Available ? _cctk.GetFnLock() : null;

    public bool SetFnLock(bool enabled, out string error)
    {
        if (_cctk.Available) return _cctk.SetFnLock(enabled, out error);
        error = "FnLock requires Dell Command Configure";
        return false;
    }

    public (int? Start, int? Stop) GetCustomChargeBounds() => (_cachedStart, _cachedStop);

    public void SeedCache(ThermalMode? thermal, ChargeMode? charge, int? start, int? stop)
    {
        if (thermal != null) _cachedThermal = thermal;
        if (charge != null) _cachedCharge = charge;
        if (start != null) _cachedStart = start;
        if (stop != null) _cachedStop = stop;
    }

    public void Dispose() { }
}
