using System;
using System.Collections.Generic;
using System.IO;
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
    public static WorldData Create(string saveDir = "01")
    {
        Debug.Log("地形データ読み込み中...");
        var terrains = SavedTerrains.FromCsv(LoadTextFile($"Scenarios/{saveDir}/terrain_data"));
        Debug.Log($"国データ読み込み中...");
        var countries = SavedCountries.FromCsv(LoadTextFile($"Scenarios/{saveDir}/country_data"));
        Debug.Log($"国関係データ読み込み中...");
        var rels = SavedCountryRelations.FromCsv(LoadTextFile($"Scenarios/{saveDir}/country_relation_data"));
        Debug.Log($"城・町データ読み込み中...");
        var castles = SavedCastles.FromCsv(LoadTextFile($"Scenarios/{saveDir}/castle_data"));
        Debug.Log($"キャラデータ読み込み中...");
        var charas = SavedCharacters.FromCsv(LoadTextFile($"Scenarios/{saveDir}/character_data"));
        Debug.Log($"軍勢データ読み込み中...");
        var forces = SavedForces.FromCsv(LoadTextFile($"Scenarios/{saveDir}/force_data"));

        var saveData = new SaveData
        {
            Countries = countries,
            CountryRelations = rels,
            Castles = castles,
            Characters = charas,
            Terrains = terrains,
            Forces = forces,
            Misc = new SavedWorldMiscData() { GameDateTicks = 0 },
            Summary = new SaveDataSummary() { },
        };

        Debug.Log("ワールドデータ復元中...");
        var world = saveData.RestoreWorldData();
        return world;
    }

    private static string LoadTextFile(string path)
    {
#if UNITY_EDITOR
        // 書き込みを即座に反映させるために、直接読み込みます。
        return File.ReadAllText($"Assets/Resources/{path}.csv");
#else
        return Resources.Load<TextAsset>(path).text;
#endif
    }

    public static void SaveToResources(WorldData world, string saveDir = "01", bool retainFreeCharaCastleRandom = false)
    {
        var countries = SavedCountries.FromWorld(world);
        var rels = SavedCountryRelations.FromWorld(world);
        var castles = SavedCastles.FromWorld(world);
        var characters = SavedCharacters.FromWorld(world, retainFreeCharaCastleRandom);
        var terrains = SavedTerrains.FromWorld(world);
        var forces = SavedForces.FromWorld(world);

        var countryCsv = SavedCountries.ToCsv(countries) + Environment.NewLine;
        var relCsv = SavedCountryRelations.ToCsv(rels) + Environment.NewLine;
        var castleCsv = SavedCastles.ToCsv(castles) + Environment.NewLine;
        var charaCsv = SavedCharacters.ToCsv(characters) + Environment.NewLine;
        var terrainCsv = SavedTerrains.ToCsv(terrains) + Environment.NewLine;
        var forceCsv = SavedForces.ToCsv(forces) + Environment.NewLine;

        var dir = $"Assets/Resources/Scenarios/{saveDir}";
        Directory.CreateDirectory(dir);
        File.WriteAllText($"{dir}/country_data.csv", countryCsv, Encoding.UTF8);
        File.WriteAllText($"{dir}/country_relation_data.csv", relCsv, Encoding.UTF8);
        File.WriteAllText($"{dir}/castle_data.csv", castleCsv, Encoding.UTF8);
        File.WriteAllText($"{dir}/character_data.csv", charaCsv, Encoding.UTF8);
        File.WriteAllText($"{dir}/terrain_data.csv", terrainCsv, Encoding.UTF8);
        File.WriteAllText($"{dir}/force_data.csv", forceCsv, Encoding.UTF8);
    }
}
