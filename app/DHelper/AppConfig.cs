using System.Text.Json;

namespace DHelper;

internal static class AppConfig
{
    private static readonly object Gate = new();
    private static Dictionary<string, JsonElement> _data = new();
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DHelper");
    private static readonly string FilePath = Path.Combine(Dir, "config.json");
    private static System.Threading.Timer? _saveTimer;

    public static void Load()
    {
        lock (Gate)
        {
            Directory.CreateDirectory(Dir);
            if (!File.Exists(FilePath))
            {
                _data = DefaultData();
                SaveNow();
                return;
            }
            try
            {
                var json = File.ReadAllText(FilePath);
                _data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? DefaultData();
            }
            catch
            {
                var bak = FilePath + ".bak";
                if (File.Exists(bak))
                {
                    var json = File.ReadAllText(bak);
                    _data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? DefaultData();
                }
                else _data = DefaultData();
            }
        }
    }

    private static Dictionary<string, JsonElement> DefaultData()
    {
        var dict = new Dictionary<string, object>
        {
            ["thermal_mode"] = "Optimized",
            ["thermal_mode_ac"] = "Optimized",
            ["thermal_mode_dc"] = "Quiet",
            ["auto_thermal"] = true,
            ["charge_mode"] = "Custom",
            ["charge_start"] = 50,
            ["charge_stop"] = 80,
            ["auto_refresh"] = true,
            ["refresh_ac"] = 0,
            ["refresh_dc"] = 60,
            ["startup"] = false,
            ["overlay"] = false,
            ["hotkeys"] = true,
            ["fn_lock"] = false,
            ["start_count"] = 0
        };
        var json = JsonSerializer.Serialize(dict);
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
    }

    public static T Get<T>(string key, T fallback)
    {
        lock (Gate)
        {
            if (!_data.TryGetValue(key, out var el)) return fallback;
            try
            {
                return JsonSerializer.Deserialize<T>(el.GetRawText()) ?? fallback;
            }
            catch { return fallback; }
        }
    }

    public static void Set<T>(string key, T value)
    {
        lock (Gate)
        {
            var json = JsonSerializer.SerializeToElement(value);
            _data[key] = json;
            ScheduleSave();
        }
    }

    private static void ScheduleSave()
    {
        _saveTimer?.Dispose();
        _saveTimer = new System.Threading.Timer(_ => SaveNow(), null, 300, Timeout.Infinite);
    }

    public static void SaveNow()
    {
        lock (Gate)
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(FilePath))
                File.Copy(FilePath, FilePath + ".bak", true);
            File.Move(tmp, FilePath, true);
        }
    }
}
