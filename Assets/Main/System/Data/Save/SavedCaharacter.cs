using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SavedCharacter
{
    public int CountryId { get; set; }
    public int MemberOrderIndex { get; set; }
    public int CastleId { get; set; }
    public Character Character { get; set; }

    public bool IsRuler => !IsFree && MemberOrderIndex == 0;
    public bool IsFree => CountryId == -1;
}

public static class SavedCharacters
{
    public static List<SavedCharacter> FromWorld(WorldData world, bool retainFreeCharaCastleRandom)
    {
        var charas = new List<SavedCharacter>();
        for (int i = 0; i < world.Characters.Count; i++)
        {
            var character = world.Characters[i];
            var country = character.Country;
            var memberIndex =
                country == null ? -1 :
                country.Ruler == character ? 0 :
                country.Vassals.OrderByDescending(m => m.Contribution).ToList().IndexOf(character) + 1;
            var chara = new SavedCharacter
            {
                Character = character,
                CountryId = country != null ? country.Id : -1,
                MemberOrderIndex = memberIndex,
                CastleId = retainFreeCharaCastleRandom && character.IsFree ?
                    -1 :
                    character.Castle.Id,
            };
            charas.Add(chara);
        }
        charas = charas.OrderBy(c => c.CountryId).ThenBy(c => c.MemberOrderIndex).ToList();
        return charas;
    }

    public static string ToCsv(List<SavedCharacter> charas)
    {
        var json = JsonConvert.SerializeObject(charas);
        var list = JsonConvert.DeserializeObject<List<JObject>>(json);
        var sb = new StringBuilder();

        var delimiter = "\t";
        // ヘッダー
        sb.Append(nameof(SavedCharacter.CountryId)).Append(delimiter);
        sb.Append(nameof(SavedCharacter.MemberOrderIndex)).Append(delimiter);
        sb.Append(nameof(SavedCharacter.CastleId)).Append(delimiter);
        foreach (JProperty prop in list[0][nameof(Character)])
        {
            sb.Append(prop.Name).Append(delimiter);
        }
        sb.AppendLine();

        // 中身
        for (var i = 0; i < charas.Count; i++)
        {
            var chara = charas[i].Character;
            var obj = list[i];

            sb.Append(obj[nameof(SavedCharacter.CountryId)]).Append(delimiter);
            sb.Append(obj[nameof(SavedCharacter.MemberOrderIndex)]).Append(delimiter);
            sb.Append(obj[nameof(SavedCharacter.CastleId)]).Append(delimiter);
            foreach (JProperty prop in obj[nameof(Character)])
            {
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

    public static List<SavedCharacter> FromCsv(string csv)
    {
        var lines = csv.Trim().Split('\n');
        var header = lines[0].Trim().Split('\t');
        var charas = new List<SavedCharacter>();
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

        static SavedCharacter ParseCsvRow(string[] header, string line)
        {
            var values = line.Split('\t');
            var chara = new SavedCharacter
            {
                CountryId = int.Parse(values[0]),
                MemberOrderIndex = int.Parse(values[1]),
                CastleId = int.Parse(values[2]),
            };
            var character = new Character();
            var characterType = character.GetType();
            for (int j = 3; j < header.Length; j++)
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

    private static readonly string EmptySlotMark = "E";
}
