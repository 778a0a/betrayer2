using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameMapManager
{
    private readonly Dictionary<MapPosition, GameMapTile> tiles = new();
    
    public IEnumerable<GameMapTile> Tiles => tiles.Values;

    public GameMapManager(List<SavedTerrain> terrains)
    {
        foreach (var terrain in terrains)
        {
            var tile = new GameMapTile(this, terrain.Position, terrain.Terrain);
            tiles.Add(terrain.Position, tile);
        }
    }

    public void AttachUI(UIMapManager ui)
    {
        ui.AttachGameMap(this);
        foreach (var uiTile in ui.uiTilemap.GetComponentsInChildren<HexTile>())
        {
            var uiPos = ui.uiTilemap.WorldToCell(uiTile.transform.position);
            var pos = MapPosition.FromGrid(uiPos);
            if (tiles.TryGetValue(pos, out var tile))
            {
                tile.AttachUI(uiTile);
            }
            else
            {
                Debug.LogWarning($"UIタイルのセットに失敗しました: ui:{uiPos} -> pos:{pos}");
            }
        }

        foreach (var tile in Tiles)
        {
            tile.Refresh();
        }
    }

    public bool IsValid(MapPosition pos) => tiles.ContainsKey(pos);

    public GameMapTile GetTile(IMapEntity entity)
    {
        tiles.TryGetValue(entity.Position, out var tile);
        return tile;
    }
}

/// <summary>
/// 地形
/// </summary>
public enum Terrain
{
    LargeRiver,
    River,
    Plain,
    Hill,
    Forest,
    Mountain,
}
