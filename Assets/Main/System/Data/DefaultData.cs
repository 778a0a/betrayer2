using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.Utilities;
using Random = UnityEngine.Random;

public class DefaultData
{

    public static WorldData Create(GameMap map)
    {
        Random.InitState(42);

        var countries = new List<Country>
        {
            new() { Id = 0, ColorIndex = 17 }, // レオナルド
            new() { Id = 1, ColorIndex = 1 }, // セレスティア
            new() { Id = 2, ColorIndex = 19 }, // ナオミ
            new() { Id = 3, ColorIndex = 3 }, // エルシア
            new() { Id = 4, ColorIndex = 34 }, // ドラゴミール
            new() { Id = 5, ColorIndex = 51 }, // マーガレット
            new() { Id = 6, ColorIndex = 0 }, // フレデリック
            new() { Id = 7, ColorIndex = 4 }, // ザンダー
            new() { Id = 8, ColorIndex = 58 }, // ウィリアム
            new() { Id = 9, ColorIndex = 36 }, // ザフィール
            new() { Id = 10, ColorIndex = 24 }, // ナディア
            new() { Id = 11, ColorIndex = 6 }, // カリオペ
            new() { Id = 12, ColorIndex = 26 }, // フリージア
            new() { Id = 13, ColorIndex = 2 }, // セレスト
            new() { Id = 14, ColorIndex = 63 }, // ケルベロス
            new() { Id = 15, ColorIndex = 27 }, // イリス
            new() { Id = 16, ColorIndex = 10 }, // アトラス
        };

        var castleTiles = new Dictionary<MapPosition, int>()
        {
            { new() { x = -10, y = 14 }, 2 },
            { new() { x = -4, y = 14 }, 2 },
            { new() { x = 2, y = 14 }, 63 },
            { new() { x = 5, y = 15 }, 27 },
            { new() { x = 8, y = 13 }, 27 },
            { new() { x = 12, y = 14 }, 10 },
            { new() { x = -1, y = 12 }, 24 },
            { new() { x = -4, y = 10 }, 24 },
            { new() { x = 11, y = 10 }, 26 },
            { new() { x = -10, y = 9 }, 2 },
            { new() { x = 4, y = 9 }, 6 },
            { new() { x = -6, y = 7 }, 24 },
            { new() { x = 8, y = 7 }, 26 },
            { new() { x = -9, y = 5 }, 4 },
            { new() { x = 0, y = 5 }, 6 },
            { new() { x = 5, y = 5 }, 6 },
            { new() { x = -4, y = 4 }, 4 },
            { new() { x = 11, y = 3 }, 36 },
            { new() { x = -1, y = 2 }, 58 },
            { new() { x = 8, y = 2 }, 0 },
            { new() { x = -8, y = 1 }, 4 },
            { new() { x = 6, y = 0 }, 0 },
            { new() { x = -5, y = -1 }, 58 },
            { new() { x = 2, y = -1 }, 0 },
            { new() { x = -8, y = -3 }, 51 },
            { new() { x = 5, y = -3 }, 0 },
            { new() { x = 9, y = -3 }, 34 },
            { new() { x = 0, y = -4 }, 1 },
            { new() { x = -4, y = -5 }, 1 },
            { new() { x = -10, y = -6 }, 17 },
            { new() { x = 3, y = -6 }, 19 },
            { new() { x = 8, y = -8 }, 34 },
            { new() { x = -6, y = -9 }, 1 },
            { new() { x = -1, y = -9 }, 19 },
            { new() { x = -10, y = -10 }, 17 },
            { new() { x = 2, y = -11 }, 19 },
            { new() { x = 11, y = -11 }, 3 },
        };

        // 各タイルのパラメーター上限値を設定する。
        foreach (var tile in map.Tiles)
        {
            if (tile.Castle != null)
            {
                tile.Castle.StrengthMax = 999;
            }
            if (tile.Town != null)
            {
                tile.Town.FoodIncomeMax = GameMapTile.TileFoodMax(tile);
                tile.Town.GoldIncomeMax += GameMapTile.TileGoldMax(tile);
            }
        }

        foreach (var castleData in castleTiles)
        {
            var countryIndex = castleData.Value;
            var pos = castleData.Key;
            var tile = map.GetTile(pos);

            tile.Castle.Exists = true;
            tile.Castle.SetCountry(countries.Find(c => c.ColorIndex == countryIndex));
            tile.Castle.Strength = Random.Range(0, tile.Castle.StrengthMax * 0.8f);
            tile.Castle.Food = 500; // tile.Castle.FoodIncomeMax;
            tile.Castle.Gold = 30;
            tile.Castle.AddTown(tile.Town);
            tile.Town.Exists = true;
            tile.Town.FoodIncome = Random.Range(0, tile.Town.FoodIncomeMax * 0.8f);
            tile.Town.GoldIncome = Random.Range(0, tile.Town.GoldIncomeMax * 0.8f);
        }

        var oldcsv = Resources.Load<TextAsset>("Scenarios/01/character_data").text;
        var oldcharas = OldSavedCharacters.Deserialize(oldcsv);
        var characters = new List<Character>();
        foreach (var chara in oldcharas)
        {
            characters.Add(chara.Character);
            if (chara.IsFree) continue;
            
            var country = countries[chara.CountryId];
            if (chara.IsRuler)
            {
                country.Ruler = chara.Character;
            }
            country.Castles.RandomPick().Members.Add(chara.Character);
        }

        var world = new WorldData
        {
            Castles = countries.SelectMany(c => c.Castles).ToArray(),
            Countries = countries.ToArray(),
            Characters = characters.ToArray(),
            Forces = new(),
        };
        return world;
    }
}


public static class OldSavedCharacters
{
    public static List<OldSavedCharacter> Extract(WorldData world)
    {
        var charas = new List<OldSavedCharacter>();
        for (int i = 0; i < world.Characters.Length; i++)
        {
            var character = world.Characters[i];
            var country = world.Countries.FirstOrDefault(c => c.Members.Contains(character));
            var memberIndex = country?.Members.TakeWhile(c => c != character).Count() ?? -1;
            var chara = new OldSavedCharacter
            {
                Character = character,
                CountryId = country != null ? country.Id : -1,
                MemberOrderIndex = memberIndex,
            };
            charas.Add(chara);
        }
        charas = charas.OrderBy(c => c.CountryId).ThenBy(c => c.MemberOrderIndex).ToList();
        return charas;
    }

    public static List<OldSavedCharacter> Deserialize(string csv)
    {
        var lines = csv.Trim().Split('\n');
        var header = lines[0].Trim().Split('\t');
        var charas = new List<OldSavedCharacter>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            var chara = OldSavedCharacter.Deserialize(header, line);
            charas.Add(chara);
        }
        return charas;
    }
}

public class OldSavedCharacter
{
    public int CountryId { get; set; }
    public int MemberOrderIndex { get; set; }
    public Character Character { get; set; }

    public bool IsRuler => !IsFree && MemberOrderIndex == 0;
    public bool IsFree => CountryId == -1;

    private static readonly string EmptySlotMark = "E";

    public static string CreateCsv(List<OldSavedCharacter> charas)
    {
        var json = JsonConvert.SerializeObject(charas);
        var list = JsonConvert.DeserializeObject<List<JObject>>(json);
        var sb = new StringBuilder();

        bool IsTargetProperty(JProperty prop)
        {
            return
                !prop.Name.Equals(nameof(global::Character.debugMemo)) &&
                !prop.Name.Equals(nameof(global::Character.debugImagePath));
        }

        var delimiter = "\t";
        // ヘッダー
        sb.Append(nameof(CountryId)).Append(delimiter);
        sb.Append(nameof(MemberOrderIndex)).Append(delimiter);
        foreach (JProperty prop in list[0][nameof(Character)])
        {
            if (!IsTargetProperty(prop)) continue;
            sb.Append(prop.Name).Append(delimiter);
        }
        sb.AppendLine();

        // 中身
        for (var i = 0; i < charas.Count; i++)
        {
            var chara = charas[i].Character;
            var obj = list[i];

            sb.Append(obj[nameof(CountryId)]).Append(delimiter);
            sb.Append(obj[nameof(MemberOrderIndex)]).Append(delimiter);
            foreach (JProperty prop in obj[nameof(Character)])
            {
                if (!IsTargetProperty(prop)) continue;

                if (prop.Name.Equals(nameof(global::Character.Soldiers)))
                {
                    var sbsub = new StringBuilder();
                    foreach (var s in chara.Soldiers)
                    {
                        if (s.IsEmptySlot)
                        {
                            sbsub.Append($"|{EmptySlotMark}");
                        }
                        else
                        {
                            sbsub.Append($"|{s.Level},");
                            sbsub.Append($"{(s.Experience == 0 ? "" : s.Experience)},");
                            sbsub.Append($"{(s.HpFloat == s.MaxHp ? "" : s.HpFloat.ToString("0.#"))}");
                        }
                    }
                    sb.Append(sbsub.ToString()).Append(delimiter);
                }
                else
                {
                    sb.Append(JsonConvert.SerializeObject(prop.Value)).Append(delimiter);
                }
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    public static OldSavedCharacter Deserialize(string[] header, string line)
    {
        var values = line.Split('\t');
        var chara = new OldSavedCharacter
        {
            CountryId = int.Parse(values[0]),
            MemberOrderIndex = int.Parse(values[1]),
        };
        var character = new Character();
        var characterType = character.GetType();
        for (int j = 2; j < header.Length; j++)
        {
            var propName = header[j];
            var prop = characterType.GetProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"Property not found: {propName}");
                continue;
            }

            if (propName.Equals(nameof(global::Character.Soldiers)))
            {
                var field = values[j];
                // 新しい形式の場合
                if (!field.StartsWith("{"))
                {
                    var soldiersRaw = field.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    var soldiers = new Soldiers(soldiersRaw.Length);
                    for (int k = 0; k < soldiersRaw.Length; k++)
                    {
                        var soldier = soldiersRaw[k];
                        if (soldier != EmptySlotMark)
                        {
                            var values2 = soldier.Split(',');
                            var s = soldiers[k];
                            s.IsEmptySlot = false;
                            s.Level = int.Parse(values2[0]);
                            s.Experience = values2[1] != "" ? int.Parse(values2[1]) : 0;
                            s.HpFloat = values2[2] != "" ? float.Parse(values2[2]) : s.MaxHp;
                        }
                    }
                    character.Soldiers = soldiers;
                    continue;
                }
                else
                {
                    var obj = JsonConvert.DeserializeObject(field) as JToken;
                    var sols = obj["Soldiers"] as JArray;
                    character.Soldiers = new Soldiers(sols.Select(s => s.ToObject<Soldier>()));
                    continue;
                }
            }

            // has setter
            if (prop.CanWrite)
            {
                var type = prop.PropertyType;
                var value = JsonConvert.DeserializeObject(values[j], type);
                prop.SetValue(character, value);
            }
        }
        chara.Character = character;
        return chara;
    }
}
