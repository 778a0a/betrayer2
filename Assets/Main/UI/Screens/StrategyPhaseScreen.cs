using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class StrategyPhaseScreen : IScreen
{
    private GameCore Core => GameCore.Instance;
    private ActionButtonHelper[] buttons;
    private Character currentCharacter;

    public void Initialize()
    {
        buttons = new[]
        {
            ActionButtonHelper.Strategy(a => a.Deploy),
            ActionButtonHelper.Strategy(a => a.Transport),
            ActionButtonHelper.Strategy(a => a.Bonus),
            ActionButtonHelper.Strategy(a => a.HireVassal),
            ActionButtonHelper.Strategy(a => a.FireVassal),
            ActionButtonHelper.Strategy(a => a.Ally),
            ActionButtonHelper.Strategy(a => a.Goodwill),
            ActionButtonHelper.Strategy(a => a.Invest),
            ActionButtonHelper.Strategy(a => a.BuildTown),
            ActionButtonHelper.Strategy(a => a.DepositCastleGold),
            ActionButtonHelper.Strategy(a => a.WithdrawCastleGold),
            ActionButtonHelper.Strategy(a => a.BecomeIndependent),
            ActionButtonHelper.Common(a => a.FinishTurn),
        };

        foreach (var button in buttons)
        {
            ActionButtons.Add(button.Element);
            button.SetEventHandlers(
                labelCostGold,
                labelActionDescription,
                () => currentCharacter,
                OnActionButtonClicked
            );
        }
    }

    public void Reinitialize()
    {
        foreach (var button in buttons)
        {
            ActionButtons.Add(button.Element);
        }
    }

    private async void OnActionButtonClicked(ActionButtonHelper button)
    {
        var chara = currentCharacter;
        var action = button.Action;

        var canPrepare = action.CanUIEnable(chara);
        if (action is CommonActionBase)
        {
            if (canPrepare)
            {
                var argsCommon = await action.Prepare(chara);
                await action.Do(argsCommon);
            }
            return;
        }

        if (!canPrepare)
        {
            return;
        }

        var args = await action.Prepare(chara);
        await action.Do(args);
        SetData(chara);
        Root.style.display = DisplayStyle.Flex;
    }

    public void Show(Character chara)
    {
        Core.MainUI.HideAllPanels();
        SetData(chara);
        Root.style.display = DisplayStyle.Flex;
    }

    public void SetData(Character chara)
    {
        currentCharacter = chara;
        Render();
    }

    public void Render()
    {
        CharacterSummary.SetData(currentCharacter);
        foreach (var button in buttons)
        {
            button.SetData(currentCharacter);
        }
        
        // 城情報の表示・非表示を制御
        if (currentCharacter?.Castle != null)
        {
            ShowCastleInfo(currentCharacter.Castle);
        }
        else
        {
            HideCastleInfo();
        }
    }

    private void ShowCastleInfo(Castle castle)
    {
        var castlePanel = Root.Q<VisualElement>("CastleInfoPanel");
        castlePanel.style.display = DisplayStyle.Flex;
        
        // 城のパラメータを表示
        Root.Q<Label>("labelDevLevel").text = ((int)castle.DevLevel).ToString();
        Root.Q<Label>("labelTotalInvestment").text = castle.TotalInvestment.ToString("F0");
        Root.Q<Label>("labelIncome").text = ((int)castle.GoldIncome).ToString();
        Root.Q<Label>("labelMaxIncome").text = ((int)castle.GoldIncomeMax).ToString();
        Root.Q<Label>("labelExpenditure").text = ((int)castle.GoldComsumption).ToString();
        Root.Q<Label>("labelGold").text = castle.Gold.ToString("F0");
        
        var balance = castle.GoldBalance;
        var balanceLabel = Root.Q<Label>("labelBalance");
        balanceLabel.text = balance >= 0 ? $"+{(int)balance}" : ((int)balance).ToString();
        balanceLabel.style.color = balance >= 0 ? Color.green : Color.red;
        
        // 城塞レベルと総兵力を表示
        Root.Q<Label>("labelCastleStrength").text = ((int)castle.Strength).ToString();
        Root.Q<Label>("labelTotalPower").text = castle.Power.ToString();
        
        // 収入バーの表示
        UpdateIncomeBar(castle.GoldIncome, castle.GoldIncomeMax);
        
        // キャラクターを在城中と出撃中に分けて表示
        var garrisoned = castle.Members.Where(m => !m.IsMoving).OrderBy(c => c.OrderIndex).ToList();
        var deployed = castle.Members.Where(m => m.IsMoving).OrderBy(c => c.OrderIndex).ToList();
        
        // 在城中キャラクター
        Root.Q<Label>("labelGarrisonedCount").text = $"({garrisoned.Count}人)";
        ShowCharacterIcons(garrisoned, "GarrisonedCharacterIcons");
        
        // 出撃中キャラクター
        Root.Q<Label>("labelDeployedCount").text = $"({deployed.Count}人)";
        ShowCharacterIcons(deployed, "DeployedCharacterIcons");
    }

    private void UpdateIncomeBar(float currentIncome, float maxIncome)
    {
        const float maxBarValue = 200f; // 最大値を200に設定
        
        var maxIncomeBar = Root.Q<VisualElement>("MaxIncomeBar");
        var currentIncomeBar = Root.Q<VisualElement>("CurrentIncomeBar");
        
        // 最大収入バー（薄い黄色）の幅を設定
        var maxIncomeRatio = Mathf.Clamp01(maxIncome / maxBarValue);
        maxIncomeBar.style.width = Length.Percent(maxIncomeRatio * 100f);
        
        // 現在収入バー（黄色）の幅を設定
        var currentIncomeRatio = Mathf.Clamp01(currentIncome / maxBarValue);
        currentIncomeBar.style.width = Length.Percent(currentIncomeRatio * 100f);
    }

    private void HideCastleInfo()
    {
        var castlePanel = Root.Q<VisualElement>("CastleInfoPanel");
        castlePanel.style.display = DisplayStyle.None;
    }

    private void ShowCharacterIcons(System.Collections.Generic.IEnumerable<Character> characters, string containerName)
    {
        var iconContainer = Root.Q<VisualElement>(containerName);
        iconContainer.Clear();
        
        foreach (var character in characters)
        {
            var icon = CreateCharacterIcon(character);
            iconContainer.Add(icon);
        }
    }

    private VisualElement CreateCharacterIcon(Character character)
    {
        // キャラクター顔画像（大きくして名前ラベルを削除）
        var faceImage = new VisualElement();
        faceImage.style.width = 70;
        faceImage.style.height = 70;
        faceImage.style.marginRight = 5;
        faceImage.style.marginBottom = 5;
        faceImage.style.backgroundImage = new StyleBackground(Static.GetFaceImage(character));
        faceImage.style.borderTopWidth = 2;
        faceImage.style.borderBottomWidth = 2;
        faceImage.style.borderLeftWidth = 2;
        faceImage.style.borderRightWidth = 2;
        faceImage.style.borderTopColor = Color.white;
        faceImage.style.borderBottomColor = Color.white;
        faceImage.style.borderLeftColor = Color.white;
        faceImage.style.borderRightColor = Color.white;
        
        // クリックイベント（キャラクター詳細表示用）
        faceImage.RegisterCallback<ClickEvent>(evt => OnCharacterIconClicked(character));
        
        // ホバーエフェクト
        faceImage.RegisterCallback<MouseEnterEvent>(evt => 
        {
            faceImage.style.borderTopColor = Color.yellow;
            faceImage.style.borderBottomColor = Color.yellow;
            faceImage.style.borderLeftColor = Color.yellow;
            faceImage.style.borderRightColor = Color.yellow;
        });
        
        faceImage.RegisterCallback<MouseLeaveEvent>(evt => 
        {
            faceImage.style.borderTopColor = Color.white;
            faceImage.style.borderBottomColor = Color.white;
            faceImage.style.borderLeftColor = Color.white;
            faceImage.style.borderRightColor = Color.white;
        });
        
        return faceImage;
    }

    private void OnCharacterIconClicked(Character character)
    {
        // キャラクター詳細を表示（今後の拡張用）
        Debug.Log($"キャラクターがクリックされました: {character.Name}");
    }
}