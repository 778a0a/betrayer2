using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SelectCharacterPanel : IPanel
{
    private ValueTaskCompletionSource<Character> tcs;
    private Character characterInfoTarget;
    private WorldData world;
    private Predicate<Character> predCanSelect;

    public LocalizationManager L => MainUI.Instance.L;

    public void Initialize()
    {
        CharacterTable.Initialize();
        CharacterInfo.Initialize();

        // マウスオーバー時
        CharacterTable.RowMouseMove += (sender, chara) =>
        {
            if (chara == characterInfoTarget) return;
            characterInfoTarget = chara;
            CharacterInfo.SetData(chara);
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

    public async ValueTask<Character> Show(
        string description,
        string cancelText,
        IList<Character> charas,
        WorldData world, 
        Predicate<Character> predCanSelect)
    {
        tcs = new ValueTaskCompletionSource<Character>();
        this.world = world;
        this.predCanSelect = predCanSelect;

        labelDescription.text = description;
        buttonClose.text = cancelText;

        // 人物情報テーブル
        CharacterTable.SetData(charas, world, predCanSelect);
        
        // 人物詳細
        if (charas != null && charas.Count > 0)
        {
            CharacterInfo.SetData(charas[0]);
        }

        // パネルを表示
        Root.style.display = DisplayStyle.Flex;

        var result = await tcs.Task;

        // パネルを非表示
        Root.style.display = DisplayStyle.None;

        return result;
    }

}