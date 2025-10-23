using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SelectPlayerCharacterScreen : MainUIComponent, IScreen
{
    private Action<Character> onCharacterSelected;
    private Character characterInfoTarget;
    private WorldData world;
    private bool isShowingFreeList = false;

    public void Initialize()
    {
        CharacterTable.Initialize();
        CastleInfo.Initialize();

        // マウスオーバー時にキャラクター情報を更新
        CharacterTable.RowMouseMove += (sender, chara) =>
        {
            if (chara == characterInfoTarget) return;
            characterInfoTarget = chara;
            CharacterSummary.SetData(chara);
        };

        // キャラクター選択時
        CharacterTable.RowMouseDown += async (sender, chara) =>
        {
            if (chara == null) return;
            await OnCharacterSelected(chara);
        };

        // 浪士一覧ボタン
        buttonShowFreeList.clicked += () =>
        {
            if(isShowingFreeList)
            {
                isShowingFreeList = false;
                ShowInitialState();
                return;
            }
            isShowingFreeList = true;
            ShowFreeList();
        };

        // ランダムボタン
        buttonRandom.clicked += async () =>
        {
            if (world == null) return;
            var chara = world.Characters.RandomPick();
            await OnCharacterSelected(chara);
        };

        // 観戦モードボタン
        buttonWatch.clicked += async () =>
        {
            await OnCharacterSelected(null);
        };

    }

    public void Reinitialize()
    {
        Initialize();
    }

    public void Show(WorldData world, Action<Character> onCharacterSelected)
    {
        UI.Frame.Root.style.display = DisplayStyle.None;

        this.world = world;
        this.onCharacterSelected = onCharacterSelected;
        isShowingFreeList = false;

        // 観戦モードの有効化判定（ゲームクリア済みなら有効）
        var cleared = GameCore.GameCleared;
        buttonWatch.SetEnabled(cleared);
        if (cleared)
        {
            buttonWatch.text = "観戦モード";
        }
        else
        {
            buttonWatch.text = "観戦モード（クリア後解放）";
        }

        // マップクリックハンドラを登録
        world.Map.SetCustomEventHandler((tile) =>
        {
            OnMapTileClicked(tile);
        });

        ShowInitialState();

        UI.HideAllPanels();
        Root.style.display = DisplayStyle.Flex;
    }

    private void ShowInitialState()
    {
        isShowingFreeList = false;
        labelDescription.text = "城タイルをクリックして操作キャラを選択してください";
        CastleInfo.Root.style.display = DisplayStyle.None;
        CharacterTable.Root.style.display = DisplayStyle.None;
        CharacterSummary.Root.style.display = DisplayStyle.None;
    }

    private void OnMapTileClicked(GameMapTile tile)
    {
        isShowingFreeList = false;

        // 城タイルの場合
        if (tile.Castle != null)
        {
            ShowCastleCharacters(tile.Castle);
        }
        // 空白タイルの場合
        else
        {
            ShowInitialState();
        }
    }

    private void ShowCastleCharacters(Castle castle)
    {
        labelDescription.text = "一覧表から操作するキャラをクリックしてください";

        // 城情報を表示
        CastleInfo.Root.style.display = DisplayStyle.Flex;
        CastleInfo.SetData(castle, isClickable: false, isSelected: false);

        // 城のメンバーのみを表示
        var castleMembers = castle.Members.OrderBy(m => m.OrderIndex).ToList();
        CharacterTable.Root.style.display = DisplayStyle.Flex;
        CharacterTable.SetData(castleMembers, _ => true, false);

        // 最初のキャラの情報を表示
        characterInfoTarget = castleMembers.FirstOrDefault();
        if (characterInfoTarget != null)
        {
            CharacterSummary.Root.style.display = DisplayStyle.Flex;
            CharacterSummary.SetData(characterInfoTarget);
        }
    }

    private void ShowFreeList()
    {
        isShowingFreeList = true;

        var frees = world.Characters.Where(c => c.IsFree).OrderBy(c => c.Name).ToList();
        if (frees.Count == 0)
        {
            labelDescription.text = "浪士はいません";
            CastleInfo.Root.style.display = DisplayStyle.None;
            CharacterTable.Root.style.display = DisplayStyle.None;
            CharacterSummary.Root.style.display = DisplayStyle.None;
            return;
        }

        labelDescription.text = "一覧表から操作するキャラをクリックしてください";
        CastleInfo.Root.style.display = DisplayStyle.None;

        CharacterTable.Root.style.display = DisplayStyle.Flex;
        CharacterTable.SetData(frees, _ => true, false);

        characterInfoTarget = frees.FirstOrDefault();
        if (characterInfoTarget != null)
        {
            CharacterSummary.Root.style.display = DisplayStyle.Flex;
            CharacterSummary.SetData(characterInfoTarget);
        }
    }

    private async ValueTask OnCharacterSelected(Character chara)
    {
        var message = chara != null
            ? $"「{chara.GetTitle()} {chara.Name}」でゲームを始めます。\nよろしいですか？"
            : "観戦モードでゲームを始めます。\nよろしいですか？";

        var ok = await MessageWindow.ShowOkCancel(message);
        if (!ok) return;

        UI.Frame.Root.style.display = DisplayStyle.Flex;
        Root.style.display = DisplayStyle.None;
        world.Map.ClearCustomEventHandler();
        onCharacterSelected?.Invoke(chara);
    }

    public void Render()
    {
    }
}
