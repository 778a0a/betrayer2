using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Force : ICountryEntity, IMapEntity
{
    public Force(WorldData world, Character character, MapPosition position)
    {
        this.world = world;
        Character = character;
        Country = character?.Country;
        Position = position;
        Destination = position;
        Direction = Direction.Right;
    }

    private WorldData world;

    /// <summary>
    /// 軍勢の所属国
    /// </summary>
    [JsonIgnore]
    public Country Country { get; set; }
    /// <summary>
    /// 軍勢の統率者
    /// </summary>
    [JsonIgnore]
    public Character Character { get; set; }

    /// <summary>
    /// 軍勢の位置
    /// </summary>
    public MapPosition Position { get; private set; }

    /// <summary>
    /// 軍勢の目的地
    /// </summary>
    [JsonIgnore]
    public IMapEntity Destination { get; private set; }

    [JsonIgnore]
    public LinkedList<MapPosition> DestinationPath { get; set; }

    /// <summary>
    /// 軍勢の向き
    /// </summary>
    public Direction Direction { get; private set; }

    /// <summary>
    /// 隣のタイルに移動するのにかかる残り日数
    /// </summary>
    public float TileMoveRemainingDays { get; set; }

    public void UpdatePosition(MapPosition pos)
    {
        var oldTile = world.Map.GetTile(Position);
        Position = pos;
        oldTile.Refresh();

        if (DestinationPath.Count > 0)
        {
            var expectedPos = DestinationPath.First();
            if (expectedPos == pos)
            {
                DestinationPath.RemoveFirst();
                if (DestinationPath.Count > 0)
                {
                    Direction = pos.DirectionTo(DestinationPath.First());
                }
            }
            else if (Destination.Position != pos)
            {
                Debug.Log($"軍勢の位置がPathと一致しません。経路を再計算します。 {this}");
                DestinationPath = FindPath(Destination.Position);
                Direction = Position.DirectionTo(DestinationPath.First());
            }
        }

        ResetTileMoveProgress();

        RefreshUI();
    }

    public void RefreshUI()
    {
        var tile = world.Map.GetTile(Position);
        tile.Refresh();
    }

    /// <summary>
    /// 目的地を設定します。
    /// </summary>
    /// <param name="destination"></param>
    public void SetDestination(IMapEntity destination, bool updateUI = true, bool isRestoring = false)
    {
        var prevDestination = Destination;
        var prevDirection = Direction;
        Destination = destination;
        if (Destination.Position != Position)
        {
            DestinationPath = FindPath(destination.Position);
            Direction = Position.DirectionTo(DestinationPath.First());
        }
        // 目的地が変わった場合は移動日数をリセットする
        if (!isRestoring && (prevDestination.Position == Position || prevDirection != Direction))
        {
            ResetTileMoveProgress();
        }

        if (updateUI)
        {
            RefreshUI();
        }
    }

    private LinkedList<MapPosition> FindPath(MapPosition dest)
    {
        if (Position == dest) return new LinkedList<MapPosition>();
        var start = Position;
        var open = new List<MapPosition> { start };
        var close = new List<MapPosition>();
        var cameFrom = new Dictionary<MapPosition, MapPosition>();
        var g = new Dictionary<MapPosition, float> { { start, 0 } };
        var f = new Dictionary<MapPosition, float> { { start, start.DistanceTo(dest) } };

        while (open.Count > 0)
        {
            var current = open.OrderBy(p => f.GetValueOrDefault(p, float.MaxValue)).First();
            if (current == dest)
            {
                return ReconstructPath(cameFrom, current);
            }

            open.Remove(current);
            close.Add(current);

            foreach (var neighborTile in world.Map.GetTile(current).Neighbors)
            {
                var neighbor = neighborTile.Position;
                if (close.Contains(neighbor))
                {
                    continue;
                }

                var gscore = g[current] + CalculateMoveCost(current, neighbor);
                if (!open.Contains(neighbor))
                {
                    open.Add(neighbor);
                }
                else if (gscore >= g.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                g[neighbor] = gscore;
                f[neighbor] = gscore + neighbor.DistanceTo(dest) * 8 * 2; // 8*2 = 1マスの大体の移動コスト
            }
        }
        throw new InvalidOperationException("Path not found");

        static LinkedList<MapPosition> ReconstructPath(Dictionary<MapPosition, MapPosition> cameFrom, MapPosition current)
        {
            var path = new LinkedList<MapPosition>();
            path.AddFirst(current);
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.AddFirst(current);
            }
            path.RemoveFirst();
            return path;
        }
    }

    public void ResetTileMoveProgress()
    {
        if (Destination.Position == Position)
        {
            TileMoveRemainingDays = 0;
            return;
        }
        TileMoveRemainingDays = CalculateMoveCost(Position.To(Direction));
    }

    public float CalculateMoveCost(MapPosition nextPos) => CalculateMoveCost(Position, nextPos);
    public float CalculateMoveCost(MapPosition fromPos, MapPosition nextPos)
    {
        // キャラの攻撃能力に応じて移動コストを補正する。
        var martialAdj = Character.Attack;

        // 自国領の場合は防衛能力で補正する。
        var current = world.Map.GetTile(fromPos);
        var next = world.Map.GetTile(nextPos);
        if (Country.Has(current) || Country.Has(next))
        {
            martialAdj = Character.Defense;
        }
        // 能力が70の場合は補正1.0、能力が80の場合は0.9、能力が60の場合は1.1
        var martialAdjRate = 1.0f - (martialAdj - 70) * 0.01f;

        // TODO traitによる補正
        var currentCost = tileMoveCost[current.Terrain];
        var nextCost = tileMoveCost[next.Terrain];
        return (currentCost + nextCost) * martialAdjRate;
    }

    public override string ToString()
    {
        return $"軍勢({Character.Name} at {Position} -> {Destination} ({TileMoveRemainingDays}))";
    }

    public void AttachWorld(WorldData world)
    {
        this.world = world;
    }

    // タイルの移動にかかる日数
    // (現在のタイルのコスト + 移動先のタイルのコスト) が実際にかかる日数
    private static readonly Dictionary<Terrain, float> tileMoveCost = new()
    {
        { Terrain.LargeRiver, 20 },
        { Terrain.River,      15 },
        { Terrain.Plain,      5 },
        { Terrain.Hill,       8 },
        { Terrain.Forest,     10 },
        { Terrain.Mountain,   15 },
    };
    private struct TerrainDevAdjustmentData
    {
        public float BaseFood;
        public float NeighborFood;
        public float BaseGold;
        public float NeighborGold;
        public TerrainDevAdjustmentData(float baseFood, float neighborFood, float baseGold, float neighborGold)
        {
            BaseFood = baseFood;
            NeighborFood = neighborFood;
            BaseGold = baseGold;
            NeighborGold = neighborGold;
        }
    }
}