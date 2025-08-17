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
    
    private enum InfoTab
    {
        Castle,
        Country, 
        Diplomacy
    }
    
    private InfoTab currentTab = InfoTab.Castle;

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
            //ActionButtonHelper.Strategy(a => a.BuildTown),
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
        
        // タブボタンのイベントハンドラーを設定
        Root.Q<Button>("TabButtonCastle").clicked += () => SwitchTab(InfoTab.Castle);
        Root.Q<Button>("TabButtonCountry").clicked += () => SwitchTab(InfoTab.Country);
        Root.Q<Button>("TabButtonDiplomacy").clicked += () => SwitchTab(InfoTab.Diplomacy);
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
        
        // 情報タブパネルの表示・非表示を制御
        if (currentCharacter?.Country != null || currentCharacter?.Castle != null)
        {
            ShowInfoTabPanel();
            UpdateTabContent();
        }
        else
        {
            HideInfoTabPanel();
        }
    }

    private void ShowInfoTabPanel()
    {
        Root.Q<VisualElement>("InfoTabPanel").style.display = DisplayStyle.Flex;
    }

    private void HideInfoTabPanel()
    {
        Root.Q<VisualElement>("InfoTabPanel").style.display = DisplayStyle.None;
    }

    private void SwitchTab(InfoTab tab)
    {
        currentTab = tab;
        UpdateTabButtons();
        UpdateTabContent();
    }

    private void UpdateTabButtons()
    {
        // タブボタンの色を更新
        Root.Q<Button>("TabButtonCastle").style.backgroundColor = currentTab == InfoTab.Castle ? Color.gray : new Color(0.2f, 0.27f, 0.33f);
        Root.Q<Button>("TabButtonCountry").style.backgroundColor = currentTab == InfoTab.Country ? Color.gray : new Color(0.2f, 0.27f, 0.33f);
        Root.Q<Button>("TabButtonDiplomacy").style.backgroundColor = currentTab == InfoTab.Diplomacy ? Color.gray : new Color(0.2f, 0.27f, 0.33f);
    }

    private void UpdateTabContent()
    {
        // タブの表示・非表示を制御
        Root.Q<VisualElement>("CastleInfoTab").style.display = currentTab == InfoTab.Castle ? DisplayStyle.Flex : DisplayStyle.None;
        Root.Q<VisualElement>("CountryInfoTab").style.display = currentTab == InfoTab.Country ? DisplayStyle.Flex : DisplayStyle.None;
        Root.Q<VisualElement>("DiplomacyInfoTab").style.display = currentTab == InfoTab.Diplomacy ? DisplayStyle.Flex : DisplayStyle.None;

        // タブボタンの有効・無効を制御
        Root.Q<Button>("TabButtonCastle").SetEnabled(currentCharacter?.Castle != null);
        Root.Q<Button>("TabButtonCountry").SetEnabled(currentCharacter?.Country != null);
        Root.Q<Button>("TabButtonDiplomacy").SetEnabled(currentCharacter?.Country != null);
        
        // 選択可能なタブがない場合は最初の利用可能なタブに切り替え
        if (currentTab == InfoTab.Castle && currentCharacter?.Castle == null)
        {
            if (currentCharacter?.Country != null)
            {
                SwitchTab(InfoTab.Country);
                return;
            }
        }
        
        // タブに応じた内容を更新
        switch (currentTab)
        {
            case InfoTab.Castle:
                if (currentCharacter?.Castle != null)
                    UpdateCastleTab(currentCharacter.Castle);
                break;
            case InfoTab.Country:
                if (currentCharacter?.Country != null)
                    UpdateCountryTab(currentCharacter.Country);
                break;
            case InfoTab.Diplomacy:
                if (currentCharacter?.Country != null)
                    UpdateDiplomacyTab(currentCharacter.Country);
                break;
        }
    }

    private void UpdateCastleTab(Castle castle)
    {
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

    private void UpdateCountryTab(Country country)
    {
        // 国の基本情報を表示
        Root.Q<Label>("labelRulerName").text = country.Ruler.Name;
        Root.Q<Label>("labelRulerPersonality").text = country.Ruler.Personality.ToString();
        Root.Q<Label>("labelCountryBalance").text = ((int)country.GoldBalance).ToString();
        Root.Q<Label>("labelCountrySurplus").text = country.GoldSurplus.ToString();
        
        // 城数と将数を計算して表示
        var castleCount = country.Castles.Count();
        var generalCount = country.Vassals.Count();
        Root.Q<Label>("labelCastleCount").text = castleCount.ToString();
        Root.Q<Label>("labelGeneralCount").text = generalCount.ToString();
    }

    private void UpdateDiplomacyTab(Country country)
    {
        var container = Root.Q<VisualElement>("DiplomacyRelations");
        container.Clear();
        
        // 他の国との関係を表示（TileInfoEditorWindow:309-341を参考）
        var world = Core.World;
        var otherCountries = world.Countries
            .Where(c => c != country)
            .OrderBy(o => o.GetRelation(country) == 50 ? 999 : o.GetRelation(country));
            
        foreach (var other in otherCountries)
        {
            var relation = country.GetRelation(other);
            var relationItem = CreateDiplomacyRelationItem(other, relation, country);
            container.Add(relationItem);
        }
    }

    private VisualElement CreateDiplomacyRelationItem(Country other, float relation, Country myCountry)
    {
        var item = new VisualElement();
        item.style.flexDirection = FlexDirection.Row;
        item.style.alignItems = Align.Center;
        item.style.marginBottom = 5;
        item.style.paddingTop = 5;
        item.style.paddingBottom = 5;
        item.style.borderBottomWidth = 1;
        item.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
        
        // 統治者の顔画像
        var faceImage = new VisualElement();
        faceImage.style.width = 40;
        faceImage.style.height = 40;
        faceImage.style.marginRight = 10;
        faceImage.style.backgroundImage = new StyleBackground(Static.GetFaceImage(other.Ruler));
        //faceImage.style.borderWidth = 1;
        //faceImage.style.borderColor = Color.white;
        
        // 国名と統治者名
        var nameLabel = new Label($"{other.Ruler.Name}");
        nameLabel.style.fontSize = 20;
        nameLabel.style.color = Color.white;
        nameLabel.style.flexGrow = 1;
        nameLabel.style.marginRight = 10;
        
        // 関係性ラベル
        var statusLabel = new Label();
        statusLabel.style.fontSize = 18;
        statusLabel.style.marginRight = 10;
        
        if (myCountry.IsAlly(other))
        {
            statusLabel.text = "同盟";
            statusLabel.style.color = Color.green;
        }
        else if (myCountry.IsEnemy(other))
        {
            statusLabel.text = "敵対";
            statusLabel.style.color = Color.red;
        }
        else if (myCountry.Neighbors.Contains(other))
        {
            statusLabel.text = "隣接";
            statusLabel.style.color = Color.yellow;
        }
        else
        {
            statusLabel.text = "";
        }
        
        // 関係度
        var relationLabel = new Label(relation.ToString());
        relationLabel.style.fontSize = 20;
        relationLabel.style.color = relation > 50 ? Color.Lerp(Color.white, Color.green, (relation - 50) / 50f) :
                                     relation < 50 ? Color.Lerp(Color.red, Color.white, relation / 50f) :
                                     Color.gray;
        
        item.Add(faceImage);
        item.Add(nameLabel);
        item.Add(statusLabel);
        item.Add(relationLabel);
        
        return item;
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