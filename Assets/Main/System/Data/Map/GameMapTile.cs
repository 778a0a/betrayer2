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
}
