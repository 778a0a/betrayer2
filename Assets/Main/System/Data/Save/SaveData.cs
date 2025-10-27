using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// セーブデータ全体を表すクラス
/// </summary>
public class SaveData
{
    public List<SavedCharacter> Characters { get; set; }
    public List<SavedCountry> Countries { get; set; }
    public List<SavedCastle> Castles { get; set; }
    public List<SavedForce> Forces { get; set; }
    public List<SavedCountryRelation> CountryRelations { get; set; }
    public List<SavedTerrain> Terrains { get; set; }
    public SavedWorldMiscData Misc { get; set; }
    public SaveDataSummary Summary { get; set; }

    public WorldData RestoreWorldData()
    {
        Debug.Log("地形データ復元中...");
        var map = new GameMapManager(Terrains);

        Debug.Log($"国データ復元中...");
        var countries = Countries
            .Select(c => c.Data)
            .ToList();

        Debug.Log($"城・町データ復元中...");
        var castles = Castles;
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
        foreach (var a in countries.SelectMany(c => c.Castles))
        {
            foreach (var b in countries.SelectMany(c => c.Castles))
            {
                if (a == b) continue;
                var d = a.DistanceTo(b);
                a.Distances[b] = d;
                if (d <= Castle.NeighborDistanceMax)
                {
                    a.Neighbors.Add(b);
                }
            }
        }

        Debug.Log($"キャラデータ復元中...");
        var characters = new List<Character>();
        foreach (var chara in Characters)
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
                chara.Character.ChangeCastle(tile.Castle, chara.IsFree);
            }
            else
            {
                Debug.Assert(chara.IsFree);
                // ランダムに配置する。
                var tile = map.GetTile(castles.RandomPick().Data);
                chara.Character.ChangeCastle(tile.Castle, true);
            }
        }

        Debug.Log($"軍勢データ復元中...");
        var savedForces = Forces;
        foreach (var force in savedForces)
        {
            force.Data.Country = countries.Find(c => c.Id == force.ContryId); ;
            force.Data.Character = characters.Find(c => c.Id == force.CharacterId);
        }

        Debug.Log($"ワールド復元中...");
        var world = new WorldData
        {
            Countries = new(countries, CountryRelations),
            Characters = characters,
            Forces = new(savedForces.Select(f => f.Data)),
            Map = map,
            GameDate = new(Misc.GameDateTicks),
        };
        map.AttachWorld(world);
        foreach (var chara in world.Characters)
        {
            chara.AttachWorld(world);
        }

        foreach (var force in savedForces)
        {
            force.Data.AttachWorld(world);
            force.Data.SetDestination(force.DestinationType switch
            {
                SavedForceDestinationType.Force => savedForces.Find(f => f.CharacterId == force.DestinationForceCharacterId).Data,
                SavedForceDestinationType.Castle => map.GetTile(force.DestinationPosition).Castle,
                SavedForceDestinationType.Position => map.GetTile(force.DestinationPosition),
                _ => throw new ArgumentOutOfRangeException(),
            }, false, true);
            force.Data.ReinforcementOriginalTarget = force.ReinforcementOriginalTargetCastleId != -1
                ? castles.First(c => c.Data.Id == force.ReinforcementOriginalTargetCastleId).Data
                : null;
        }

        var player = world.Characters.FirstOrDefault(c => c.IsPlayer);
        world.SetPlayer(player);

        return world;
    }
}
