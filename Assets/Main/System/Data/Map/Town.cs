
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

    /// <summary>
    /// 商業
    /// </summary>
    public float GoldIncome { get; set; }
    [JsonIgnore]
    public float GoldIncomeMaxBase { get; set; }
    [JsonIgnore]
    public float GoldIncomeMax => CalculateMax(GoldIncomeMaxBase, Castle.DevelopmentLevel);
    
    private static float CalculateMax(float baseVal, int level)
    {
        return baseVal + baseVal * 0.5f * level;
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
        { Terrain.Plain,      new(0500, 100, 010, 000) },
        { Terrain.Hill,       new(0500, 020, 010, 001) },
        { Terrain.Forest,     new(0500, 030, 010, 002) },
        { Terrain.Mountain,   new(0500, 000, 010, 002) },
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
