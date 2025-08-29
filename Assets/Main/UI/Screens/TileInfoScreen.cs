using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class TileInfoScreen : IScreen
{
    private GameCore Core => GameCore.Instance;
    private MapPosition currentTilePosition;

    public void Initialize()
    {
        buttonClose.clicked += () =>
        {
            Root.style.display = DisplayStyle.None;
        };

        CastleInfoPanel.Initialize();
    }

    public void Reinitialize()
    {
        buttonClose.clicked += () =>
        {
            Root.style.display = DisplayStyle.None;
        };
        CastleInfoPanel.Initialize();
    }

    public void Show(MapPosition tilePosition)
    {
        Core.MainUI.HideAllPanels();
        SetData(tilePosition);
        Root.style.display = DisplayStyle.Flex;
    }

    public void SetData(MapPosition tilePosition)
    {
        currentTilePosition = tilePosition;
        Render();
    }

    public void Render()
    {
        var tile = Core.World.Map.GetTile(currentTilePosition);
        if (tile.HasCastle)
        {
            CastleInfoPanel.SetData(tile.Castle, tile.Castle.Boss);
        }
        else
        {
            // TODO
        }
    }
}