using System.Diagnostics;
using System.Text;

namespace DHelper.Hardware;

internal static class CliRunner
{
    public static (int ExitCode, string StdOut, string StdErr) Run(string fileName, string arguments, int timeoutMs = 5000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.ASCII,
            StandardErrorEncoding = Encoding.ASCII
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Sync read on caller thread (always a background thread from ApplyAsync).
        // Faster and more reliable than BeginOutputReadLine + double WaitForExit.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMs))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            return (-1, "", "Timed out");
        }

        // Brief wait for stream flush after exit
        if (!Task.WaitAll(new Task[] { stdoutTask, stderrTask }, 1000))
            return (process.ExitCode, Safe(stdoutTask), Safe(stderrTask));

        return (process.ExitCode, stdoutTask.Result.Trim(), stderrTask.Result.Trim());
    }

    public static Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        string fileName, string arguments, int timeoutMs = 5000) =>
        Task.Run(() => Run(fileName, arguments, timeoutMs));

    private static string Safe(Task<string> t)
    {
        try { return t.IsCompletedSuccessfully ? t.Result.Trim() : ""; }
        catch { return ""; }
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
