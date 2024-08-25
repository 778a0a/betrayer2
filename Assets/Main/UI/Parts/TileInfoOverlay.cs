using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class TileInfoOverlay
{
    public void Initialize()
    {
    }

    public void SetData(GameMapTile tile)
    {
        if (tile == null)
        {
            Root.style.display = DisplayStyle.None;
            return;
        }
        Root.style.display = DisplayStyle.Flex;

        labelTileTerrain.text = tile.Terrain.ToString();
        labelTileHasCastle.style.display = Util.Display(tile.Castle.Exists);
        labelTileHasTown.style.display = Util.Display(tile.Town.Exists);

        var country = tile.Country;
        var hasCountry = country != null;
        labelTileOwnerParent.style.display = Util.Display(hasCountry);
        if (hasCountry)
        {
            labelTileOwner.text = country.GetTerritoryName();
            imageTileCountryColor.style.backgroundImage = new StyleBackground(country.Sprite);
        }

        labelTileForceParent.style.display = Util.Display(false);
    }
}