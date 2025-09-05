using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class TransportScreen : MainUIComponent, IScreen
{
    private ValueTaskCompletionSource<(Castle castle, float amount)> tcs;
    private IList<Castle> currentCastles;
    private float currentAmount = 10;
    private float maxTransportAmount = 1000;

    public void Initialize()
    {
        CastleTable.Initialize();

        // 選択された場合
        CastleTable.RowMouseDown += (sender, castle) =>
        {
            if (!currentCastles.Contains(castle)) return;
            OnSelect(castle, currentAmount);
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

        // スライダーの値変更時
        sliderAmount.RegisterCallback<ChangeEvent<float>>(evt =>
        {
            currentAmount = Mathf.RoundToInt(evt.newValue);
            labelAmount.text = currentAmount.ToString("0");
        });

        // +10ボタン
        buttonPlus10.clicked += () =>
        {
            currentAmount = Mathf.Clamp(currentAmount + 10, 0, maxTransportAmount);
            sliderAmount.value = currentAmount;
            labelAmount.text = currentAmount.ToString("0");
        };

        // -10ボタン
        buttonMinus10.clicked += () =>
        {
            currentAmount = Mathf.Clamp(currentAmount - 10, 0, maxTransportAmount);
            sliderAmount.value = currentAmount;
            labelAmount.text = currentAmount.ToString("0");
        };

        // キャンセルされた場合
        buttonClose.clicked += () =>
        {
            tcs.SetResult((null, 0));
        };
    }

    public void Reinitialize()
    {
        Initialize();
    }

    /// <summary>
    /// 輸送先の城を選択し、輸送量を設定します。
    /// </summary>
    public async ValueTask<(Castle castle, float amount)> Show(
        IList<Castle> castles,
        float initialAmount,
        float maxAmount)
    {
        tcs = new();
        currentCastles = castles;
        currentAmount = initialAmount;
        maxTransportAmount = maxAmount;

        // マップハイライトを有効化
        Core.World.Map.SetEnableHighlight(castles);
        
        // マップクリック処理を設定
        Core.World.Map.SetCustomEventHandler(tile =>
        {
            if (!tile.HasCastle) return;
            if (!currentCastles.Contains(tile.Castle)) return;
            {
                OnSelect(tile.Castle, currentAmount);
            }
        });

        (_Render = () =>
        {
            // スライダー設定
            sliderAmount.lowValue = 0;
            sliderAmount.highValue = maxAmount;
            sliderAmount.value = currentAmount;
            labelAmount.text = currentAmount.ToString("0");
            
            // 城情報テーブル
            CastleTable.SetData(castles, c => true);
        }).Invoke();

        UI.HideAllPanels();
        Root.style.display = DisplayStyle.Flex;
        var result = await tcs.Task;
        Root.style.display = DisplayStyle.None;

        // クリーンアップ
        Core.World.Map.ClearAllEnableHighlight();
        Core.World.Map.ClearCustomEventHandler();

        Debug.Log($"TransportScreen.SelectTransportDestination: Result = {result}, Amount = {result.amount}");
        return result;
    }

    private void OnSelect(Castle castle, float amount)
    {
        tcs.SetResult((castle, amount));
    }

    private Action _Render;
    public void Render()
    {
        _Render?.Invoke();
    }
}