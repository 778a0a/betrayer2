using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SystemMenuWindow
{
    private GameCore Core => GameCore.Instance;

    public void Initialize()
    {
        Root.style.display = DisplayStyle.None;

        buttonClose.clicked += () =>
        {
            Root.style.display = DisplayStyle.None;
        };

        buttonSave.clicked += () =>
        {
            MessageWindow.Show("セーブ機能は未実装です。");
        };

        buttonChangePlayer.clicked += async () =>
        {
            var ok = await MessageWindow.ShowOkCancel("操作キャラを変更します。\nよろしいですか？");
            if (!ok) return;

            // 現在のプレイヤーフラグをクリア
            foreach (var chara in Core.World.Characters.Where(c => c.IsPlayer))
            {
                chara.IsPlayer = false;
            }

            Root.style.display = DisplayStyle.None;
            Core.MainUI.SelectPlayerCharacterScreen.Show(Core.World, chara =>
            {
                // 観戦モード
                if (chara == null)
                {
                    Debug.Log("観戦モードが選択されました。");
                    Core.IsWatchMode = true;
                    Core.Booter.hold = false;
                    MessageWindow.Show("観戦モードは未実装です。");
                }
                else
                {
                    chara.IsPlayer = true;
                    Debug.Log($"Player selected: {chara.Name}");
                    Core.Booter.hold = false;

                    Core.MainUI.ActionScreen.Show();
                }
            });

        };

        buttonGoToTitle.clicked += async () =>
        {
            var ok = await MessageWindow.ShowOkCancel("タイトル画面に戻ります。\nよろしいですか？");
            if (!ok) return;

            TitleSceneManager.LoadScene();
        };
    }

    public void Show()
    {
        Root.style.display = DisplayStyle.Flex;

        var cleared = GameCore.GameCleared;
        buttonChangePlayer.enabledSelf = cleared;
        if (!cleared)
        {
            buttonChangePlayer.text = "操作キャラ変更 (クリア後解放)";
            buttonChangePlayer.style.fontSize = 30;
        }
    }
}
