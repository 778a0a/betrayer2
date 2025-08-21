using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CastleInfoPanel
{
    private GameCore Core => GameCore.Instance;
    private Castle targetCastle;
    private Character characterSummaryTarget;
    private Character characterSummaryTargetDefault;

    private InfoTab currentTab;
    private Button CurrentTabButton => currentTab switch
    {
        InfoTab.Castle => TabButtonCastle,
        InfoTab.Country => TabButtonCountry,
        InfoTab.Diplomacy => TabButtonDiplomacy,
        _ => throw new NotImplementedException(),
    };
    private enum InfoTab
    {
        Castle,
        Country, 
        Diplomacy
    }

    public void Initialize()
    {
        TabButtonCastle.clicked += () => SwitchTab(InfoTab.Castle);
        TabButtonCountry.clicked += () => SwitchTab(InfoTab.Country);
        TabButtonDiplomacy.clicked += () => SwitchTab(InfoTab.Diplomacy);

        CastleInfoTab.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            CharacterSummary.SetData(characterSummaryTargetDefault);
        });
     
        SwitchTab(InfoTab.Castle);
    }

    private void SwitchTab(InfoTab tab)
    {
        currentTab = tab;

        // タブボタンの色を更新する。
        TabButtonCastle.RemoveFromClassList("active");
        TabButtonCountry.RemoveFromClassList("active");
        TabButtonDiplomacy.RemoveFromClassList("active");
        CurrentTabButton.AddToClassList("active");

        CastleInfoTab.style.display = Util.Display(currentTab == InfoTab.Castle);
        CountryInfoTab.style.display = Util.Display(currentTab == InfoTab.Country);
        DiplomacyInfoTab.style.display = Util.Display(currentTab == InfoTab.Diplomacy);
    }

    public void SetData(Castle castle, Character characterSummaryTargetDefault)
    {
        targetCastle = castle;
        characterSummaryTarget = characterSummaryTargetDefault;
        this.characterSummaryTargetDefault = characterSummaryTargetDefault;

        Render();
    }

    private void Render()
    {
        SetCastleData(targetCastle);
        SetCountryData(targetCastle.Country);
        SetDiplomacyData(targetCastle.Country);
    }

    /// <summary>
    /// 城情報を設定します。
    /// </summary>
    private void SetCastleData(Castle castle)
    {
        // 城名等
        labelCastleName.text = castle.Name;
        labelCastleRegion.text = castle.Region;
        if (castle.Boss != null)
        {
            CastleBossImage.style.backgroundImage = new(Static.GetFaceImage(castle.Boss));
        }
        ObjectiveContainer.style.display = Util.Display((castle.Boss?.IsPlayer ?? false) || castle.Country.Ruler.IsPlayer);
        labelObjective.text = castle.Objective.ToString();
        
        // パラメーター等
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
        
        // 収入バー
        SetIncomeBar(castle.GoldIncome, castle.GoldIncomeMax);
        
        // 在城中キャラ一覧
        var inCastle = castle.Members.Where(m => !m.IsMoving).OrderBy(c => c.OrderIndex).ToList();
        labelInCastleMemberCount.text = $"({inCastle.Count}名)";
        labelInCastlePower.text = $"{inCastle.Sum(c => c.Power):0}";
        ShowCharacterIcons(inCastle, InCastleCharacterIcons);
        
        // 出撃中キャラ一覧
        var deployed = castle.Members.Where(m => m.IsMoving).OrderBy(c => c.OrderIndex).ToList();
        labelDeployedCount.text = $"({deployed.Count}名)";
        labelDeployedPower.text = $"{deployed.Sum(c => c.Power):0}";
        ShowCharacterIcons(deployed, DeployedCharacterIcons);

        // 人物サマリー
        CharacterSummary.SetData(characterSummaryTarget);
    }

    /// <summary>
    /// 収入バーを設定します。
    /// </summary>
    private void SetIncomeBar(float currentIncome, float maxIncome)
    {
        const float maxBarValue = 200f;
        
        // 最大収入バー（薄い黄色）の幅を設定する。
        var maxIncomeRatio = Mathf.Clamp01(maxIncome / maxBarValue);
        MaxIncomeBar.style.width = Length.Percent(maxIncomeRatio * 100f);
        
        // 現在収入バー（黄色）の幅を設定する。
        var currentIncomeRatio = Mathf.Clamp01(currentIncome / maxBarValue);
        CurrentIncomeBar.style.width = Length.Percent(currentIncomeRatio * 100f);
    }

    /// <summary>
    /// 国情報タブを設定します。
    /// </summary>
    private void SetCountryData(Country country)
    {
    }

    /// <summary>
    /// 外交関係タブを設定します。
    /// </summary>
    private void SetDiplomacyData(Country country)
    {
        var container = DiplomacyRelations;
        container.Clear();
        
        // 他の国との関係を表示（TileInfoEditorWindow:309-341を参考）
        var world = Core.World;
        var otherCountries = world.Countries
            .Where(c => c != country)
            .Where(o => o.GetRelation(country) != 50)
            .OrderBy(o => o.GetRelation(country));
            
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

    private void ShowCharacterIcons(IEnumerable<Character> characters, VisualElement iconContainer)
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
        var faceImage = new VisualElement();
        faceImage.AddToClassList("SmallCharacterIcon");
        faceImage.style.backgroundImage = new(Static.GetFaceImage(character));
        faceImage.RegisterCallback<MouseEnterEvent>(evt =>
        {
            CharacterSummary.SetData(character);
        });
        return faceImage;
    }
}