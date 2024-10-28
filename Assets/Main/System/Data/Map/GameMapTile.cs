using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameMapTile : ICountryEntity, IMapEntity
{
    private WorldData world;
    private readonly GameMapManager map;

    public MapPosition Position { get; }
    public Terrain Terrain { get; }
    public HexTile UI { get; private set; }

    public Castle Castle { get; set; }
    public bool HasCastle => Castle != null;
    public Town Town { get; set; }
    public bool HasTown => Town != null;
    public Country Country => (Town?.Castle ?? Castle)?.Country;

    public GameMapTile(GameMapManager map, MapPosition pos, Terrain terrain)
    {
        this.map = map;
        Position = pos;
        Terrain = terrain;
    }

    public void AttachWorld(WorldData world)
    {
        this.world = world;
    }

    public void AttachUI(HexTile ui)
    {
        UI = ui;
    }

    public void Refresh()
    {
        UI.SetCellBorder(false);
        UI.SetCastle(HasCastle);
        UI.SetTown(!HasCastle && HasTown);
        UI.SetCountryFlag(HasCastle ? Country.Sprite : null);
        var force = Forces.FirstOrDefault();
        UI.SetForce(force);
        UI.SetForceFlag(force?.Country.Sprite);
    }


    public IEnumerable<Force> Forces => world.Forces.Where(f => f.Position == Position);

    public GameMapTile[] NeighborArray => Neighbors.ToArray();
    public IEnumerable<GameMapTile> Neighbors
    {
        get
        {
            var t = default(GameMapTile);
            if ((t = map.TryGetTile(Position.UpLeft)) != null) yield return t;
            if ((t = map.TryGetTile(Position.UpRight)) != null) yield return t;
            if ((t = map.TryGetTile(Position.Left)) != null) yield return t;
            if ((t = map.TryGetTile(Position.Right)) != null) yield return t;
            if ((t = map.TryGetTile(Position.DownLeft)) != null) yield return t;
            if ((t = map.TryGetTile(Position.DownRight)) != null) yield return t;
        }
    }

    public override string ToString()
    {
        return $"Tile{Position}";
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
        { Terrain.LargeRiver, new(-300, 050, -30, 020) },
        { Terrain.River,      new(-300, 050, -30, 010) },
        { Terrain.Plain,      new(0500, 100, 030, 000) },
        { Terrain.Hill,       new(0350, 000, 040, 005) },
        { Terrain.Forest,     new(0250, 000, 040, 010) },
        { Terrain.Mountain,   new(0200, 000, 040, 005) },
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
