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

        Debug.Log($"国データ読み込み中...");
        var countriesRaw = Resources.Load<TextAsset>("Scenarios/01/country_data").text;
        var countries = SavedCountries.FromCsv(countriesRaw).Select(c => c.Data).ToList();
        
        Debug.Log($"城・町データ読み込み中...");
        var castles = SavedCastles.FromCsv(Resources.Load<TextAsset>("Scenarios/01/castle_data").text);
        foreach (var castle in castles)
        {
            var tile = map.GetTile(castle.Data.Position);
            tile.Castle = castle.Data;
            tile.Castle.Country = countries.Find(c => c.Id == castle.CountryId);
            tile.Castle.Country.Castles.Add(tile.Castle);
            foreach (var town in castle.Towns)
            {
                var townTile = map.GetTile(town.Position);
                townTile.Town = town;
                townTile.Town.Castle = tile.Castle;
                townTile.Town.FoodIncomeMax = GameMapTile.TileFoodMax(townTile);
                townTile.Town.GoldIncomeMax = GameMapTile.TileGoldMax(townTile);
                tile.Castle.AddTown(townTile.Town);
            }
        }

        Debug.Log($"キャラデータ読み込み中...");
        var oldcsv = Resources.Load<TextAsset>("Scenarios/01/character_data").text;
        var oldcharas = SavedCharacters.FromCsv(oldcsv);
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
                var tile = map.GetTile(castles.Find(c => c.Data.Id == chara.CastleId).Data.Position);
                tile.Castle.Members.Add(chara.Character);
            }
        }

        var world = new WorldData
        {
            Castles = countries.SelectMany(c => c.Castles).ToArray(),
            Countries = countries.ToArray(),
            Characters = characters.ToArray(),
            Forces = new(),
            Map = map,
        };
        foreach (var tile in map.Tiles)
        {
            tile.AttachWorld(world);
        }
        foreach (var chara in world.Characters)
        {
            chara.AttachWorld(world);
        }
        foreach (var tile in map.Tiles)
        {
            tile.Refresh();
        }
        return world;
    }
}
