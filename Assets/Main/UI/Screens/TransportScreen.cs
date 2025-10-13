using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class TransportScreen : MainUIComponent, IScreen
{
    private ValueTaskCompletionSource tcs;
    private IList<Castle> currentCastles;
    private float currentAmount = 10;
    private float maxTransportAmount = 1000;
    private Castle selectedCastle;
    private Func<Castle, float, ValueTask<bool>> onConfirmClicked;
    private Func<float> getMaxAmountFunc;
    private Func<bool> canExecuteFunc;
    private bool isProcessingConfirm;

    public void Initialize()
    {
        CastleTable.Initialize();

        // 選択された場合
        CastleTable.RowMouseDown += (sender, castle) =>
        {
            if (currentCastles == null || !currentCastles.Contains(castle)) return;
            SelectCastle(castle);
        };

        // テーブル行のマウスオーバーでハイライト
        CastleTable.RowMouseEnter += (sender, index) =>
        {
            if (currentCastles == null) return;
            if (index < 0 || index >= currentCastles.Count) return;
            var castle = currentCastles[index];
            var tile = Core.World.Map.GetTile(castle.Position);
            tile.UI.SetFocusHighlight(true);
        };

        // テーブル行のマウスリーブでハイライト解除
        CastleTable.RowMouseLeave += (sender, index) =>
        {
            if (currentCastles == null) return;
            if (index < 0 || index >= currentCastles.Count) return;
            var castle = currentCastles[index];
            if (castle == selectedCastle) return;
            var tile = Core.World.Map.GetTile(castle.Position);
            tile.UI.SetFocusHighlight(false);
        };

        // スライダーの値変更時
        sliderAmount.RegisterCallback<ChangeEvent<float>>(evt =>
        {
            currentAmount = Mathf.RoundToInt(evt.newValue);
            labelAmount.text = currentAmount.ToString("0");
            labelCastleGold.text = getMaxAmountFunc().ToString("0");
            UpdateDescription();
            UpdateConfirmButtonState();
        });

        // +10ボタン
        buttonPlus10.clicked += () =>
        {
            currentAmount = Mathf.Clamp(currentAmount + 10, 0, maxTransportAmount);
            sliderAmount.SetValueWithoutNotify(currentAmount);
            labelAmount.text = currentAmount.ToString("0");
            labelCastleGold.text = getMaxAmountFunc().ToString("0");
            UpdateDescription();
            UpdateConfirmButtonState();
        };

        // -10ボタン
        buttonMinus10.clicked += () =>
        {
            currentAmount = Mathf.Clamp(currentAmount - 10, 0, maxTransportAmount);
            sliderAmount.SetValueWithoutNotify(currentAmount);
            labelAmount.text = currentAmount.ToString("0");
            labelCastleGold.text = getMaxAmountFunc().ToString("0");
            UpdateDescription();
            UpdateConfirmButtonState();
        };

        // 実行ボタン
        if (buttonConfirm != null)
        {
            buttonConfirm.clicked -= ConfirmSelection;
            buttonConfirm.clicked += ConfirmSelection;
        }

        // 閉じるボタン
        buttonClose.clicked -= OnCloseClicked;
        buttonClose.clicked += OnCloseClicked;

        UpdateDescription();
        UpdateConfirmButtonState();
    }

    public void Reinitialize()
    {
        Initialize();
    }

    /// <summary>
    /// 輸送先の城を選択し、輸送量を設定します。
    /// </summary>
    public async ValueTask Show(
        IList<Castle> castles,
        float initialAmount,
        Func<float> getMaxAmount,
        Func<bool> canExecute,
        Func<Castle, float, ValueTask<bool>> onConfirmClicked)
    {
        tcs = new();
        currentCastles = castles;
        this.onConfirmClicked = onConfirmClicked;
        getMaxAmountFunc = getMaxAmount;
        canExecuteFunc = canExecute;

        maxTransportAmount = Mathf.Max(0, getMaxAmountFunc?.Invoke() ?? 0f);
        currentAmount = Mathf.RoundToInt(Mathf.Clamp(initialAmount, 0, maxTransportAmount));
        ClearSelection();

        // マップハイライトを有効化
        Core.World.Map.SetEnableHighlight(castles);

        // マップクリック処理を設定
        Core.World.Map.SetCustomEventHandler(tile =>
        {
            if (!tile.HasCastle) return;
            if (!currentCastles.Contains(tile.Castle)) return;
            SelectCastle(tile.Castle);
        });

        (_Render = () =>
        {
            UpdateAvailableAmount();
            sliderAmount.SetValueWithoutNotify(currentAmount);
            labelAmount.text = currentAmount.ToString("0");
            labelCastleGold.text = getMaxAmount().ToString("0");
            CastleTable.SetData(castles, c => true);
            CastleTable.ClearSelection();
            UpdateDescription();
            UpdateConfirmButtonState();
        })?.Invoke();

        UI.HideAllPanels();
        Root.style.display = DisplayStyle.Flex;
        await tcs.Task;
        Root.style.display = DisplayStyle.None;

        // クリーンアップ
        ClearSelection();
        onConfirmClicked = null;
        getMaxAmountFunc = null;
        canExecuteFunc = null;
        currentCastles = null;

        Core.World.Map.ClearAllEnableHighlight();
        Core.World.Map.ClearCustomEventHandler();
    }

    private void OnCloseClicked()
    {
        ClearSelection();
        tcs?.SetResult();
    }

    private async void ConfirmSelection()
    {
        if (selectedCastle == null) return;
        if (currentAmount <= 0) return;
        if (onConfirmClicked == null) return;
        if (isProcessingConfirm) return;

        isProcessingConfirm = true;
        try
        {
            var success = await onConfirmClicked(selectedCastle, currentAmount);
            if (success)
            {
                UpdateAvailableAmount();
                CastleTable.ListView?.RefreshItems();
                UpdateDescription();
            }
            UpdateConfirmButtonState();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            isProcessingConfirm = false;
        }
    }

    private void SelectCastle(Castle castle)
    {
        if (castle == null) return;

        if (selectedCastle != null && selectedCastle != castle)
        {
            var previousTile = Core.World.Map.GetTile(selectedCastle.Position);
            previousTile.UI.SetFocusHighlight(false);
        }

        selectedCastle = castle;
        CastleTable?.SetSelection(castle);

        var tile = Core.World.Map.GetTile(castle.Position);
        tile.UI.SetFocusHighlight(true);

        UpdateDescription();
        UpdateConfirmButtonState();
    }

    private void ClearSelection()
    {
        if (selectedCastle != null)
        {
            var tile = Core.World.Map.GetTile(selectedCastle.Position);
            tile.UI.SetFocusHighlight(false);
        }

        selectedCastle = null;
        CastleTable?.ClearSelection();

        UpdateDescription();
        UpdateConfirmButtonState();
    }

    private void UpdateAvailableAmount()
    {
        if (getMaxAmountFunc != null)
        {
            maxTransportAmount = Mathf.Max(0, getMaxAmountFunc());
        }

        sliderAmount.lowValue = 0;
        sliderAmount.highValue = maxTransportAmount;

        if (currentAmount > maxTransportAmount)
        {
            currentAmount = Mathf.RoundToInt(maxTransportAmount);
            sliderAmount.SetValueWithoutNotify(currentAmount);
            labelAmount.text = currentAmount.ToString("0");
            labelCastleGold.text = getMaxAmountFunc().ToString("0");
        }
    }

    private void UpdateDescription()
    {
        if (labelDescription == null) return;
        labelDescription.text = selectedCastle == null
            ? "輸送先の城を選択してください"
            : $"{selectedCastle.Name}に金{currentAmount:0}を輸送します";
    }

    private void UpdateConfirmButtonState()
    {
        var hasSelection = selectedCastle != null;
        var hasAmount = currentAmount > 0 && currentAmount <= maxTransportAmount;
        var canExecute = canExecuteFunc?.Invoke() ?? true;

        buttonConfirm.SetEnabled(hasSelection && hasAmount && canExecute);
    }

    private Action _Render;
    public void Render()
    {
        _Render?.Invoke();
    }
}
