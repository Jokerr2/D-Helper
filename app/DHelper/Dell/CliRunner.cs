using System.Diagnostics;
using System.Text;

namespace GHelper.Dell;

internal static class CliRunner
{
    private static readonly object Gate = new();

    /// <summary>
    /// CCTK on Dell Pro often needs 20–40s per call under load; 15s caused false "unsupported".
    /// </summary>
    public static (int ExitCode, string StdOut, string StdErr) Run(string fileName, string arguments, int timeoutMs = 60000)
    {
        lock (Gate)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // CCTK loads sibling DLLs from its install folder
            try
            {
                var dir = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrEmpty(dir))
                    psi.WorkingDirectory = dir;
            }
            catch { /* ignore */ }

            using var process = new Process { StartInfo = psi };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(timeoutMs))
            {
                try { process.Kill(true); } catch { /* ignore */ }
                Logger.WriteLine($"CLI timeout ({timeoutMs}ms): {Path.GetFileName(fileName)} {arguments}");
                return (-1, "", "Timed out");
            }

            if (!Task.WaitAll(new Task[] { stdoutTask, stderrTask }, 2000))
            {
                var partial = (process.ExitCode, Safe(stdoutTask), Safe(stderrTask));
                LogResult(fileName, arguments, partial.Item1, partial.Item2, partial.Item3, sw.ElapsedMilliseconds);
                return partial;
            }

            var result = (process.ExitCode, stdoutTask.Result.Trim(), stderrTask.Result.Trim());
            LogResult(fileName, arguments, result.Item1, result.Item2, result.Item3, sw.ElapsedMilliseconds);
            return result;
        }
    }

    public static Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        string fileName, string arguments, int timeoutMs = 60000) =>
        Task.Run(() => Run(fileName, arguments, timeoutMs));

    private static void LogResult(string file, string args, int code, string stdout, string stderr, long ms = 0)
    {
        var tip = FirstLine(stderr);
        if (string.IsNullOrEmpty(tip)) tip = FirstLine(stdout);
        if (string.IsNullOrEmpty(tip)) tip = "(no output)";
        Logger.WriteLine($"CLI {(code == 0 ? "ok" : "exit " + code)} {ms}ms: {Path.GetFileName(file)} {args} → {tip}");
    }

    private static string FirstLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var line = text.Split('\n', 2)[0].Trim();
        return line.Length > 160 ? line[..160] + "…" : line;
    }

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
