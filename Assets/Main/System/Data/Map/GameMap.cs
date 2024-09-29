using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

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

            var terrainTile = m.terrainTilemap.GetTile<Tile>(gridPos);
            var terrainIndex = Array.IndexOf(m.terrainTiles, terrainTile);
            var terrain = terrains[terrainIndex];

            var tile = new GameMapTile(this, pos, uiTile, terrain);
            tiles.Add(pos, tile);
        }
    }

    public bool IsValid(MapPosition pos) => tiles.ContainsKey(pos);
    public IEnumerable<GameMapTile> Tiles => tiles.Values;
    public GameMapTile GetTile(MapPosition pos)
    {
        tiles.TryGetValue(pos, out var tile);
        return tile;
    }
}

/// <summary>
/// 地形
/// </summary>
public enum Terrain
{
    LargeRiver,
    River,
    Plain,
    Hill,
    Forest,
    Mountain,
}
