using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Force2
{
    /// <summary>
    /// 軍勢の統率者
    /// </summary>
    public Character Character { get; set; }

    /// <summary>
    /// 軍勢の位置
    /// </summary>
    public MapPosition Position { get; set; }

    /// <summary>
    /// 軍勢の目的地
    /// </summary>
    public MapPosition Destination { get; set; }

    /// <summary>
    /// 軍勢の向き
    /// </summary>
    public Direction Direction { get; set; }

    /// <summary>
    /// 隣のタイルに移動するのにかかる時間
    /// </summary>
    public float TileMoveProgress { get; set; } // 向き毎に持つ方がいいかもしれない。

    public float CalculateMoveCost(WorldData world, GameMapTile current, GameMapTile next)
    {
        var country = world.CountryOf(Character);
        // キャラの攻撃能力に応じて移動コストを補正する。
        var martialAdj = Character.Attack;
        // 自国領の場合は防衛能力との高い方を採用する。
        if (country.Has(current) || country.Has(next))
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