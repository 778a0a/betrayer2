using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Force : ICountryEntity, IMapEntity
{
    public Force(WorldData world, Character character, MapPosition position)
    {
        this.world = world;
        Character = character;
        Country = character.Country;
        Position = position;
        Destination = position;
        Direction = Direction.Right;
    }

    private WorldData world;

    /// <summary>
    /// 軍勢の所属国
    /// </summary>
    public Country Country { get; set; }
    /// <summary>
    /// 軍勢の統率者
    /// </summary>
    public Character Character { get; set; }

    /// <summary>
    /// 軍勢の位置
    /// </summary>
    public MapPosition Position { get; private set; }

    /// <summary>
    /// 軍勢の目的地
    /// </summary>
    public IMapEntity Destination { get; private set; }

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
    public void SetDestination(IMapEntity destination)
    {
        var prevDestination = Destination;
        var prevDirection = Direction;
        Destination = destination;
        Direction = Position.DirectionTo(destination);
        // 目的地が変わった場合は移動日数をリセットする
        if (prevDestination.Position == Position || prevDirection != Direction)
        {
            ResetTileMoveProgress();
        }

        RefreshUI();
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

    public float CalculateMoveCost(MapPosition nextPos)
    {
        // キャラの攻撃能力に応じて移動コストを補正する。
        var martialAdj = Character.Attack;
        // 自国領の場合は防衛能力との高い方を採用する。
        var current = world.Map.GetTile(Position);
        var next = world.Map.GetTile(nextPos);
        if (Country.Has(current) || Country.Has(next))
        {
            martialAdj = Mathf.Max(martialAdj, Character.Defense);
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

    // タイルの移動にかかる日数
    // (現在のタイルのコスト + 移動先のタイルのコスト) が実際にかかる日数
    private static readonly Dictionary<Terrain, float> tileMoveCost = new()
    {
        { Terrain.LargeRiver, 15 },
        { Terrain.River,      10 },
        { Terrain.Plain,      5 },
        { Terrain.Hill,       7 },
        { Terrain.Forest,     8 },
        { Terrain.Mountain,   10 },
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