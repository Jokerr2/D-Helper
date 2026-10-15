using System.Text.RegularExpressions;

namespace DHelper.Hardware;

internal sealed class DellCctkBackend
{
    private string? _path;

    public string? Path => _path;
    public bool Available => !string.IsNullOrEmpty(_path);

    public void Probe()
    {
        _path = CliRunner.FindFirstExisting(
            @"C:\Program Files (x86)\Dell\Command Configure\X86_64\cctk.exe",
            @"C:\Program Files\Dell\Command Configure\X86_64\cctk.exe",
            @"C:\Program Files (x86)\Dell\Command Configure\cctk.exe");

        if (_path == null)
        {
            try
            {
                foreach (var root in new[]
                         {
                             @"C:\Program Files (x86)\Dell",
                             @"C:\Program Files\Dell"
                         })
                {
                    if (!Directory.Exists(root)) continue;
                    var hit = Directory.EnumerateFiles(root, "cctk.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (hit != null) { _path = hit; break; }
                }
            }
            catch { /* ignore */ }
        }
    }

    public ThermalMode? GetThermalMode()
    {
        if (!Available) return null;
        var (code, stdout, stderr) = CliRunner.Run(_path!, "--ThermalManagement");
        var text = stdout + "\n" + stderr;
        if (code != 0 && !text.Contains("ThermalManagement", StringComparison.OrdinalIgnoreCase)) return null;
        return ParseThermal(text);
    }

    public bool SetThermalMode(ThermalMode mode, out string error)
    {
        error = "";
        if (!Available) { error = "Dell Command Configure (cctk) not found"; return false; }
        var value = mode switch
        {
            ThermalMode.Quiet => "Quiet",
            ThermalMode.Cool => "Cool",
            ThermalMode.Ultra => "UltraPerformance",
            _ => "Optimized"
        };
        var (code, stdout, stderr) = CliRunner.Run(_path!, $"--ThermalManagement={value}");
        if (code == 0) return true;
        if (LooksSuccessful(stdout + "\n" + stderr, "ThermalManagement")) return true;
        error = FirstUseful(stdout, stderr, $"cctk exit {code}");
        return false;
    }

    public (ChargeMode? Mode, int? Start, int? Stop) GetCharge()
    {
        if (!Available) return (null, null, null);
        var (code, stdout, stderr) = CliRunner.Run(_path!, "--PrimaryBattChargeCfg");
        var text = stdout + "\n" + stderr;
        if (code != 0 && !text.Contains("PrimaryBattChargeCfg", StringComparison.OrdinalIgnoreCase))
            return (null, null, null);

        var custom = Regex.Match(text, @"Custom:(\d+)\s*-\s*(\d+)", RegexOptions.IgnoreCase);
        if (custom.Success)
            return (ChargeMode.Custom, int.Parse(custom.Groups[1].Value), int.Parse(custom.Groups[2].Value));

        if (Regex.IsMatch(text, @"Express", RegexOptions.IgnoreCase)) return (ChargeMode.ExpressCharge, null, null);
        if (Regex.IsMatch(text, @"PrimAcUse|Primarily", RegexOptions.IgnoreCase)) return (ChargeMode.PrimarilyAC, null, null);
        if (Regex.IsMatch(text, @"Adaptive", RegexOptions.IgnoreCase)) return (ChargeMode.Adaptive, null, null);
        if (Regex.IsMatch(text, @"Standard", RegexOptions.IgnoreCase)) return (ChargeMode.Standard, null, null);
        if (Regex.IsMatch(text, @"Custom", RegexOptions.IgnoreCase)) return (ChargeMode.Custom, null, null);
        return (null, null, null);
    }

    public bool SetChargeMode(ChargeMode mode, int? customStart, int? customStop, out string error)
    {
        error = "";
        if (!Available) { error = "Dell Command Configure (cctk) not found"; return false; }

        string arg = mode switch
        {
            ChargeMode.ExpressCharge => "Express",
            ChargeMode.PrimarilyAC => "PrimAcUse",
            ChargeMode.Adaptive => "Adaptive",
            ChargeMode.Custom => BuildCustom(customStart, customStop, out error),
            _ => "Standard"
        };
        if (arg.Length == 0) return false;

        var (code, stdout, stderr) = CliRunner.Run(_path!, $"--PrimaryBattChargeCfg={arg}");
        var text = stdout + "\n" + stderr;
        // Exit 0 = success. Also accept echoed assignment without ERROR keywords.
        if (code == 0) return true;
        if (LooksSuccessful(text, "PrimaryBattChargeCfg")) return true;
        error = FirstUseful(stdout, stderr, $"cctk exit {code}");
        return false;
    }

    public bool? GetFnLock()
    {
        if (!Available) return null;
        var (_, stdout, stderr) = CliRunner.Run(_path!, "--FnLock");
        var text = stdout + "\n" + stderr;
        if (Regex.IsMatch(text, @"FnLock=Enabled|FnLock\s*=\s*1", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(text, @"FnLock=Disabled|FnLock\s*=\s*0", RegexOptions.IgnoreCase)) return false;
        return null;
    }

    public bool SetFnLock(bool enabled, out string error)
    {
        error = "";
        if (!Available) { error = "Dell Command Configure (cctk) not found"; return false; }
        var value = enabled ? "Enabled" : "Disabled";
        var (code, stdout, stderr) = CliRunner.Run(_path!, $"--FnLock={value}");
        if (code == 0 || LooksSuccessful(stdout, "FnLock")) return true;
        error = FirstUseful(stdout, stderr, $"cctk exit {code}");
        return false;
    }

    private static string BuildCustom(int? start, int? stop, out string error)
    {
        error = "";
        var s = start ?? 50;
        var e = stop ?? 80;
        if (s < 50 || s > 95) { error = "Custom start must be 50–95"; return ""; }
        if (e < 55 || e > 100) { error = "Custom stop must be 55–100"; return ""; }
        if (e - s < 5) { error = "Stop must be at least start + 5"; return ""; }
        return $"Custom:{s}-{e}";
    }

    private static ThermalMode? ParseThermal(string text)
    {
        if (Regex.IsMatch(text, @"Quiet", RegexOptions.IgnoreCase)) return ThermalMode.Quiet;
        if (Regex.IsMatch(text, @"Cool", RegexOptions.IgnoreCase)) return ThermalMode.Cool;
        if (Regex.IsMatch(text, @"UltraPerformance|Ultra", RegexOptions.IgnoreCase)) return ThermalMode.Ultra;
        if (Regex.IsMatch(text, @"Optimized", RegexOptions.IgnoreCase)) return ThermalMode.Optimized;
        return null;
    }

    private static bool LooksSuccessful(string stdout, string key) =>
        stdout.Contains(key, StringComparison.OrdinalIgnoreCase) &&
        !stdout.Contains("ERROR", StringComparison.OrdinalIgnoreCase) &&
        !stdout.Contains("not supported", StringComparison.OrdinalIgnoreCase);

    private static string FirstUseful(params string[] parts)
    {
        foreach (var p in parts)
            if (!string.IsNullOrWhiteSpace(p)) return p.Trim();
        return "Unknown error";
    }
}
