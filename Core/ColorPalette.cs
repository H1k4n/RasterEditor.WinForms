using System.Text.Json;

namespace RasterEditor.WinFormsDemo.Core;

public class ColorPalette
{
    public string Name { get; set; }
    public List<Color> Colors { get; set; }

    public ColorPalette(string name, List<Color> colors)
    {
        Name = name;
        Colors = colors ?? new List<Color>();
    }

    public static List<ColorPalette> LoadPalettes()
    {
        var palettesDir = Path.Combine(AppContext.BaseDirectory, "Palettes");
        if (!Directory.Exists(palettesDir))
            return GetDefaultPalettes();

        var palettes = new List<ColorPalette>();
        foreach (var file in Directory.GetFiles(palettesDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<PaletteData>(json);
                if (data?.Name != null && data.Colors != null)
                {
                    var colors = data.Colors.Select(c => Color.FromArgb(c)).ToList();
                    palettes.Add(new ColorPalette(data.Name, colors));
                }
            }
            catch { }
        }

        return palettes.Count > 0 ? palettes : GetDefaultPalettes();
    }

    public void Save()
    {
        var palettesDir = Path.Combine(AppContext.BaseDirectory, "Palettes");
        Directory.CreateDirectory(palettesDir);

        var data = new PaletteData
        {
            Name = Name,
            Colors = Colors.Select(c => c.ToArgb()).ToList()
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        var fileName = Path.Combine(palettesDir, $"{Name}.json");
        File.WriteAllText(fileName, json);
    }

    /// <summary>Deletes the JSON file for a palette with this name (if present).</summary>
    public static void TryDeletePaletteFile(string paletteName)
    {
        if (string.IsNullOrWhiteSpace(paletteName)) return;
        var safe = Path.GetFileNameWithoutExtension(paletteName.Trim());
        if (string.IsNullOrEmpty(safe)) return;
        var path = Path.Combine(AppContext.BaseDirectory, "Palettes", $"{safe}.json");
        if (File.Exists(path))
            File.Delete(path);
    }

    private static List<ColorPalette> GetDefaultPalettes()
    {
        var colors = new Color[]
        {
            Color.Black, Color.White, Color.Gray, Color.Silver,
            Color.Red, Color.Orange, Color.Yellow, Color.LimeGreen,
            Color.Cyan, Color.DodgerBlue, Color.Blue, Color.Purple,
            Color.Brown, Color.Pink, Color.Gold, Color.Teal
        };
        return new List<ColorPalette>
        {
            new ColorPalette("Default", colors.ToList())
        };
    }

    private class PaletteData
    {
        public string? Name { get; set; }
        public List<int>? Colors { get; set; }
    }
}
