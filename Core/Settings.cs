using System;
using System.IO;
using System.Text.Json;

namespace StageDeck.Core;

public static class Settings
{
    private static string _path => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "StageDeck", "settings.json");

    public static int CardSize { get; set; } = 128;

    public static void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                var d = JsonSerializer.Deserialize<SettingData>(json);
                if (d != null && d.CardSize >= 80 && d.CardSize <= 260)
                    CardSize = d.CardSize;
            }
        }
        catch { }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var d = new SettingData { CardSize = CardSize };
            File.WriteAllText(_path, JsonSerializer.Serialize(d));
        }
        catch { }
    }

    private class SettingData
    {
        public int CardSize { get; set; }
    }
}
