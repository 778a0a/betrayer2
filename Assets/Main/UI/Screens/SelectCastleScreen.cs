using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SelectCastleScreen : MainUIComponent, IScreen
{
    private ValueTaskCompletionSource<GameMapTile> tcs;
    private Predicate<GameMapTile> predCanSelect;
    private Func<GameMapTile, ValueTask<bool>> onSelect;
    private IList<Castle> currentCastles;

    public void Initialize()
    {
        CastleTable.Initialize();

        // 選択された場合
        CastleTable.RowMouseDown += (sender, castle) =>
        {
            if (!(predCanSelect?.Invoke(castle.Tile) ?? true)) return;
            OnSelect(castle.Tile);
        };

        // テーブル行のマウスオーバーでハイライト
        CastleTable.RowMouseEnter += (sender, index) =>
        {
            if (index < 0 || index >= currentCastles.Count) return;
            var castle = currentCastles[index];
            var tile = Core.World.Map.GetTile(castle.Position);
            tile.UI.SetFocusHighlight(true);
        };

        // テーブル行のマウスリーブでハイライト解除
        CastleTable.RowMouseLeave += (sender, index) =>
        {
            if (index < 0 || index >= currentCastles.Count) return;
            var castle = currentCastles[index];
            var tile = Core.World.Map.GetTile(castle.Position);
            tile.UI.SetFocusHighlight(false);
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

    /// <summary>
    /// 進軍先の城を選択します。
    /// </summary>
    public async ValueTask<GameMapTile> SelectDeployDestination(
        string description,
        string cancelText,
        IList<Castle> castles,
        Func<GameMapTile, ValueTask<bool>> onSelect)
    {
        return await SelectTile(
            description,
            cancelText,
            castles,
            tile => true,
            onSelect);
    }

    public async ValueTask<GameMapTile> SelectTile(
        string description,
        string cancelText,
        IList<Castle> castles,
        Predicate<GameMapTile> predCanSelect,
        Func<GameMapTile, ValueTask<bool>> onSelect)
    {
        tcs = new();
        currentCastles = castles;
        this.predCanSelect = predCanSelect;
        this.onSelect = onSelect;

        // マップハイライトを有効化
        Core.World.Map.SetEnableHighlight(castles);
        
        // マップクリック処理を設定
        Core.World.Map.SetCustomEventHandler(tile =>
        {
            if (predCanSelect?.Invoke(tile) ?? true)
            {
                OnSelect(tile);
            }
        });

        (_Render = () =>
        {
            labelDescription.text = description;
            buttonClose.text = cancelText;
            // 城情報テーブル
            CastleTable.SetData(castles, c => predCanSelect(c.Tile));
        }).Invoke();

        UI.HideAllPanels();
        Root.style.display = DisplayStyle.Flex;
        var result = await tcs.Task;
        Root.style.display = DisplayStyle.None;

        // クリーンアップ
        Core.World.Map.ClearAllEnableHighlight();
        Core.World.Map.ClearCustomEventHandler();

        Debug.Log($"SelectCastleScreen.SelectTile: Result = {result}");
        return result;
    }

    private async void OnSelect(GameMapTile tile)
    {
        if (onSelect == null) 
        {
            tcs.SetResult(tile);
            return;
        }
        if (await onSelect(tile))
        {
            tcs.SetResult(tile);
        }
    }

    private Action _Render;
    public void Render()
    {
        _Render?.Invoke();
    }
}