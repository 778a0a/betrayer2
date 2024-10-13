using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SavedTerrain
{
    public MapPosition Position { get; set; }
    public Terrain Terrain { get; set; }
    public static SavedTerrain ParseCsvRow(string[] header, string line)
    {
        var values = line.Split('\t');
        var country = new SavedTerrain
        {
            Position = MapPosition.Of(
                int.Parse(values[0]),
                int.Parse(values[1])),
            Terrain = Enum.Parse<Terrain>(values[2]),
        };
        return country;
    }
}

public static class SavedTerrains
{
    public static List<SavedTerrain> FromWorld(WorldData world)
    {
        var terrains = new List<SavedTerrain>();
        foreach (var tile in world.Map.Tiles)
        {
            terrains.Add(new SavedTerrain
            {
                Position = tile.Position,
                Terrain = tile.Terrain,
            });
        }
        return terrains.OrderBy(t => t.Position.y).ThenBy(t => t.Position.x).ToList();
    }

    public static string ToCsv(List<SavedTerrain> terrains)
    {
        var sb = new StringBuilder();

        var delimiter = "\t";
        // ヘッダー
        sb.Append(nameof(SavedTerrain.Position.x)).Append(delimiter);
        sb.Append(nameof(SavedTerrain.Position.y)).Append(delimiter);
        sb.Append(nameof(SavedTerrain.Terrain)).Append(delimiter);
        sb.AppendLine();

        // 中身
        for (var i = 0; i < terrains.Count; i++)
        {
            var terrain = terrains[i];
            sb.Append(terrain.Position.x).Append(delimiter);
            sb.Append(terrain.Position.y).Append(delimiter);
            sb.Append(terrain.Terrain).Append(delimiter);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    public static List<SavedTerrain> FromCsv(string csv)
    {
        var lines = csv.Trim().Split('\n');
        var header = lines[0].Trim().Split('\t');
        var charas = new List<SavedTerrain>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            var chara = SavedTerrain.ParseCsvRow(header, line);
            charas.Add(chara);
        }
        return charas;
    }
}

