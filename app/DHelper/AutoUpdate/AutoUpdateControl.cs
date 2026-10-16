using GHelper.Helpers;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GHelper.AutoUpdate
{
    public class AutoUpdateControl
    {
        const string ReleasesPage = "https://github.com/Jokerr2/D-Helper/releases";
        const string LatestApi = "https://api.github.com/repos/Jokerr2/D-Helper/releases/latest";
        const string UserAgent = "D-Helper App";

        SettingsForm settings;

        public string versionUrl = ReleasesPage;
        public bool update = false;

        static long lastUpdate;

        public AutoUpdateControl(SettingsForm settingsForm)
        {
            settings = settingsForm;
            var appVersion = new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            settings.SetVersionLabel(Properties.Strings.VersionLabel + $": {appVersion.Major}.{appVersion.Minor}.{appVersion.Build}");
        }

        public void CheckForUpdates()
        {
            // Run update once per 12 hours
            if (Math.Abs(DateTimeOffset.Now.ToUnixTimeSeconds() - lastUpdate) < 43200) return;
            lastUpdate = DateTimeOffset.Now.ToUnixTimeSeconds();

            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                CheckForUpdatesAsync();
            });
        }

        public void Update()
        {
            if (update)
            {
                Task.Run(() => CheckForUpdatesAsync(true));
            }
            else
            {
                LoadReleases();
            }
        }

        public void LoadReleases()
        {
            try
            {
                Process.Start(new ProcessStartInfo(versionUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Failed to open releases page:" + ex.Message);
            }
        }

        async void CheckForUpdatesAsync(bool force = false)
        {
            if (AppConfig.Is("skip_updates")) return;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                var json = await httpClient.GetStringAsync(LatestApi);
                var config = JsonSerializer.Deserialize<JsonElement>(json);
                var tag = config.GetProperty("tag_name").ToString().TrimStart('v', 'V');
                var assets = config.GetProperty("assets");

                string? url = PickAssetUrl(assets);
                if (url is null)
                {
                    Logger.WriteLine("Update check: no downloadable assets on latest release");
                    return;
                }

                var gitVersion = new Version(NormalizeVersion(tag));
                var appVersion = new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString());

                if (gitVersion.CompareTo(appVersion) > 0)
                {
                    versionUrl = url;
                    update = true;
                    settings.SetVersionLabel(Properties.Strings.DownloadUpdate + $": {appVersion.Major}.{appVersion.Minor}.{appVersion.Build} → {tag}", true);

                    string[] args = Environment.GetCommandLineArgs();
                    if (force || (args.Length > 1 && args[1] == "autoupdate"))
                    {
                        AutoUpdate(url);
                        return;
                    }

                    if (AppConfig.GetString("skip_version") != tag)
                    {
                        DialogResult dialogResult = settings.ShowMessage(
                            Properties.Strings.DownloadUpdate + $": D-Helper {tag}?",
                            "Update",
                            MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.Yes)
                            AutoUpdate(url);
                        else
                            AppConfig.Set("skip_version", tag);
                    }
                }
                else
                {
                    Logger.WriteLine($"Latest version {appVersion} (GitHub {tag})");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Failed to check for updates:" + ex.Message);
            }
        }

        static string? PickAssetUrl(JsonElement assets)
        {
            string? zipUrl = null;
            string? firstUrl = null;

            for (int i = 0; i < assets.GetArrayLength(); i++)
            {
                string name = assets[i].GetProperty("name").ToString();
                string download = assets[i].GetProperty("browser_download_url").ToString();
                firstUrl ??= download;

                if (name.Equals("DHelper.exe", StringComparison.OrdinalIgnoreCase))
                    return download;
                if (zipUrl is null && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    zipUrl = download;
            }

            return zipUrl ?? firstUrl;
        }

        static string NormalizeVersion(string tag)
        {
            // Ensure 4-part Version parse: 1.1.0 → 1.1.0.0
            var parts = tag.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string maj = parts.Length > 0 ? parts[0] : "0";
            string min = parts.Length > 1 ? parts[1] : "0";
            string build = parts.Length > 2 ? parts[2] : "0";
            string rev = parts.Length > 3 ? parts[3] : "0";
            return $"{maj}.{min}.{build}.{rev}";
        }

        public static string EscapeString(string input)
        {
            return Regex.Replace(Regex.Replace(input, @"\[|\]", "`$0"), @"\'", "''");
        }

        async void AutoUpdate(string requestUri)
        {
            Uri uri = new Uri(requestUri);
            string remoteName = Path.GetFileName(uri.LocalPath);
            bool isZip = remoteName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

            string exeLocation = Application.ExecutablePath;
            string exeDir = Path.GetDirectoryName(exeLocation)!;
            string exeName = Path.GetFileName(exeLocation);
            // Never download over the locked running exe
            string assetName = isZip ? remoteName : "DHelper.update.exe";
            string downloadPath = Path.Combine(exeDir, assetName);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            Logger.WriteLine(requestUri);
            Logger.WriteLine(exeDir);
            Logger.WriteLine(assetName);
            Logger.WriteLine(exeName);

            try
            {
                var bytes = await client.GetByteArrayAsync(uri);
                File.WriteAllBytes(downloadPath, bytes);
                Logger.WriteLine($"Downloaded {bytes.Length}b: {downloadPath} (exists={File.Exists(downloadPath)}, size={new FileInfo(downloadPath).Length})");
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex.Message);
                if (!ProcessHelper.IsUserAdministrator())
                {
                    ProcessHelper.RunAsAdmin("autoupdate");
                    Application.Exit();
                }
                else
                {
                    LoadReleases();
                }
                return;
            }

            string install;
            if (isZip)
            {
                install = $"Expand-Archive \"{EscapeString(assetName)}\" -DestinationPath . -Force; Remove-Item \"{EscapeString(assetName)}\" -Force; \".\\{EscapeString(exeName)}\"";
            }
            else
            {
                string target = EscapeString(exeName);
                string src = EscapeString(assetName);
                install = $"Copy-Item \"{src}\" -Destination \"{target}\" -Force; Remove-Item \"{src}\" -Force -ErrorAction SilentlyContinue; \".\\{target}\"";
            }

            string command =
                $"$ErrorActionPreference = \"Stop\"; Set-Location -Path '{EscapeString(exeDir)}'; " +
                $"Wait-Process -Name \"DHelper\" -ErrorAction SilentlyContinue; " +
                install + "; ";
            Logger.WriteLine(command);

            try
            {
                var cmd = new Process();
                cmd.StartInfo.WorkingDirectory = exeDir;
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.FileName = "powershell";
                cmd.StartInfo.Arguments = command;
                if (ProcessHelper.IsUserAdministrator()) cmd.StartInfo.Verb = "runas";
                cmd.Start();
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex.Message);
            }

            Application.Exit();
        }
    }
}
