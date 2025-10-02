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
        buttonSelectLowestLoyalty.clicked += () =>
        {
            if (charas == null || charas.Count == 0) return;

            var count = Math.Min(5, actor.ActionPoints / StrategyActions.BonusAction.APCostUnit);
            var sortedCharas = charas.OrderBy(c => c.Loyalty).Take(count).ToList();

            // 現在の選択と同じなら、選択を解除する。
            if (CharacterTable.GetSelectedCharacters().OrderBy(c => c.Name).SequenceEqual(sortedCharas.OrderBy(c => c.Name)))
            {
                CharacterTable.ClearSelection();
                return;
            }

            CharacterTable.SetSelection(sortedCharas);
            onSelectionChanged?.Invoke(sortedCharas);
        };

        // 実行ボタン
        buttonConfirm.clicked += () =>
        {
            var selected = CharacterTable.GetSelectedCharacters();
            onConfirmClicked?.Invoke(selected);
            // 忠誠度順に並び替え直す。
            charas = charas.OrderBy(c => c.Loyalty).ToList();
            onSelectionChanged?.Invoke(selected);
            Render();
        };

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
        CharacterTable.SetData(charas, _ => true);
        if (charas != null && charas.Count > 0)
        {
            CharacterSummary.SetData(charas[0]);
        }
    }
}
