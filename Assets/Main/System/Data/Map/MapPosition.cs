using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public struct MapPosition : IEquatable<MapPosition>, IMapEntity
{
    readonly MapPosition IMapEntity.Position => this;

    public int x;
    public int y;

    public static MapPosition FromGrid(Vector3Int grid) => new() { x = grid.x, y = -grid.y };
    public static MapPosition Of(int x, int y) => new() { x = x, y = y };
    [JsonIgnore] public readonly MapPosition UpLeft => y % 2 == 0 ? Of(x - 1, y - 1) : Of(x, y - 1);
    [JsonIgnore] public readonly MapPosition UpRight => y % 2 == 0 ? Of(x, y - 1) : Of(x + 1, y - 1);
    [JsonIgnore] public readonly MapPosition DownLeft => y % 2 == 0 ? Of(x - 1, y + 1) : Of(x, y + 1);
    [JsonIgnore] public readonly MapPosition DownRight => y % 2 == 0 ? Of(x, y + 1) : Of(x + 1, y + 1);
    [JsonIgnore] public readonly MapPosition Left => Of(x - 1, y);
    [JsonIgnore] public readonly MapPosition Right => Of(x + 1, y);

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

    [JsonIgnore] public readonly Vector3Int Vector3Int => new(x, -y, 0);

    public override readonly string ToString() => $"({x}, {y})";

    public readonly bool Equals(MapPosition other) => x == other.x && y == other.y;
    public static bool operator ==(MapPosition left, MapPosition right) => left.Equals(right);
    public static bool operator !=(MapPosition left, MapPosition right) => !(left == right);
    public override readonly bool Equals(object obj) => obj is MapPosition other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(x, y);

    public static readonly MapPosition Invalid = new() { x = int.MinValue, y = int.MinValue };
    [JsonIgnore] public readonly bool IsValid => this != Invalid;
}

public enum Direction
{
    UpLeft,
    UpRight,
    Right,
    DownRight,
    DownLeft,
    Left,
}