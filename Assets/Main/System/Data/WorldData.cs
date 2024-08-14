using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class WorldData
{
    public Character[] Characters { get; set; }
    // なぜかList<T>だと、HotReload後にforeachしたときにエラーが起きるのでIList<T>を使います。
    public IList<Country> Countries { get; set; }
    public GameMap Map { get; set; }

    public bool IsRuler(Character chara) => Countries.Any(c => c.Ruler == chara);
    public bool IsVassal(Character chara) => Countries.Any(c => c.Vassals.Contains(chara));
    public bool IsFree(Character chara) => !IsRuler(chara) && !IsVassal(chara);
    public bool IsRulerOrVassal(Character chara) => IsRuler(chara) || IsVassal(chara);

    public Country CountryOf(Character chara) => Countries.FirstOrDefault(c => c.Ruler == chara || c.Vassals.Contains(chara));
    public Country CountryOf(Castle castle) => Countries.FirstOrDefault(c => c.Catsles.Contains(castle));

    public override string ToString() => $"WorldData {Characters.Length} characters, {Countries.Count} countries";
}

public class GameMap
{
    private readonly Dictionary<MapPosition, GameMapTile> tiles = new();

    public GameMap(MapManager m)
    {
        var uiTiles = m.uiTilemap.GetComponentsInChildren<HexTile>()
            .ToDictionary(h => MapPosition.FromGrid(m.uiTilemap.WorldToCell(h.transform.position)));

        var terrains = Util.EnumArray<Terrain>();
        foreach (var pos in uiTiles.Keys)
        {
            var gridPos = pos.Vector3Int;

            var uiTile = uiTiles[pos];
            var countryTile = m.castleTilemap.GetTile<Tile>(gridPos);
            var countryIndex = Array.IndexOf(m.countryTiles, countryTile);
                
            var terrainTile = m.terrainTilemap.GetTile<Tile>(gridPos);
            var terrainIndex = Array.IndexOf(m.terrainTiles, terrainTile);
            var terrain = terrains[terrainIndex];

            var tile = new GameMapTile
            {
                Position = pos,
                UI = uiTile,
                Terrain = terrain,
                CountryIndex = countryIndex,
            };
            tiles.Add(pos, tile);
        }
    }

    public bool IsValid(MapPosition pos) => tiles.ContainsKey(pos);
    public GameMapTile GetTile(MapPosition pos)
    {
        tiles.TryGetValue(pos, out var tile);
        return tile;
    }
}

public class GameMapTile
{
    public MapPosition Position { get; set; }
    public HexTile UI { get; set; }
    public Terrain Terrain { get; set; }
    public int CountryIndex { get; set; }
}
