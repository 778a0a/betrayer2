using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class BonusScreen : MainUIComponent, IScreen
{
    private ValueTaskCompletionSource tcs;
    private Character characterInfoTarget;
    private Action<List<Character>> onSelectionChanged;
    private Func<List<Character>, ValueTask> onConfirmClicked;
    private IList<Character> charas;
    private Character actor;

    public void Initialize()
    {
        CharacterTable.Initialize();

        // マウスオーバー時にキャラクター情報を更新
        CharacterTable.RowMouseMove += (sender, chara) =>
        {
            if (chara == characterInfoTarget) return;
            characterInfoTarget = chara;
            CharacterSummary.SetData(chara);
        };

        // 複数選択時の選択変更イベント
        CharacterTable.SelectionChanged += (sender, selectedList) =>
        {
            onSelectionChanged?.Invoke(selectedList);
        };

        // 忠誠下位5人を選択ボタン
        buttonSelectLowestLoyalty.clicked += async () =>
        {
            if (charas == null || charas.Count == 0) return;

            var count = Math.Min(5, actor.ActionPoints / StrategyActions.BonusAction.APCostUnit);
            var sortedCharas = CharacterTable.charas
                // チェックボックスがオンの場合、忠誠105以下のキャラのみに絞り込む。
                .Where(c => !toggleFilterHighLoyalty.value || (int)c.Loyalty <= 105)
                .Take(count)
                .ToList();
            if (sortedCharas.Count == 0) return;

            CharacterTable.SetSelection(sortedCharas);
            labelDescription.visible = false;
            buttonSelectLowestLoyalty.SetEnabled(false);

            await Awaitable.WaitForSecondsAsync(0.1f);
            Execute(sortedCharas);
            await Awaitable.WaitForSecondsAsync(0.1f);
            ClearSelection();
            labelDescription.visible = true;
        };

        // 実行ボタン
        buttonConfirm.clicked += () =>
        {
            var selected = CharacterTable.GetSelectedCharacters();
            Execute(selected);
        };

        void Execute(List<Character> selected)
        {
            onConfirmClicked?.Invoke(selected);
            // 忠誠度順に並び替え直す。
            charas = charas.OrderBy(c => c.Loyalty).ToList();
            onSelectionChanged?.Invoke(selected);
            Render();
        }

        // 選択クリア
        void ClearSelection()
        {
            CharacterTable.ClearSelection();
            onSelectionChanged?.Invoke(new List<Character>());
        }

        // 閉じるボタン
        buttonClose.clicked += () =>
        {
            tcs.SetResult();
        };

        CharacterTable.SetMultiSelectMode(true);
    }

    public void Reinitialize()
    {
        Initialize();
    }

    public async ValueTask Show(
        Character actor,
        IList<Character> initialCharacterList,
        Action<List<Character>> onSelectionChanged,
        Func<List<Character>, ValueTask> onConfirmClicked)
    {
        using var _ = Core.World.Map.DisableClickEventHandler();
        tcs = new();
        this.onSelectionChanged = onSelectionChanged;
        this.onConfirmClicked = onConfirmClicked;
        charas = initialCharacterList;
        this.actor = actor;

        CharacterTable.ClearSelection();
        Render();

        UI.HideAllPanels();
        Root.style.display = DisplayStyle.Flex;
        await tcs.Task;
        Root.style.display = DisplayStyle.None;
    }

    public void Render()
    {
        CharacterTable.SetData(charas, _ => true, true);
        if (charas != null && charas.Count > 0)
        {
            CharacterSummary.SetData(charas[0]);
        }
    }
}
