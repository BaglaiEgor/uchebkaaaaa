using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace uchebkaaa.Services;

/// <summary>
/// Хранит позиции значков планировки цехов в JSON-файле (без БД).
/// </summary>
public static class WorkshopLayoutStorage
{
    private static readonly string FilePath = Path.Combine(
        AppContext.BaseDirectory, "workshop_layout.json");

    private static Dictionary<string, List<IconPosition>>? _cache;

    public static IReadOnlyList<string> WorkshopNames { get; } = new[]
    {
        "Заготовительный цех",
        "Механический цех",
        "Покрасочный цех",
        "Сборочный цех",
        "Упаковочный цех"
    };

    /// <summary>
    /// URI ресурса изображения плана цеха (avares).
    /// </summary>
    public static string GetPlanImageUri(string workshopName) =>
        $"avares://uchebkaaa/Assets/Images/Workshops/{workshopName}.png";

    public static IReadOnlyList<(string Type, double X, double Y)> LoadIcons(string workshopName)
    {
        var data = LoadData();
        if (!data.TryGetValue(workshopName, out var list))
            return Array.Empty<(string, double, double)>();

        var result = new List<(string, double, double)>();
        foreach (var p in list)
            result.Add((p.Type, p.X, p.Y));
        return result;
    }

    public static void SaveIcons(string workshopName, IReadOnlyList<(string Type, double X, double Y)> icons)
    {
        var data = LoadData();
        var list = new List<IconPosition>();
        foreach (var (type, x, y) in icons)
            list.Add(new IconPosition { Type = type, X = x, Y = y });
        data[workshopName] = list;
        SaveData(data);
    }

    private static Dictionary<string, List<IconPosition>> LoadData()
    {
        if (_cache != null)
            return _cache;

        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, List<IconPosition>>>(json);
                _cache = loaded ?? new Dictionary<string, List<IconPosition>>();
            }
            else
            {
                _cache = new Dictionary<string, List<IconPosition>>();
            }
        }
        catch
        {
            _cache = new Dictionary<string, List<IconPosition>>();
        }

        return _cache;
    }

    private static void SaveData(Dictionary<string, List<IconPosition>> data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
            _cache = data;
        }
        catch { }
    }

    private class IconPosition
    {
        public string Type { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
    }
}
