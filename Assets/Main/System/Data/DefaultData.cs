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
        Random.InitState(42);

        Debug.Log("地形データ読み込み中...");
        var terrains = SavedTerrains.FromCsv(LoadTextFile($"Scenarios/{saveDir}/terrain_data"));
        var map = new GameMapManager(terrains);

        Debug.Log($"国データ読み込み中...");
        var countries = SavedCountries.FromCsv(LoadTextFile($"Scenarios/{saveDir}/country_data"))
            .Select(c => c.Data)
            .ToList();

        Debug.Log($"国関係データ読み込み中...");
        var rels = SavedCountryRelations.FromCsv(LoadTextFile($"Scenarios/{saveDir}/country_relation_data"));

        Debug.Log($"城・町データ読み込み中...");
        var castles = SavedCastles.FromCsv(LoadTextFile($"Scenarios/{saveDir}/castle_data"));
        foreach (var savedCastle in castles)
        {
            var castle = savedCastle.Data;
            var country = countries.Find(c => c.Id == savedCastle.CountryId);
            map.RegisterCastle(country, castle);
            foreach (var town in savedCastle.Towns)
            {
                var townTile = map.GetTile(town.Position);
                map.RegisterTown(castle, town);
            }
        }

        Debug.Log($"キャラデータ読み込み中...");
        var oldcharas = SavedCharacters.FromCsv(LoadTextFile($"Scenarios/{saveDir}/character_data"));
        var characters = new List<Character>();
        foreach (var chara in oldcharas)
        {
            characters.Add(chara.Character);

            if (!chara.IsFree)
            {
                var country = countries[chara.CountryId];
                if (chara.IsRuler)
                {
                    country.Ruler = chara.Character;
                }
            }

            if (chara.CastleId != -1)
            {
                var tile = map.GetTile(castles.Find(c => c.Data.Id == chara.CastleId).Data);
                if (chara.IsFree)
                {
                    tile.Castle.Frees.Add(chara.Character);
                }
                else
                {
                    tile.Castle.Members.Add(chara.Character);
                }
            }
            else
            {
                Debug.Assert(chara.IsFree);
                // ランダムに配置する。
                var tile = map.GetTile(castles.RandomPick().Data);
                tile.Castle.Frees.Add(chara.Character);
            }
        }

        Debug.Log($"軍勢データ読み込み中...");
        var savedForces = SavedForces.FromCsv(LoadTextFile($"Scenarios/{saveDir}/force_data"));
        foreach (var force in savedForces)
        {
            force.Data.Country = countries.Find(c => c.Id == force.ContryId); ;
            force.Data.Character = characters.Find(c => c.Id == force.CharacterId);
            force.Data.SetDestination(force.DestinationType switch
            {
                ForceDestinationType.Force => savedForces.Find(f => f.CharacterId == force.DestinationForceCharacterId).Data,
                ForceDestinationType.Position => map.GetTile(force.DestinationPosition),
                _ => throw new ArgumentOutOfRangeException(),
            }, false, true);
        }

        var world = new WorldData
        {
            Countries = new(countries, rels),
            Characters = characters,
            Forces = new(savedForces.Select(f => f.Data)),
            Map = map,
        };
        map.AttachWorld(world);
        foreach (var chara in world.Characters)
        {
            chara.AttachWorld(world);
        }
        foreach (var force in world.Forces)
        {
            force.AttachWorld(world);
        }
        return world;
    }

    private static string LoadTextFile(string path)
    {
#if UNITY_EDITOR
        // 書き込みを即座に反映させるために、直接読み込みます。
        return File.ReadAllText($"Assets/Resources/{path}.csv");
#else
        return Resources.Load<TextAsset>(path).text
#endif
    }

    public static void SaveToResources(WorldData world, string saveDir = "01")
    {
        var countries = SavedCountries.FromWorld(world);
        var rels = SavedCountryRelations.FromWorld(world);
        var castles = SavedCastles.FromWorld(world);
        var characters = SavedCharacters.FromWorld(world);
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
