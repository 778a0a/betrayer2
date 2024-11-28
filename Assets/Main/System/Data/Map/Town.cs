
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
    /// 食料生産
    /// </summary>
    [JsonProperty("F")]
    public float FoodIncome { get; set; }
    [JsonIgnore]
    public float FoodIncomeMaxBase { get; set; }
    [JsonIgnore]
    public float FoodIncomeMax => CalculateMax(FoodIncomeMaxBase, DevelopmentLevel);
    [JsonIgnore]
    public float FoodIncomeProgress => FoodIncome / FoodIncomeMax;
    [JsonIgnore]
    public float FoodImproveAdj => Diminish(FoodIncome, FoodIncomeMax, FoodIncomeMaxBase);

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


    public static float TileFoodMax(GameMapTile tile, int townCount = 1)
    {
        var val = BaseFoodAdj(tile.Terrain) + tile.Neighbors.Sum(t => NeighborFoodAdj(t.Terrain));
        return val * TownCountAdj(townCount);
    }
    public static float TileGoldMax(GameMapTile tile, int townCount = 1)
    {
        var val = BaseGoldAdj(tile.Terrain) + tile.Neighbors.Sum(t => NeighborGoldAdj(t.Terrain));
        return val * TownCountAdj(townCount);
    }

    private static float TownCountAdj(int townCount) => Mathf.Pow(0.8f, townCount - 1);
    private static float BaseFoodAdj(Terrain terrain) => devAdj[terrain].BaseFood + devAdj[terrain].NeighborFood;
    private static float NeighborFoodAdj(Terrain terrain) => devAdj[terrain].NeighborFood;
    private static float BaseGoldAdj(Terrain terrain) => devAdj[terrain].BaseGold + devAdj[terrain].NeighborGold;
    private static float NeighborGoldAdj(Terrain terrain) => devAdj[terrain].NeighborGold;
    private static readonly Dictionary<Terrain, TerrainDevAdjustmentData> devAdj = new()
    {
        // Terrain                Food  F+   Gold G+
        { Terrain.LargeRiver, new(0000, 000, 000, 001.5f) },
        { Terrain.River,      new(0000, 025, 000, 001) },
        { Terrain.Plain,      new(0500, 050, 010, 000) },
        { Terrain.Hill,       new(0500, 025, 010, 000.5f) },
        { Terrain.Forest,     new(0500, 000, 010, 001) },
        { Terrain.Mountain,   new(0500, 000, 010, 001) },
        //{ Terrain.LargeRiver, new(-300, 050, -30, 020) },
        //{ Terrain.River,      new(-300, 050, -30, 010) },
        //{ Terrain.Plain,      new(0500, 100, 030, 000) },
        //{ Terrain.Hill,       new(0350, 000, 040, 005) },
        //{ Terrain.Forest,     new(0250, 000, 040, 010) },
        //{ Terrain.Mountain,   new(0200, 000, 040, 005) },
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
