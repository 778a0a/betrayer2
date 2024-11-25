
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
    /// 食料生産
    /// </summary>
    public float FoodIncome { get; set; }
    [JsonIgnore]
    public float FoodIncomeMaxBase { get; set; }
    [JsonIgnore]
    public float FoodIncomeMax => CalculateMax(FoodIncomeMaxBase, Castle.DevelopmentLevel);
    [JsonIgnore]
    public float FoodIncomeProgress => FoodIncome / FoodIncomeMax;

    /// <summary>
    /// 商業
    /// </summary>
    public float GoldIncome { get; set; }
    [JsonIgnore]
    public float GoldIncomeMaxBase { get; set; }
    [JsonIgnore]
    public float GoldIncomeMax => CalculateMax(GoldIncomeMaxBase, Castle.DevelopmentLevel);
    [JsonIgnore]
    public float GoldIncomeProgress => GoldIncome / GoldIncomeMax;

    private static float CalculateMax(float baseVal, int level)
    {
        return baseVal + baseVal * 0.5f * level;
    }
    
    public float FoodImproveCost()
    {
        if (FoodIncomeProgress < 0.5f) return 3;

        var rem = FoodIncome - FoodIncomeMaxBase;
        return (int)(rem / (FoodIncomeMaxBase * 0.5f) + 1).MinWith(3);
    }
    public float GoldImproveCost()
    {
        if (GoldIncomeProgress < 0.4f) return 3;

        var rem = GoldIncome - GoldIncomeMaxBase;
        return (int)(rem / (GoldIncomeMaxBase * 0.5f) + 1).MinWith(3);
    }

    public static float TileFoodMax(GameMapTile tile) => Mathf.Max(0,
        BaseFoodAdjustment(tile.Terrain) + tile.Neighbors.Sum(t => NeighborFoodAdjustment(t.Terrain)));
    public static float TileGoldMax(GameMapTile tile) => Mathf.Max(0,
        BaseGoldAdjustment(tile.Terrain) + tile.Neighbors.Sum(t => NeighborGoldAdjustment(t.Terrain)));

    public static float BaseFoodAdjustment(Terrain terrain) => devAdjustments[terrain].BaseFood;
    public static float NeighborFoodAdjustment(Terrain terrain) => devAdjustments[terrain].NeighborFood;
    public static float BaseGoldAdjustment(Terrain terrain) => devAdjustments[terrain].BaseGold;
    public static float NeighborGoldAdjustment(Terrain terrain) => devAdjustments[terrain].NeighborGold;
    private static readonly Dictionary<Terrain, TerrainDevAdjustmentData> devAdjustments = new()
    {
        // Terrain                Food  F+   Gold G+
        { Terrain.LargeRiver, new(0000, 050, 000, 003) },
        { Terrain.River,      new(0000, 050, 000, 002) },
        { Terrain.Plain,      new(1100, 100, 020, 000) },
        { Terrain.Hill,       new(1020, 020, 021, 001) },
        { Terrain.Forest,     new(1030, 030, 022, 002) },
        { Terrain.Mountain,   new(1000, 000, 022, 002) },
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
