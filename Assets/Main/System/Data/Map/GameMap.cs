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

public struct MapPosition : IEquatable<MapPosition>
{
    public int x;
    public int y;

    public static MapPosition FromGrid(Vector3Int grid) => new() { x = grid.x, y = -grid.y };
    public static MapPosition Of(int x, int y) => new() { x = x, y = y };
    public readonly MapPosition UpLeft =>    y % 2 == 0 ? Of(x - 1, y - 1) : Of(x    , y - 1);
    public readonly MapPosition UpRight =>   y % 2 == 0 ? Of(x    , y - 1) : Of(x + 1, y - 1);
    public readonly MapPosition DownLeft =>  y % 2 == 0 ? Of(x - 1, y + 1) : Of(x    , y + 1);
    public readonly MapPosition DownRight => y % 2 == 0 ? Of(x    , y + 1) : Of(x + 1, y + 1);
    public readonly MapPosition Left => Of(x - 1, y);
    public readonly MapPosition Right => Of(x + 1, y);

    public readonly MapPosition To(Direction direction) => direction switch
    {
        Direction.UpLeft => UpLeft,
        Direction.UpRight => UpRight,
        Direction.DownLeft => DownLeft,
        Direction.DownRight => DownRight,
        Direction.Left => Left,
        Direction.Right => Right,
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    public readonly Direction GetDirectionTo(MapPosition pos)
    {
        if (pos.y == y)
        {
            if (pos.x < x) return Direction.Left;
            else return Direction.Right;
        }
        if (pos.y < y)
        {
            if (pos.x < x) return Direction.UpLeft;
            else return Direction.UpRight;
        }
        if (pos.y > y)
        {
            if (pos.x < x) return Direction.DownLeft;
            else return Direction.DownRight;
        }
        throw new InvalidOperationException();
    }

    public readonly float DistanceTo(MapPosition pos)
    {
        var dx = pos.x - x;
        var dy = pos.y - y;
        if (dy % 2 == 0)
        {
            return Mathf.Abs(dx) + Mathf.Abs(dy) / 2;
        }
        else
        {
            return Mathf.Abs(dx) + (Mathf.Abs(dy) - 1) / 2;
        }
    }

    public readonly Vector3Int Vector3Int => new(x, -y, 0);

    public override readonly string ToString() => $"({x}, {y})";

    public readonly bool Equals(MapPosition other) => x == other.x && y == other.y;
    public static bool operator ==(MapPosition left, MapPosition right) => left.Equals(right);
    public static bool operator !=(MapPosition left, MapPosition right) => !(left == right);
    public override readonly bool Equals(object obj) => obj is MapPosition other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(x, y);

    public static readonly MapPosition Invalid = new() { x = int.MinValue, y = int.MinValue };
    public readonly bool IsValid => this != Invalid;
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

public enum Direction
{
    UpLeft,
    UpRight,
    DownLeft,
    DownRight,
    Left,
    Right,
}