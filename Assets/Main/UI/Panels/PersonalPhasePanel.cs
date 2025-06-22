using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 個人行動フェーズのUIパネル部品
/// MainUIの一部として動作する
/// </summary>
public partial class PersonalPhasePanel
{
    private GameCore gameCore;
    private Character currentCharacter;
    private PersonalActionBase selectedAction;

    /// <summary>
    /// パネルの初期化
    /// </summary>
    public void Initialize()
    {
        // TODO: Rosalina自動生成後に実装
        // - イベントハンドラーの設定
        // - アクションボタンのクリックイベント
        // - 実行ボタンのクリックイベント
        SetupActionButtons();
    }

    /// <summary>
    /// GameCoreインスタンスを設定
    /// </summary>
    public void SetGameCore(GameCore core)
    {
        gameCore = core;
    }

    /// <summary>
    /// データを設定してパネルを更新
    /// </summary>
    /// <param name="character">表示するキャラクター</param>
    /// <param name="gameDate">現在のゲーム日付</param>
    public void SetData(Character character, GameDate gameDate)
    {
        currentCharacter = character;
        
        if (character == null) return;

        // TODO: Rosalina自動生成後に実装
        UpdateDateDisplay(gameDate);
        UpdatePlayerInfo(character);
        UpdateCharacterStats(character);
        UpdateActionButtons(character);
        UpdateSoldierDisplay(character);
    }

    /// <summary>
    /// 日付表示を更新
    /// </summary>
    private void UpdateDateDisplay(GameDate gameDate)
    {
        // TODO: 実装
        // labelDate.text = $"{gameDate.Year}年";
        // labelMonth.text = $"{gameDate.Month}月";
        // labelDay.text = $"{gameDate.Day}日";
    }

    /// <summary>
    /// プレイヤー情報を更新
    /// </summary>
    private void UpdatePlayerInfo(Character character)
    {
        // TODO: 実装
        // labelPlayerName.text = character.Name;
        // labelPlayerGold.text = character.Gold.ToString("N0");
        // labelPlayerAP.text = $"{character.ActionPoints}/{character.MaxActionPoints}";
        // imagePlayerFace.image = FaceImageManager.Instance.GetImage(character);
    }

    /// <summary>
    /// キャラクター能力値を更新
    /// </summary>
    private void UpdateCharacterStats(Character character)
    {
        // TODO: 実装
        // labelAttack.text = character.Attack.ToString();
        // labelDefense.text = character.Defense.ToString();
        // labelIntelligence.text = character.Intelligence.ToString();
        // labelGoverning.text = character.Governing.ToString();
        // labelLoyalty.text = character.Loyalty.ToString();
        // labelSoldierCount.text = character.Soldiers.SoldierCount.ToString();
    }

    /// <summary>
    /// アクションボタンの状態を更新
    /// </summary>
    private void UpdateActionButtons(Character character)
    {
        // TODO: 実装
        // 各アクションの実行可能性をチェックして、ボタンの有効/無効を設定
    }

    /// <summary>
    /// 兵士表示を更新
    /// </summary>
    private void UpdateSoldierDisplay(Character character)
    {
        // TODO: 実装
        // 兵士スロットの色や状態を兵士のHPに応じて更新
    }

    /// <summary>
    /// アクションボタンの設定
    /// </summary>
    private void SetupActionButtons()
    {
        // TODO: Rosalina自動生成後に実装
        // buttonDevelop.clicked += () => OnActionSelected("Develop");
        // buttonFortify.clicked += () => OnActionSelected("Fortify");
        // buttonHireSoldier.clicked += () => OnActionSelected("HireSoldier");
        // buttonInvest.clicked += () => OnActionSelected("Invest");
        // buttonTrainSoldier.clicked += () => OnActionSelected("TrainSoldier");
        // buttonRebel.clicked += () => OnActionSelected("Rebel");
        // buttonResign.clicked += () => OnActionSelected("Resign");
        // buttonExecute.clicked += OnExecuteAction;
    }

    /// <summary>
    /// アクションが選択された時の処理
    /// </summary>
    private void OnActionSelected(string actionName)
    {
        // TODO: 実装
        // selectedAction = gameCore.PersonalActions.GetAction(actionName);
        // UpdateActionDetail(selectedAction);
    }

    /// <summary>
    /// アクション詳細表示を更新
    /// </summary>
    private void UpdateActionDetail(PersonalActionBase action)
    {
        // TODO: 実装
        // labelActionName.text = action.Label;
        // labelActionDescription.text = action.Description;
        // var cost = action.Cost(new ActionArgs(currentCharacter));
        // labelCostGold.text = cost.actorGold.ToString();
        // labelCostAP.text = cost.actionPoints.ToString();
        // buttonExecute.SetEnabled(action.CanDo(new ActionArgs(currentCharacter)));
    }

    /// <summary>
    /// アクション実行処理
    /// </summary>
    private void OnExecuteAction()
    {
        // TODO: 実装
        // if (selectedAction != null && currentCharacter != null)
        // {
        //     var args = new ActionArgs(currentCharacter);
        //     await selectedAction.Do(args);
        //     // UI更新
        //     SetData(currentCharacter, gameCore.GameDate);
        // }
    }

    /// <summary>
    /// パネルの表示/非表示を切り替え
    /// </summary>
    public void SetVisible(bool visible)
    {
        // TODO: Rosalina自動生成後に実装
        // PersonalPhasePanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}