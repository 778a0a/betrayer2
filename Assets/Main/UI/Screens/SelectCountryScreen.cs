using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SelectCountryScreen : MainUIComponent, IScreen
{
    private ValueTaskCompletionSource<Country> tcs;
    private Predicate<Country> predCanSelect;
    private IList<Country> currentCountries;

    public void Initialize()
    {
        CountryTable.Initialize();

        // 選択された場合
        CountryTable.RowMouseDown += (sender, country) =>
        {
            if (!(predCanSelect?.Invoke(country) ?? true)) return;
            tcs.SetResult(country);
        };

        // テーブル行のマウスオーバーでハイライト
        CountryTable.RowMouseEnter += (sender, index) =>
        {
            if (index < 0 || index >= currentCountries.Count) return;
            var country = currentCountries[index];
            // 国の全ての城をハイライト
            foreach (var castle in country.Castles)
            {
                var tile = Core.World.Map.GetTile(castle.Position);
                tile.UI.SetFocusHighlight(true);
            }
        };

        // テーブル行のマウスリーブでハイライト解除
        CountryTable.RowMouseLeave += (sender, index) =>
        {
            if (index < 0 || index >= currentCountries.Count) return;
            var country = currentCountries[index];
            // 国の全ての城のハイライト解除
            foreach (var castle in country.Castles)
            {
                var tile = Core.World.Map.GetTile(castle.Position);
                tile.UI.SetFocusHighlight(false);
            }
        };

        // キャンセルされた場合
        buttonClose.clicked += () =>
        {
            tcs.SetResult(null);
        };
    }

    public void Reinitialize()
    {
        Initialize();
    }

    public async ValueTask<Country> Show(
        string description,
        string cancelText,
        IList<Country> countries,
        Predicate<Country> predCanSelect)
    {
        tcs = new();
        this.predCanSelect = predCanSelect;
        this.currentCountries = countries;

        // 国の城をすべてハイライト対象に設定
        var allCastles = countries.SelectMany(c => c.Castles).ToList();
        Core.World.Map.SetEnableHighlight(allCastles);
        
        // マップクリック処理を設定
        Core.World.Map.SetCustomEventHandler(tile =>
        {
            if (!tile.HasCastle) return;
            var clickedCountry = tile.Castle.Country;
            var isInTargets = countries.Contains(clickedCountry);
            if (isInTargets && (predCanSelect?.Invoke(clickedCountry) ?? true))
            {
                tcs.SetResult(clickedCountry);
            }
        });

        (_Render = () =>
        {
            labelDescription.text = description;
            buttonClose.text = cancelText;
            // 国情報テーブル
            CountryTable.SetData(countries, predCanSelect);
        }).Invoke();

        UI.HideAllPanels();
        Root.style.display = DisplayStyle.Flex;
        var result = await tcs.Task;
        Root.style.display = DisplayStyle.None;

        // クリーンアップ
        Core.World.Map.ClearAllEnableHighlight();
        Core.World.Map.ClearCustomEventHandler();

        Debug.Log($"SelectCountryScreen.Show: Result = {result?.Ruler?.Name ?? "null"}");
        return result;
    }

    private Action _Render;
    public void Render()
    {
        _Render?.Invoke();
    }
}