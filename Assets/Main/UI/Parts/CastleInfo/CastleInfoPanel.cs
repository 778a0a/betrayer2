using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CastleInfoPanel
{
    private GameCore Core => GameCore.Instance;
    private GameMapTile targetTile;
    private Castle targetCastle;

    public CastleInfoTabType CurrentTab { get; set; }
    private Button CurrentTabButton => CurrentTab switch
    {
        CastleInfoTabType.Castle => TabButtonCastle,
        CastleInfoTabType.Country => TabButtonCountry,
        CastleInfoTabType.Force => TabButtonForce,
        _ => throw new NotImplementedException(),
    };

    public void Initialize()
    {
        TabButtonCastle.clicked += () => SwitchTab(CastleInfoTabType.Castle);
        TabButtonCountry.clicked += () => SwitchTab(CastleInfoTabType.Country);
        TabButtonForce.clicked += () => SwitchTab(CastleInfoTabType.Force);

        CastleDetailTab.Initialize();
        CountryDetailTab.Initialize();
        ForceDetailTab.Initialize();

        SwitchTab(CastleInfoTabType.Castle);
    }

    public void SwitchTab(CastleInfoTabType tab)
    {
        CurrentTab = tab;

        // タブボタンの色を更新する。
        TabButtonCastle.RemoveFromClassList("active");
        TabButtonCountry.RemoveFromClassList("active");
        TabButtonForce.RemoveFromClassList("active");
        CurrentTabButton.AddToClassList("active");

        CastleInfoTab.style.display = Util.Display(CurrentTab == CastleInfoTabType.Castle);
        CountryInfoTab.style.display = Util.Display(CurrentTab == CastleInfoTabType.Country);
        ForceInfoTab.style.display = Util.Display(CurrentTab == CastleInfoTabType.Force);
    }

    public void SetData(GameMapTile tile, Character characterSummaryTargetDefault)
    {
        targetTile = tile;
        targetCastle = tile.Castle;
        CastleDetailTab.SetData(targetCastle, characterSummaryTargetDefault);
        CountryDetailTab.SetData(targetCastle?.Country);
        ForceDetailTab.SetData(targetTile);

        Render();
    }

    private void Render()
    {
        CastleDetailTab.Render();
        CountryDetailTab.Render();
        ForceDetailTab.Render();
    }
}

public enum CastleInfoTabType
{
    Castle,
    Country,
    Force,
}
