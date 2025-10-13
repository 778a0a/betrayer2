using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SelectCharacterScreen : MainUIComponent, IScreen
{
    private ValueTaskCompletionSource<Character> tcs;
    private ValueTaskCompletionSource<List<Character>> multiTcs;
    private Character characterInfoTarget;
    private Predicate<Character> predCanSelect;
    private bool isMultiSelectMode = false;
    private Action<List<Character>> onSelectionChanged;

    public void Initialize()
    {
        CharacterTable.Initialize();

        // マウスオーバー時
        CharacterTable.RowMouseMove += (sender, chara) =>
        {
            if (chara == characterInfoTarget) return;
            characterInfoTarget = chara;
            CharacterSummary.SetData(chara);
        };

        // 選択された場合
        CharacterTable.RowMouseDown += (sender, chara) =>
        {
            if (!(predCanSelect?.Invoke(chara) ?? true)) return;
            if (!isMultiSelectMode)
            {
                tcs.SetResult(chara);
            }
        };

        // 複数選択の選択変更イベント
        CharacterTable.SelectionChanged += (sender, selectedList) =>
        {
            if (isMultiSelectMode)
            {
                onSelectionChanged?.Invoke(selectedList);
            }
        };

        // 決定ボタン（複数選択モード用）
        buttonConfirm.clicked += () =>
        {
            if (isMultiSelectMode)
            {
                var selected = CharacterTable.GetSelectedCharacters();
                multiTcs?.SetResult(selected);
            }
        };

        // キャンセルされた場合
        buttonClose.clicked += () =>
        {
            if (isMultiSelectMode)
            {
                multiTcs?.SetResult(null);
            }
            else
            {
                tcs?.SetResult(null);
            }
        };
    }

    public void Reinitialize()
    {
        Initialize();
    }

    public async ValueTask<Character> Show(
        string description,
        string cancelText,
        IList<Character> charas,
        Predicate<Character> predCanSelect)
    {
        using var _ = Core.World.Map.DisableClickEventHandler();
        tcs = new();
        this.predCanSelect = predCanSelect;

        (_Render = () =>
        {
            labelDescription.text = description;
            buttonClose.text = cancelText;
            // 人物情報テーブル
            CharacterTable.SetData(charas, predCanSelect);
            // 人物詳細
            if (charas != null && charas.Count > 0)
            {
                CharacterSummary.SetData(charas[0]);
            }
        }).Invoke();

        UI.HideAllPanels();
        Root.style.display = DisplayStyle.Flex;
        var result = await tcs.Task;
        Root.style.display = DisplayStyle.None;

        Debug.Log($"SelectCharacterScreen.Show: Result = {result?.Name ?? "null"}");
        return result;
    }

    public async ValueTask<List<Character>> SelectMultiple(
        string description,
        string confirmText,
        string cancelText,
        IList<Character> charas,
        Predicate<Character> predCanSelect,
        Action<List<Character>> onSelectionChanged = null)
    {
        using var _ = Core.World.Map.DisableClickEventHandler();
        multiTcs = new();
        this.predCanSelect = predCanSelect;
        this.onSelectionChanged = onSelectionChanged;
        isMultiSelectMode = true;

        // 複数選択モードを有効化
        CharacterTable.SetMultiSelectMode(true);

        (_Render = () =>
        {
            labelDescription.text = description;
            buttonConfirm.text = confirmText;
            buttonConfirm.style.display = DisplayStyle.Flex;
            buttonClose.text = cancelText;
            
            // 人物情報テーブル
            CharacterTable.SetData(charas, predCanSelect);
            // 人物詳細
            if (charas != null && charas.Count > 0)
            {
                CharacterSummary.SetData(charas[0]);
            }
        }).Invoke();

        UI.HideAllPanels();
        Root.style.display = DisplayStyle.Flex;
        var result = await multiTcs.Task;
        Root.style.display = DisplayStyle.None;

        // 複数選択モードを無効化
        CharacterTable.SetMultiSelectMode(false);
        isMultiSelectMode = false;
        buttonConfirm.style.display = DisplayStyle.None;

        Debug.Log($"SelectCharacterScreen.SelectMultiple: Result = {result?.Count ?? 0} characters");
        return result;
    }

    private Action _Render;
    public void Render()
    {
        _Render?.Invoke();
    }

}