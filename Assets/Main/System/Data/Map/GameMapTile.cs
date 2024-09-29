using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameMapTile : ICountryEntity
{
    private readonly GameMap map;
    private WorldData world;

    public MapPosition Position { get; }
    public HexTile UI { get; }
    public Terrain Terrain { get; }

    public Castle Castle { get; }
    public Town Town { get; }
    public Country Country => (Town.Castle ?? Castle).Country;

    public GameMapTile(GameMap map, MapPosition pos, HexTile ui, Terrain terrain)
    {
        this.map = map;
        Position = pos;
        UI = ui;
        Terrain = terrain;

        Castle = new Castle() { Position = pos };
        Town = new Town() { Position = pos };
    }

    public void AttachWorld(WorldData world)
    {
        this.world = world;
    }

    public IEnumerable<Force> Forces => world.Forces.Where(f => f.Position == Position);

    public GameMapTile[] NeighborArray => Neighbors.ToArray();
    public IEnumerable<GameMapTile> Neighbors
    {
        get
        {
            var t = default(GameMapTile);
            if ((t = map.GetTile(Position.UpLeft)) != null) yield return t;
            if ((t = map.GetTile(Position.UpRight)) != null) yield return t;
            if ((t = map.GetTile(Position.Left)) != null) yield return t;
            if ((t = map.GetTile(Position.Right)) != null) yield return t;
            if ((t = map.GetTile(Position.DownLeft)) != null) yield return t;
            if ((t = map.GetTile(Position.DownRight)) != null) yield return t;
        }
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
        { Terrain.LargeRiver, new(-300, 050, -30, 030) },
        { Terrain.River,      new(-300, 050, -30, 020) },
        { Terrain.Plain,      new(1000, 150, 030, 000) },
        { Terrain.Hill,       new(0700, 000, 050, 010) },
        { Terrain.Forest,     new(0500, 000, 080, 020) },
        { Terrain.Mountain,   new(0400, 000, 040, 015) },
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
