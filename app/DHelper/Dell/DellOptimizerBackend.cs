using System.Text.RegularExpressions;

namespace GHelper.Dell;

internal sealed class DellOptimizerBackend
{
    private string? _path;

    public string? Path => _path;
    public bool Available => !string.IsNullOrEmpty(_path);

    public void Probe()
    {
        _path = CliRunner.FindFirstExisting(
            @"C:\Program Files\Dell\DellOptimizer\do-cli.exe",
            @"C:\Program Files\Dell\Dell Optimizer\do-cli.exe",
            @"C:\Program Files (x86)\Dell\DellOptimizer\do-cli.exe",
            @"C:\Program Files\Dell\Optimizer\do-cli.exe");
    }

    public ThermalMode? GetThermalMode()
    {
        if (!Available) return null;
        var (code, stdout, stderr) = CliRunner.Run(_path!, "/get -name=SystemPowerConfiguration.ThermalMode");
        var text = stdout + "\n" + stderr;
        if (code != 0 && string.IsNullOrWhiteSpace(text)) return null;
        return ParseThermal(text);
    }

    public bool SetThermalMode(ThermalMode mode, out string error)
    {
        error = "";
        if (!Available) { error = "Dell Optimizer (do-cli) not found"; return false; }
        var value = mode switch
        {
            ThermalMode.Quiet => "Quiet",
            ThermalMode.Cool => "Cool",
            ThermalMode.Ultra => "Ultra",
            _ => "Optimized"
        };
        var (code, stdout, stderr) = CliRunner.Run(_path!, $"/configure -name=SystemPowerConfiguration.ThermalMode -value={value}");
        if (code == 0) return true;
        error = FirstUseful(stdout, stderr, $"do-cli exit {code}");
        return false;
    }

    public ChargeMode? GetChargeMode()
    {
        if (!Available) return null;
        var (code, stdout, stderr) = CliRunner.Run(_path!, "/get -name=SystemPowerConfiguration.ChargingMode");
        var text = stdout + "\n" + stderr;
        if (code != 0 && string.IsNullOrWhiteSpace(text)) return null;
        return ParseCharge(text);
    }

    public bool SetChargeMode(ChargeMode mode, int? customStart, int? customStop, out string error)
    {
        error = "";
        if (!Available) { error = "Dell Optimizer (do-cli) not found"; return false; }

        var value = mode switch
        {
            ChargeMode.ExpressCharge => "ExpressCharge",
            ChargeMode.PrimarilyAC => "PrimarilyAC",
            ChargeMode.Adaptive => "Adaptive",
            ChargeMode.Custom => "Custom",
            _ => "Standard"
        };

        var (code, stdout, stderr) = CliRunner.Run(_path!, $"/configure -name=SystemPowerConfiguration.ChargingMode -value={value}");
        if (code != 0)
        {
            error = FirstUseful(stdout, stderr, $"do-cli exit {code}");
            return false;
        }

        // Custom thresholds are often applied via CCTK; Optimizer may only set mode.
        if (mode == ChargeMode.Custom && customStart is int s && customStop is int e)
        {
            // Best-effort; ignore unknown attribute failures here — CompositeHal will use CCTK.
            CliRunner.Run(_path!, $"/configure -name=SystemPowerConfiguration.CustomChargeStart -value={s}");
            CliRunner.Run(_path!, $"/configure -name=SystemPowerConfiguration.CustomChargeStop -value={e}");
        }

        return true;
    }

    private static ThermalMode? ParseThermal(string text)
    {
        if (Regex.IsMatch(text, @"\bQuiet\b", RegexOptions.IgnoreCase)) return ThermalMode.Quiet;
        if (Regex.IsMatch(text, @"\bCool\b", RegexOptions.IgnoreCase)) return ThermalMode.Cool;
        if (Regex.IsMatch(text, @"\bUltra\b", RegexOptions.IgnoreCase)) return ThermalMode.Ultra;
        if (Regex.IsMatch(text, @"\bOptimized\b", RegexOptions.IgnoreCase)) return ThermalMode.Optimized;
        return null;
    }

    private static ChargeMode? ParseCharge(string text)
    {
        if (Regex.IsMatch(text, @"ExpressCharge|Express", RegexOptions.IgnoreCase)) return ChargeMode.ExpressCharge;
        if (Regex.IsMatch(text, @"PrimarilyAC|PrimAcUse", RegexOptions.IgnoreCase)) return ChargeMode.PrimarilyAC;
        if (Regex.IsMatch(text, @"\bAdaptive\b", RegexOptions.IgnoreCase)) return ChargeMode.Adaptive;
        if (Regex.IsMatch(text, @"\bCustom\b", RegexOptions.IgnoreCase)) return ChargeMode.Custom;
        if (Regex.IsMatch(text, @"\bStandard\b", RegexOptions.IgnoreCase)) return ChargeMode.Standard;
        return null;
    }

    private static string FirstUseful(params string[] parts)
    {
        foreach (var p in parts)
            if (!string.IsNullOrWhiteSpace(p)) return p.Trim();
        return "Unknown error";
    }
}
