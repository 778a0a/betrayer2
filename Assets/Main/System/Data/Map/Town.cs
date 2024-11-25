
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
        BaseFoodAdj(tile.Terrain) + tile.Neighbors.Sum(t => NeighborFoodAdj(t.Terrain)));
    public static float TileGoldMax(GameMapTile tile) => Mathf.Max(0,
        BaseGoldAdj(tile.Terrain) + tile.Neighbors.Sum(t => NeighborGoldAdj(t.Terrain)));

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
