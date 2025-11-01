using System;
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
            try
            {
                Debug.Log("セーブします。");
                var core = GameCore.Instance;
                SaveDataManager.Instance.Save(core.SaveDataSlotNo, core, core.MainUI.ActionScreen.CurrentPhase);
                MessageWindow.Show("セーブしました。");
            }
            catch (Exception ex)
            {
                MessageWindow.Show($"セーブに失敗しました。\n({ex.Message})");
                Debug.LogError($"セーブに失敗しました。{ex}");
            }
        };

        buttonSystemSetting.clicked += () =>
        {
            Core.MainUI.SystemSettingWindow.Show();
        };

        buttonChangePlayer.clicked += async () =>
        {
            var ok = await MessageWindow.ShowOkCancel("操作キャラを変更します。\nよろしいですか？");
            if (!ok) return;

            Root.style.display = DisplayStyle.None;
            Core.MainUI.SelectPlayerCharacterScreen.Show(false, Core.World, chara =>
            {
                Debug.Log($"プレーヤー変更: {chara?.Name}");
                Core.World.SetPlayer(chara);
                Core.Booter.hold = false;
                Core.MainUI.ActionScreen.ActivatePhase(chara, Phase.Progress);
                Core.MainUI.ActionScreen.Show();
            });

        };

        buttonGoToTitle.clicked += async () =>
        {
            var ok = await MessageWindow.ShowOkCancel("タイトル画面に戻ります。\nよろしいですか？");
            if (!ok) return;

            _ = MessageWindow.Show("ゲーム終了中...", MessageBoxButton.None);
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
