using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SavedCountries
{
    public static List<SavedCountry> FromWorld(WorldData world)
    {
        var countries = new List<SavedCountry>();
        for (int i = 0; i < world.Countries.Count; i++)
        {
            var original = world.Countries[i];
            var country = new SavedCountry { Data = original, Memo = original.Ruler.Name, };
            countries.Add(country);
        }
        return countries;
    }

    public static List<SavedCountry> FromCsv(string csv)
    {
        var lines = csv.Trim().Split('\n');
        var header = lines[0].Trim().Split('\t');
        var charas = new List<SavedCountry>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            var chara = SavedCountry.ParseCsvRow(header, line);
            charas.Add(chara);
        }
        return charas;
    }

    public static string ToCsv(List<SavedCountry> countries)
    {
        var sb = new StringBuilder();

        var delimiter = "\t";
        // ヘッダー
        sb.Append(nameof(Country.Id)).Append(delimiter);
        sb.Append(nameof(Country.ColorIndex)).Append(delimiter);
        sb.Append(nameof(SavedCountry.Memo)).Append(delimiter);
        sb.AppendLine();

        // 中身
        for (var i = 0; i < countries.Count; i++)
        {
            var country = countries[i];
            sb.Append(country.Data.Id).Append(delimiter);
            sb.Append(country.Data.ColorIndex).Append(delimiter);
            sb.Append(country.Memo).Append(delimiter);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}

public class SavedCountry
{
    public Country Data { get; set; }
    public string Memo { get; set; }

    public static SavedCountry ParseCsvRow(string[] header, string line)
    {
        var values = line.Split('\t');
        var country = new SavedCountry
        {
            Data = new Country()
            {
                Id = int.Parse(values[0]),
                ColorIndex = int.Parse(values[1]),
            },
            Memo = values[2],
        };
        return country;
    }
}
