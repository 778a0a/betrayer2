using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CastleInfoPanel
{
    private GameCore Core => GameCore.Instance;
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
        TabButtonCastle.clicked += () => SwitchTab(InfoTab.Castle);
        TabButtonCountry.clicked += () => SwitchTab(InfoTab.Country);
        TabButtonDiplomacy.clicked += () => SwitchTab(InfoTab.Diplomacy);
    }

    public void SetData(Character character)
    {
        currentCharacter = character;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
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
        InfoTabPanel.style.display = DisplayStyle.Flex;
    }

    private void HideInfoTabPanel()
    {
        InfoTabPanel.style.display = DisplayStyle.None;
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
        TabButtonCastle.style.backgroundColor = currentTab == InfoTab.Castle ? Color.gray : new Color(0.2f, 0.27f, 0.33f);
        TabButtonCountry.style.backgroundColor = currentTab == InfoTab.Country ? Color.gray : new Color(0.2f, 0.27f, 0.33f);
        TabButtonDiplomacy.style.backgroundColor = currentTab == InfoTab.Diplomacy ? Color.gray : new Color(0.2f, 0.27f, 0.33f);
    }

    private void UpdateTabContent()
    {
        // タブの表示・非表示を制御
        CastleInfoTab.style.display = currentTab == InfoTab.Castle ? DisplayStyle.Flex : DisplayStyle.None;
        CountryInfoTab.style.display = currentTab == InfoTab.Country ? DisplayStyle.Flex : DisplayStyle.None;
        DiplomacyInfoTab.style.display = currentTab == InfoTab.Diplomacy ? DisplayStyle.Flex : DisplayStyle.None;

        // タブボタンの有効・無効を制御
        TabButtonCastle.SetEnabled(currentCharacter?.Castle != null);
        TabButtonCountry.SetEnabled(currentCharacter?.Country != null);
        TabButtonDiplomacy.SetEnabled(currentCharacter?.Country != null);
        
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
        // 城名・地方名・城主情報を表示
        labelCastleName.text = castle.Name;
        labelCastleRegion.text = castle.Region;
        
        if (castle.Boss != null)
        {
            CastleBossImage.style.backgroundImage = new StyleBackground(Static.GetFaceImage(castle.Boss));
        }
        labelObjective.text = castle.Objective.ToString() ?? "--";

        // 城のパラメータを表示
        labelGold.text = castle.Gold.ToString("F0");
        var balance = castle.GoldBalance;
        labelBalance.text = balance >= 0 ? $"+{(int)balance}" : $"-{(int)balance}";
        labelBalance.style.color = balance >= 0 ? Color.green : Color.red;
        labelIncome.text = $"{castle.GoldIncome:0}";
        labelMaxIncome.text = $"{castle.GoldIncomeMax:0}";
        labelExpenditure.text = $"{castle.GoldComsumption:0}";
        labelDevLevel.text = $"{castle.DevLevel}";
        labelTotalInvestment.text = $"{castle.TotalInvestment:0}";
        labelCastleStrength.text = $"{castle.Strength:0}";
        labelTotalPower.text = $"{castle.Power:0}";
        labelMemberCount.text = $"{castle.Members.Count}";
        
        // 収入バーの表示
        UpdateIncomeBar(castle.GoldIncome, castle.GoldIncomeMax);
        
        // キャラクターを在城中と出撃中に分けて表示
        var inCastle = castle.Members.Where(m => !m.IsMoving).OrderBy(c => c.OrderIndex).ToList();
        var deployed = castle.Members.Where(m => m.IsMoving).OrderBy(c => c.OrderIndex).ToList();
        
        // 在城中キャラクター
        labelInCastleMemberCount.text = $"({inCastle.Count}名)";
        labelInCastlePower.text = $"{inCastle.Sum(c => c.Power):0}";
        ShowCharacterIcons(inCastle, GarrisonedCharacterIcons);
        
        // 出撃中キャラクター
        labelDeployedCount.text = $"({deployed.Count}名)";
        labelDeployedPower.text = $"{deployed.Sum(c => c.Power):0}";
        ShowCharacterIcons(deployed, DeployedCharacterIcons);
    }

    private void UpdateIncomeBar(float currentIncome, float maxIncome)
    {
        const float maxBarValue = 200f; // 最大値を200に設定
        
        // 最大収入バー（薄い黄色）の幅を設定
        var maxIncomeRatio = Mathf.Clamp01(maxIncome / maxBarValue);
        MaxIncomeBar.style.width = Length.Percent(maxIncomeRatio * 100f);
        
        // 現在収入バー（黄色）の幅を設定
        var currentIncomeRatio = Mathf.Clamp01(currentIncome / maxBarValue);
        CurrentIncomeBar.style.width = Length.Percent(currentIncomeRatio * 100f);
    }

    private void UpdateCountryTab(Country country)
    {
        // 国の基本情報を表示
        labelRulerName.text = country.Ruler.Name;
        labelRulerPersonality.text = country.Ruler.Personality.ToString();
        labelCountryBalance.text = ((int)country.GoldBalance).ToString();
        labelCountrySurplus.text = country.GoldSurplus.ToString();
        
        // 城数と将数を計算して表示
        var castleCount = country.Castles.Count();
        var generalCount = country.Vassals.Count();
        labelCastleCount.text = castleCount.ToString();
        labelGeneralCount.text = generalCount.ToString();
    }

    private void UpdateDiplomacyTab(Country country)
    {
        var container = DiplomacyRelations;
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

    private void ShowCharacterIcons(System.Collections.Generic.IEnumerable<Character> characters, VisualElement iconContainer)
    {
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