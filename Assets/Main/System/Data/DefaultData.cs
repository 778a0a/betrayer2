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
    public static WorldData Create()
    {
        Random.InitState(42);

        Debug.Log("地形データ読み込み中...");
        var terrains = SavedTerrains.FromCsv(LoadTextFile("Scenarios/01/terrain_data"));
        var map = new GameMapManager(terrains);

        Debug.Log($"国データ読み込み中...");
        var countries = SavedCountries.FromCsv(LoadTextFile("Scenarios/01/country_data"))
            .Select(c => c.Data)
            .ToList();

        Debug.Log($"城・町データ読み込み中...");
        var castles = SavedCastles.FromCsv(LoadTextFile("Scenarios/01/castle_data"));
        foreach (var savedCastle in castles)
        {
            var castle = savedCastle.Data;
            var country = countries.Find(c => c.Id == savedCastle.CountryId);
            map.RegisterCastle(country, castle);
            foreach (var town in savedCastle.Towns)
            {
                var townTile = map.GetTile(town.Position);
                town.FoodIncomeMax = GameMapTile.TileFoodMax(townTile);
                town.GoldIncomeMax = GameMapTile.TileGoldMax(townTile);

                map.RegisterTown(castle, town);
            }
        }

        Debug.Log($"キャラデータ読み込み中...");
        var oldcharas = SavedCharacters.FromCsv(LoadTextFile("Scenarios/01/character_data"));
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

            if (chara.CastleId != -1)
            {
                var tile = map.GetTile(castles.Find(c => c.Data.Id == chara.CastleId).Data);
                tile.Castle.Members.Add(chara.Character);
            }
        }

        Debug.Log($"軍勢データ読み込み中...");
        var savedForces = new List<SavedForce>();
        //var savedForces = SavedForces.FromCsv(LoadTextFile("Scenarios/01/force_data"));
        foreach (var force in savedForces)
        {
            force.Data.Country = countries.Find(c => c.Id == force.ContryId); ;
            force.Data.Character = characters.Find(c => c.Id == force.CharacterId);
            force.Data.SetDestination(force.DestinationType switch
            {
                ForceDestinationType.Force => savedForces.Find(f => f.CharacterId == force.DestinationForceCharacterId).Data,
                ForceDestinationType.Position => map.GetTile(force.DestinationPosition),
                _ => throw new ArgumentOutOfRangeException(),
            }, false);
        }

        var world = new WorldData
        {
            Countries = new(countries),
            Characters = characters.ToArray(),
            Forces = new(savedForces.Select(f => f.Data)),
            Map = map,
        };
        map.AttachWorld(world);
        foreach (var chara in world.Characters)
        {
            chara.AttachWorld(world);
        }
        return world;
    }

    private static string LoadTextFile(string path)
    {
#if UNITY_EDITOR
        // 書き込みを即座に反映させるために、直接読み込みます。
        return File.ReadAllText($"Assets/Resources/{path}.csv");
#else
        return Resources.Load<TextAsset>("").text
#endif
    }

    public static void SaveToResources(WorldData world)
    {
        var countries = SavedCountries.FromWorld(world);
        var castles = SavedCastles.FromWorld(world);
        var characters = SavedCharacters.FromWorld(world);
        var terrains = SavedTerrains.FromWorld(world);
        var forces = SavedForces.FromWorld(world);

        var countryCsv = SavedCountries.ToCsv(countries) + Environment.NewLine;
        var castleCsv = SavedCastles.ToCsv(castles) + Environment.NewLine;
        var charaCsv = SavedCharacters.ToCsv(characters) + Environment.NewLine;
        var terrainCsv = SavedTerrains.ToCsv(terrains) + Environment.NewLine;
        var forceCsv = SavedForces.ToCsv(forces) + Environment.NewLine;

        File.WriteAllText("Assets/Resources/Scenarios/01/country_data.csv", countryCsv, Encoding.UTF8);
        File.WriteAllText("Assets/Resources/Scenarios/01/castle_data.csv", castleCsv, Encoding.UTF8);
        File.WriteAllText("Assets/Resources/Scenarios/01/character_data.csv", charaCsv, Encoding.UTF8);
        File.WriteAllText("Assets/Resources/Scenarios/01/terrain_data.csv", terrainCsv, Encoding.UTF8);
        File.WriteAllText("Assets/Resources/Scenarios/01/force_data.csv", forceCsv, Encoding.UTF8);
    }
}
