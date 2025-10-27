using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SavedCountryRelation
{
    public int CountryA { get; set; }
    public int CountryB { get; set; }
    public float Relation { get; set; }
    public string Memo { get; set; }

    public static SavedCountryRelation ParseCsvRow(string[] header, string line)
    {
        var values = line.Split('\t');
        var country = new SavedCountryRelation
        {
            CountryA = int.Parse(values[0]),
            CountryB = int.Parse(values[1]),
            Relation = float.Parse(values[2]),
            Memo = values[3],
        };
        return country;
    }
}

public static class SavedCountryRelations
{
    public static List<SavedCountryRelation> FromWorld(WorldData world)
    {
        var rels = new List<SavedCountryRelation>();
        var countries = world.Countries.OrderBy(c => c.Id);
        foreach (var a in countries)
        {
            foreach (var b in countries)
            {
                if (a == b) continue;
                if (a.Id > b.Id) continue;
                rels.Add(new()
                {
                    CountryA = a.Id,
                    CountryB = b.Id,
                    Relation = a.GetRelation(b),
                    Memo = $"{a.Ruler.Name} - {b.Ruler.Name}",
                });
            }
        }
        return rels;
    }

    public static string ToCsv(List<SavedCountryRelation> rels)
    {
        var sb = new StringBuilder();

        var delimiter = "\t";
        // ヘッダー
        sb.Append("a").Append(delimiter);
        sb.Append("b").Append(delimiter);
        sb.Append("relation").Append(delimiter);
        sb.Append("memo").Append(delimiter);
        sb.AppendLine();

        // 中身
        foreach (var rel in rels)
        {
            // 50なら省略する。
            if (Mathf.Approximately(rel.Relation, 50f))
            {
                continue;
            }

            sb.Append(rel.CountryA).Append(delimiter);
            sb.Append(rel.CountryB).Append(delimiter);
            sb.Append(rel.Relation.ToString("000.000")).Append(delimiter);
            sb.Append(rel.Memo).Append(delimiter);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static List<SavedCountryRelation> FromCsv(string csv)
    {
        var lines = csv.Trim().Split('\n');
        var header = lines[0].Trim().Split('\t');
        var rels = new List<SavedCountryRelation>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            var chara = SavedCountryRelation.ParseCsvRow(header, line);
            rels.Add(chara);
        }
        return rels;
    }
}
