using System.Text.RegularExpressions;

namespace GHelper.Dell;

internal static class DellErrors
{
    public static string Humanize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Change failed";

        var text = Regex.Replace(raw, @"\s+", " ").Trim();

        if (text.Contains("ChargingMode", StringComparison.OrdinalIgnoreCase) &&
            text.Contains("denied", StringComparison.OrdinalIgnoreCase))
            return "Battery charge is locked by Dell/IT policy. Use CCTK or BIOS Battery settings.";

        if (text.Contains("denied", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Access to internal settings", StringComparison.OrdinalIgnoreCase))
            return "Setting locked by Dell Optimizer or IT policy.";

        if (text.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("privileges", StringComparison.OrdinalIgnoreCase))
            return "Run D-Helper as Administrator.";

        if (text.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Authentication", StringComparison.OrdinalIgnoreCase))
            return "BIOS password required — unlock in BIOS or Dell Command Configure.";

        if (text.Contains("not supported", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("unsupported", StringComparison.OrdinalIgnoreCase))
            return "This setting is not supported on this laptop.";

        if (text.Contains("Timed out", StringComparison.OrdinalIgnoreCase))
            return "Dell tool timed out — try again.";

        // Keep toast short
        return text.Length > 90 ? text[..90] + "…" : text;
    }
}
