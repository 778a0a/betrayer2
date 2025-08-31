using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ForceDetailTab
{
    private GameCore Core => GameCore.Instance;
    private GameMapTile targetTile;

    public void Initialize()
    {
        ForceListViewTable.Initialize();
    }

    public void SetData(GameMapTile tile)
    {
        targetTile = tile;
        Render();
    }

    public void Render()
    {
        if (targetTile == null)
        {
            Root.style.display = DisplayStyle.None;
            return;
        }
        Root.style.display = DisplayStyle.Flex;

        // そのタイルにいる軍勢を取得
        var forces = Core.World.Forces
            .Where(f => f.Position == targetTile.Position)
            .ToList();

        // 軍勢数表示
        labelForceCount.text = forces.Count.ToString();

        // 軍勢一覧表示
        ForceListViewTable.SetData(forces, true);
    }
}