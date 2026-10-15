using System.Diagnostics;
using System.Text;

namespace DHelper.Hardware;

internal static class CliRunner
{
    public static (int ExitCode, string StdOut, string StdErr) Run(string fileName, string arguments, int timeoutMs = 8000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            // Avoid UTF8 decoder alloc cost when CLI is ASCII-heavy
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default
        };

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder(256);
        var stderr = new StringBuilder(128);
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(timeoutMs))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            return (-1, stdout.ToString(), "Timed out");
        }

        // Ensure async readers flush
        process.WaitForExit(500);
        return (process.ExitCode, stdout.ToString().Trim(), stderr.ToString().Trim());
    }

    public static string? FindFirstExisting(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return path;
        }
        return null;
    }
}
