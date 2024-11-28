using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameMapManager
{
    private WorldData world;
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

    public void AttachWorld(WorldData world)
    {
        this.world = world;
        foreach (var tile in Tiles)
        {
            tile.AttachWorld(world);
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
        if (tiles.TryGetValue(entity.Position, out var tile))
        {
            return tile;
        }
        Debug.LogError($"タイルが見つかりません: {entity}");
        // デバッグ中の場合はポーズ状態にする。
#if UNITY_EDITOR        
        Debug.Break();
#endif
        throw new InvalidOperationException($"GetTileエラー ({entity.Position})");
    }
    public GameMapTile TryGetTile(MapPosition pos)
    {
        tiles.TryGetValue(pos, out var tile);
        return tile;
    }

    public void RegisterCastle(Country country, Castle castle)
    {
        var tile = GetTile(castle);
        tile.Castle = castle;

        castle.UpdateCountry(country);
        //Debug.Log($"城({castle.Id})を登録しました。");
    }

    public void ReregisterCastle(MapPosition newPos, Castle castle)
    {
        var origTile = GetTile(castle);
        var newTile = GetTile(newPos);

        origTile.Castle = null;
        castle.Position = newPos;
        newTile.Castle = castle;
    }

    public void UnregisterCastle(Castle castle)
    {
        foreach (var town in castle.Towns)
        {
            var townTile = GetTile(town);
            townTile.Town = null;
        }

        var tile = GetTile(castle);
        tile.Castle = null;
        castle.UpdateCountry(null);

        var members = castle.Members.ToList();
        var otherCastle = castle.Country.Castles.FirstOrDefault();
        if (members.Count > 0 && otherCastle != null)
        {
            Debug.LogWarning($"城が削除されたため、所属キャラを移動します。");
            foreach (var member in members)
            {
                member.ChangeCastle(otherCastle, false);
            }
        }
        Debug.Log($"城({castle.Id})が削除されました。");
    }

    public void RegisterTown(Castle castle, Town town)
    {
        var tile = GetTile(town);
        tile.Town = town;

        town.Castle = castle;
        castle.Towns.Add(town);
        town.FoodIncomeMaxBase = Town.TileFoodMax(tile, castle.Towns.Count);
        town.GoldIncomeMaxBase = Town.TileGoldMax(tile, castle.Towns.Count);
    }

    public void UnregisterTown(Town town)
    {
        var tile = GetTile(town);
        tile.Town = null;

        town.Castle.Towns.Remove(town);
        Debug.Log($"町({town.Position}, 城ID:{town.Castle.Id})が削除されました。");
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
