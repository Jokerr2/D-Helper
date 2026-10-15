namespace GHelper.Dell;

public sealed class CompositeLaptopHal : ILaptopHal
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
        // Battery: CCTK is the reliable path. Optimizer ChargingMode is often denied on managed PCs.
        var battery = _cctk.Available || _optimizer.Available;
        _caps = new HalCapabilities
        {
            OptimizerAvailable = _optimizer.Available,
            CctkAvailable = _cctk.Available,
            ThermalControl = thermal,
            BatteryControl = battery,
            FnLockControl = _cctk.Available,
            PrimaryBackend = _cctk.Available ? "Dell Command Configure" : _optimizer.Available ? "Dell Optimizer" : "None",
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
        _cachedThermal = _cctk.Available
            ? _cctk.GetThermalMode()
            : _optimizer.GetThermalMode();
        return _cachedThermal;
    }

    public bool SetThermalMode(ThermalMode mode, out string error)
    {
        error = "";
        if (_cctk.Available)
        {
            if (_cctk.SetThermalMode(mode, out error))
            {
                _cachedThermal = mode;
                return true;
            }
            // Fall through to Optimizer only if CCTK hard-failed and Optimizer exists.
        }
        if (_optimizer.Available)
        {
            if (_optimizer.SetThermalMode(mode, out error))
            {
                _cachedThermal = mode;
                return true;
            }
        }
        _cachedThermal = null; // force next get to re-read
        error = DellErrors.Humanize(error);
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

        // IMPORTANT: When CCTK exists, use it exclusively for battery.
        // Dell Optimizer ChargingMode is frequently denied ("Access ... ChargingMode is denied")
        // on Dell Pro / IT-managed machines — falling through only shows a confusing error.
        if (_cctk.Available)
        {
            if (_cctk.SetChargeMode(mode, customStart, customStop, out error))
            {
                CacheCharge(mode, customStart, customStop);
                return true;
            }
            error = DellErrors.Humanize(error);
            _caps.LastError = error;
            return false;
        }

        if (_optimizer.Available)
        {
            if (_optimizer.SetChargeMode(mode, customStart, customStop, out error))
            {
                CacheCharge(mode, customStart, customStop);
                return true;
            }
            error = DellErrors.Humanize(error);
            _caps.LastError = error;
            return false;
        }

        error = "Install Dell Command | Configure for battery charge control";
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
        if (_cctk.Available)
        {
            var ok = _cctk.SetFnLock(enabled, out error);
            if (!ok) error = DellErrors.Humanize(error);
            return ok;
        }
        error = "FnLock requires Dell Command Configure";
        return false;
    }

    /// <summary>Sticky: true if any CCTK get in this process needed elevation.</summary>
    public bool NeededAdmin { get; private set; }

    private readonly object _extraGate = new();
    private DellExtraSnapshot? _extraSnap;
    private DateTime _extraSnapAt;
    private bool _extraPrefetchStarted;
    private Task<DellExtraSnapshot>? _extraRefreshTask;

    /// <summary>Read a CCTK string option (e.g. KeyboardIllumination). Null if unavailable.</summary>
    public string? GetCctkOption(string optionName)
    {
        if (!_cctk.Available) return null;
        var v = _cctk.GetOption(optionName);
        if (_cctk.LastGetNeededAdmin) NeededAdmin = true;
        return v;
    }

    public bool SetCctkOption(string optionName, string value, out string error) =>
        SetCctkOption(optionName, value, out error, out _);

    public bool SetCctkOption(string optionName, string value, out string error, out string? verified)
    {
        verified = null;
        if (_cctk.Available)
        {
            var ok = _cctk.SetOption(optionName, value, out error, out verified);
            if (ok)
            {
                lock (_extraGate)
                {
                    if (_extraSnap != null)
                        _extraSnap = _extraSnap.WithOption(optionName, verified ?? value);
                    _extraSnapAt = DateTime.UtcNow;
                }
            }
            else error = DellErrors.Humanize(error);
            return ok;
        }
        error = "Requires Dell Command Configure (cctk)";
        return false;
    }

    public bool? GetCctkEnabled(string optionName)
    {
        if (!_cctk.Available) return null;
        var v = _cctk.GetEnabled(optionName);
        if (_cctk.LastGetNeededAdmin) NeededAdmin = true;
        return v;
    }

    public bool SetCctkEnabled(string optionName, bool enabled, out string error) =>
        SetCctkEnabled(optionName, enabled, out error, out _);

    public bool SetCctkEnabled(string optionName, bool enabled, out string error, out string? verified)
    {
        verified = null;
        if (_cctk.Available)
        {
            var ok = _cctk.SetEnabled(optionName, enabled, out error, out verified);
            if (ok)
            {
                bool on = verified?.Equals("Enabled", StringComparison.OrdinalIgnoreCase) == true || enabled;
                lock (_extraGate)
                {
                    if (_extraSnap != null)
                        _extraSnap = _extraSnap.WithEnabled(optionName, on);
                    _extraSnapAt = DateTime.UtcNow;
                }
            }
            else error = DellErrors.Humanize(error);
            return ok;
        }
        error = "Requires Dell Command Configure (cctk)";
        return false;
    }

    /// <summary>Cached Extra snapshot if still fresh; otherwise null.</summary>
    public DellExtraSnapshot? TryGetExtraSnapshot(TimeSpan maxAge)
    {
        lock (_extraGate)
        {
            if (_extraSnap == null) return null;
            if (DateTime.UtcNow - _extraSnapAt > maxAge) return null;
            return _extraSnap;
        }
    }

    /// <summary>One CCTK pass for all Extra options; single-flight + cached.</summary>
    public Task<DellExtraSnapshot> RefreshExtraSnapshotAsync(bool force = false)
    {
        lock (_extraGate)
        {
            if (!force && _extraSnap != null && DateTime.UtcNow - _extraSnapAt < TimeSpan.FromSeconds(20))
                return Task.FromResult(_extraSnap);
            if (_extraRefreshTask is { IsCompleted: false })
                return _extraRefreshTask;

            _extraRefreshTask = Task.Run(() =>
            {
                // One cctk process for all Extra reads (~1× spawn cost instead of 8×)
                var map = _cctk.GetOptions(
                    "KeyboardIllumination",
                    "WakeOnLan",
                    "UsbPowerShare",
                    "LidSwitch",
                    "Camera",
                    "AutoOn",
                    "BlockSleep",
                    "PrimaryBattChargeCfg");
                if (_cctk.LastGetNeededAdmin) NeededAdmin = true;

                string? illum = map.GetValueOrDefault("KeyboardIllumination");
                string? wol = map.GetValueOrDefault("WakeOnLan");
                bool? usb = AsEnabled(map.GetValueOrDefault("UsbPowerShare"));
                bool? lid = AsEnabled(map.GetValueOrDefault("LidSwitch"));
                bool? cam = AsEnabled(map.GetValueOrDefault("Camera"));
                bool? autoOn = AsEnabled(map.GetValueOrDefault("AutoOn"));
                bool? block = AsEnabled(map.GetValueOrDefault("BlockSleep"));
                var chargeRaw = map.GetValueOrDefault("PrimaryBattChargeCfg");
                var charge = chargeRaw != null
                    ? DellCctkBackend.ParseChargeText("PrimaryBattChargeCfg=" + chargeRaw)
                    : (Mode: (ChargeMode?)null, Start: (int?)null, Stop: (int?)null);
                if (charge.Mode != null) CacheCharge(charge.Mode.Value, charge.Start, charge.Stop);

                var snap = new DellExtraSnapshot(illum, charge.Mode, wol, usb, lid, cam, autoOn, block, NeededAdmin);
                lock (_extraGate)
                {
                    _extraSnap = snap;
                    _extraSnapAt = DateTime.UtcNow;
                }
                Logger.WriteLine($"Dell Extra snapshot: illum={illum} charge={charge.Mode} wol={wol} usb={usb} lid={lid} cam={cam} autoOn={autoOn} block={block} admin={NeededAdmin}");
                return snap;
            });
            return _extraRefreshTask;
        }
    }

    private static bool? AsEnabled(string? v)
    {
        if (v == null) return null;
        if (v.Equals("Enabled", StringComparison.OrdinalIgnoreCase) || v == "1") return true;
        if (v.Equals("Disabled", StringComparison.OrdinalIgnoreCase) || v == "0") return false;
        return null;
    }

    /// <summary>Warm Extra cache after startup CCTK (battery/mode) so we don't queue behind them.</summary>
    public void PrefetchExtraSettingsAsync(int delayMs = 12000)
    {
        if (!_cctk.Available) return;
        lock (_extraGate)
        {
            if (_extraPrefetchStarted) return;
            _extraPrefetchStarted = true;
        }
        _ = Task.Run(async () =>
        {
            try
            {
                if (delayMs > 0) await Task.Delay(delayMs).ConfigureAwait(false);
                await RefreshExtraSnapshotAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Dell Extra prefetch: " + ex.Message);
            }
        });
    }

    /// <summary>Force a fresh CCTK charge read (bypasses HAL cache).</summary>
    public (ChargeMode? Mode, int? Start, int? Stop) RefreshChargeFromDevice()
    {
        if (!_cctk.Available) return (null, null, null);
        var c = _cctk.GetCharge();
        if (c.Mode != null) CacheCharge(c.Mode.Value, c.Start, c.Stop);
        return c;
    }

    public void InvalidateCaches()
    {
        _cachedThermal = null;
        _cachedCharge = null;
        _cachedStart = null;
        _cachedStop = null;
        NeededAdmin = false;
        lock (_extraGate)
        {
            _extraSnap = null;
            _extraSnapAt = DateTime.MinValue;
            // Leave in-flight refresh running; it will repopulate the cache
        }
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
