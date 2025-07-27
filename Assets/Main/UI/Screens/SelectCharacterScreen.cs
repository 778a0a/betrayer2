using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SelectCharacterScreen : MainUIComponent, IScreen
{
    private ValueTaskCompletionSource<Character> tcs;
    private Character characterInfoTarget;
    private Predicate<Character> predCanSelect;

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
            tcs.SetResult(chara);
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

    public async ValueTask<Character> Show(
        string description,
        string cancelText,
        IList<Character> charas,
        Predicate<Character> predCanSelect)
    {
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

    private Action _Render;
    public void Render()
    {
        _Render?.Invoke();
    }

}