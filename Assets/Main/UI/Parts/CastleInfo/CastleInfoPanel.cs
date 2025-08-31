using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CastleInfoPanel
{
    private GameCore Core => GameCore.Instance;
    private GameMapTile targetTile;
    private Castle targetCastle;

    private InfoTab currentTab;
    private Button CurrentTabButton => currentTab switch
    {
        InfoTab.Castle => TabButtonCastle,
        InfoTab.Country => TabButtonCountry,
        InfoTab.Force => TabButtonForce,
        _ => throw new NotImplementedException(),
    };
    private enum InfoTab
    {
        Castle,
        Country,
        Force,
    }

    public void Initialize()
    {
        TabButtonCastle.clicked += () => SwitchTab(InfoTab.Castle);
        TabButtonCountry.clicked += () => SwitchTab(InfoTab.Country);
        TabButtonForce.clicked += () => SwitchTab(InfoTab.Force);

        CastleDetailTab.Initialize();
        CountryDetailTab.Initialize();
        ForceDetailTab.Initialize();

        SwitchTab(InfoTab.Castle);
    }

    private void SwitchTab(InfoTab tab)
    {
        currentTab = tab;

        // タブボタンの色を更新する。
        TabButtonCastle.RemoveFromClassList("active");
        TabButtonCountry.RemoveFromClassList("active");
        TabButtonForce.RemoveFromClassList("active");
        CurrentTabButton.AddToClassList("active");

        CastleInfoTab.style.display = Util.Display(currentTab == InfoTab.Castle);
        CountryInfoTab.style.display = Util.Display(currentTab == InfoTab.Country);
        ForceInfoTab.style.display = Util.Display(currentTab == InfoTab.Force);
    }

    public void SetData(GameMapTile tile, Character characterSummaryTargetDefault)
    {
        targetTile = tile;
        targetCastle = tile.Castle;
        if (targetCastle != null)
        {
            CastleDetailTab.SetData(targetCastle, characterSummaryTargetDefault);
            CountryDetailTab.SetData(targetCastle?.Country);
        }
        ForceDetailTab.SetData(targetTile);

        Render();
    }

    private void Render()
    {
        if (targetCastle == null)
        {
            Root.style.display = DisplayStyle.None;
            return;
        }
        Root.style.display = DisplayStyle.Flex;

        CastleDetailTab.Render();
        CountryDetailTab.Render();
        ForceDetailTab.Render();
    }



}