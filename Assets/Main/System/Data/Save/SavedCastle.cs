using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SavedCastle
{
    public int CountryId { get; set; }
    public Castle Data { get; set; }
    public List<Town> Towns { get; set; }
    public string Memo { get; set; }
}

public static class SavedCastles
{
    public static List<SavedCastle> FromWorld(WorldData world)
    {
        var countries = new List<SavedCastle>();
        for (int i = 0; i < world.Castles.Count; i++)
        {
            var original = world.Castles[i];
            var country = new SavedCastle
            {
                CountryId = original.Country.Id,
                Data = original,
                Towns = original.Towns,
                Memo = $"{original.Country.Ruler.Name}",
            };
            countries.Add(country);
        }
        return countries;
    }

    public static string ToCsv(List<SavedCastle> castles)
    {
        var json = JsonConvert.SerializeObject(castles);
        var list = JsonConvert.DeserializeObject<List<JObject>>(json);
        var sb = new StringBuilder();

        bool IsTargetProperty(JProperty prop)
        {
            return true;
        }

        var delimiter = "\t";
        // ヘッダー
        sb.Append(nameof(SavedCastle.CountryId)).Append(delimiter);
        foreach (JProperty prop in list[0][nameof(SavedCastle.Data)])
        {
            if (!IsTargetProperty(prop)) continue;
            sb.Append(prop.Name).Append(delimiter);
        }
        sb.Append(nameof(SavedCastle.Towns)).Append(delimiter);
        sb.Append(nameof(SavedCastle.Memo)).Append(delimiter);
        sb.AppendLine();

        // 中身
        for (var i = 0; i < castles.Count; i++)
        {
            var castle = castles[i].Data;
            var obj = list[i];

            sb.Append(JsonConvert.SerializeObject(castles[i].CountryId)).Append(delimiter);
            foreach (JProperty prop in obj[nameof(SavedCastle.Data)])
            {
                if (!IsTargetProperty(prop)) continue;
                sb.Append(JsonConvert.SerializeObject(prop.Value)).Append(delimiter);
            }
            sb.Append(JsonConvert.SerializeObject(castles[i].Towns)).Append(delimiter);
            sb.Append(JsonConvert.SerializeObject(castles[i].Memo)).Append(delimiter);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    public static List<SavedCastle> FromCsv(string csv)
    {
        var lines = csv.Trim().Split('\n');
        var header = lines[0].Trim().Split('\t');
        var charas = new List<SavedCastle>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            var chara = ParseCsvRow(header, line);
            charas.Add(chara);
        }
        return charas;

        static SavedCastle ParseCsvRow(string[] header, string line)
        {
            var values = line.Split('\t');

            var castle = new SavedCastle()
            {
                Data = new Castle(),
            };
            var savedCastleType = castle.GetType();
            var castleType = castle.Data.GetType();
            for (int i = 0; i < header.Length; i++)
            {
                var propName = header[i];

                var savedProp = savedCastleType.GetProperty(propName);
                if (savedProp?.CanWrite ?? false)
                {
                    var type = savedProp.PropertyType;
                    var value = JsonConvert.DeserializeObject(values[i], type);
                    savedProp.SetValue(castle, value);
                    continue;
                }

                var prop = castleType.GetProperty(propName);
                if (prop == null)
                {
                    Debug.LogWarning($"Property not found: {propName}");
                    continue;
                }

                if (prop.CanWrite)
                {
                    var type = prop.PropertyType;
                    var value = JsonConvert.DeserializeObject(values[i], type);
                    prop.SetValue(castle.Data, value);
                }
            }
            return castle;
        }
    }
}
