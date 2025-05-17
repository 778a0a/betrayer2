
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 町
/// </summary>
public class Town : ICountryEntity, IMapEntity
{
    /// <summary>
    /// 位置
    /// </summary>
    public MapPosition Position { get; set; }

    /// <summary>
    /// 町が所属する城
    /// </summary>
    [JsonIgnore]
    public Castle Castle { get; set; }

    [JsonIgnore]
    public Country Country => Castle.Country;

    /// <summary>
    /// 開発レベル
    /// </summary>
    [JsonProperty("D")]
    public int DevelopmentLevel { get; set; } = 0;

    /// <summary>
    /// 商業
    /// </summary>
    [JsonProperty("G")]
    public float GoldIncome { get; set; }
    [JsonIgnore]
    public float GoldIncomeMaxBase { get; set; }
    [JsonIgnore]
    public float GoldIncomeMax => CalculateMax(GoldIncomeMaxBase, DevelopmentLevel);
    [JsonIgnore]
    public float GoldIncomeProgress => GoldIncome / GoldIncomeMax;
    [JsonIgnore]
    public float GoldImproveAdj => Diminish(GoldIncome, GoldIncomeMax, GoldIncomeMaxBase);

    private static float CalculateMax(float baseVal, int level)
    {
        return baseVal + baseVal * level;
    }
    
    public static float Diminish(float current, float max, float maxBase)
    {
        var progress = current / max;
        return progress switch
        {
            < 0.25f => 2.0f,
            < 0.5f => 1.5f,
            _ => (1.5f - progress) * (3f / (1 + current / maxBase)).MaxWith(1), // lv4なら0.6、lv5なら0.5、lv6なら0.43
        };
    }


    public static float TileGoldMax(GameMapTile tile, Castle castle = null)
    {
        var val = BaseGoldAdj(tile.Terrain) + tile.Neighbors.Sum(t => NeighborGoldAdj(t.Terrain));
        return val * TileMaxAdj(tile, castle);
    }
    private static float TileMaxAdj(GameMapTile tile, Castle castle = null)
    {
        var adj = 1.0f;
        if (castle != null)
        {
            adj *= Mathf.Pow(0.8f, castle.Towns.Count - 1);
           var distance = castle.Position.DistanceTo(tile.Position);
            if (distance > 1)
            {
                adj *= Mathf.Pow(0.8f, distance - 1);
            }
        }
        return adj;
    }

    private static float BaseGoldAdj(Terrain terrain) => devAdj[terrain].BaseGold + devAdj[terrain].NeighborGold;
    private static float NeighborGoldAdj(Terrain terrain) => devAdj[terrain].NeighborGold;
    private static readonly Dictionary<Terrain, TerrainDevAdjustmentData> devAdj = new()
    {
        // Terrain                Gold G+
        { Terrain.LargeRiver, new(000, 001.5f) },
        { Terrain.River,      new(000, 001) },
        { Terrain.Plain,      new(010, 000) },
        { Terrain.Hill,       new(010, 000.5f) },
        { Terrain.Forest,     new(010, 001) },
        { Terrain.Mountain,   new(010, 001) },
    };
    private struct TerrainDevAdjustmentData
    {
        public float BaseGold;
        public float NeighborGold;
        public TerrainDevAdjustmentData(float baseGold, float neighborGold)
        {
            BaseGold = baseGold;
            NeighborGold = neighborGold;
        }
    }
}
