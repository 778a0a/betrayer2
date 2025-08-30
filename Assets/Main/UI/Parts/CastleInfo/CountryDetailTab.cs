using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CountryDetailTab
{
    private GameCore Core => GameCore.Instance;
    private Country targetCountry;
    private Character characterSummaryTarget;
    private Character characterSummaryTargetDefault;

    private bool isCharacterListViewVisible;
    private bool isObjectiveSelectViewVisible;

    public void Initialize()
    {
        // CharacterTable初期化
        CharacterTable.Initialize();
        // SimpleTable初期化
        CountryObjectiveSimpleTable.Initialize();

        Root.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            CharacterSummary.SetData(characterSummaryTargetDefault);
        });

        buttonCountryCharacterList.clicked += () =>
        {
            if (isCharacterListViewVisible)
            {
                HideCharacterListView();
                buttonCountryCharacterList.text = "人物一覧";
            }
            else
            {
                ShowCharacterListView();
                buttonCountryCharacterList.text = "戻る";
            }
        };

        buttonBackFromCountryObjectiveSelect.clicked += () =>
        {
            HideCountryObjectiveSelectView();
        };

        // SimpleTableの選択イベント
        CountryObjectiveSimpleTable.ItemSelected += (sender, selectedItem) =>
        {
            OnCountryObjectiveSelected(selectedItem);
        };

        CharacterTable.RowMouseMove += (sender, chara) =>
        {
            if (chara == characterSummaryTarget) return;
            characterSummaryTarget = chara;
            CharacterSummary.SetData(chara);
        };

        comboCountryObjective.RegisterCallback<ChangeEvent<string>>(evt =>
        {
            Debug.Log($"Country Objective changed: {evt.newValue}");
            if (targetCountry == null) return;
            var selected = comboCountryObjective.index;
            
            switch (selected)
            {
                case 0: // 地方統一
                    var regions = targetCountry.Castles
                        .Concat(targetCountry.Castles.SelectMany(c => c.Neighbors))
                        .Select(c => c.Region)
                        .Distinct()
                        .ToList();
                    ShowCountryObjectiveSelectView("目標を選択してください", regions);
                    break;
                case 1: // 勢力打倒
                    var enemies = targetCountry.Neighbors
                        .Where(n => targetCountry.IsAttackable(n))
                        .Select(n => n.Ruler.Name)
                        .ToList();
                    ShowCountryObjectiveSelectView("目標を選択してください", enemies);
                    break;
                case 2: // 現状維持
                    targetCountry.Objective = new CountryObjective.StatusQuo();
                    HideCountryObjectiveSelectView();
                    break;
            }
        });
    }

    public void SetData(Country country, Character characterSummaryTargetDefault)
    {
        targetCountry = country;
        characterSummaryTarget = characterSummaryTargetDefault;
        this.characterSummaryTargetDefault = characterSummaryTargetDefault;
        
        Render();
    }

    public void Render()
    {
        SetCountryData(targetCountry);
        
        // CharacterSummary更新
        CharacterSummary.SetData(characterSummaryTarget);
    }

    /// <summary>
    /// 国情報を設定します。
    /// </summary>
    private void SetCountryData(Country country)
    {
        // 統治者名
        labelCountryRulerName.text = country.Ruler.Name;
        // 統治者の顔画像
        CountryRulerImage.style.backgroundImage = new(Static.GetFaceImage(country.Ruler));

        // 国目標表示制御（プレイヤー国のみ設定可能）
        CountryObjectiveContainer.style.display = Util.Display(Core.World.Player?.Country == country);
        var canOrder = Core.World.Player?.Country == country;
        labelCountryObjective.style.display = Util.Display(!canOrder);
        comboCountryObjective.style.display = Util.Display(canOrder);
        // 国目標の値設定
        SetObjectiveComboValue();

        // 総資金・総収支
        var totalGold = country.Castles.Sum(c => c.Gold);
        var totalBalance = country.Castles.Sum(c => c.GoldBalance);
        labelTotalGold.text = totalGold.ToString("F0");
        labelTotalBalance.text = totalBalance >= 0 ? $"+{(int)totalBalance}" : $"{(int)totalBalance}";
        labelTotalBalance.style.color = totalBalance >= 0 ? Color.green : Color.red;

        // 総収入・総支出
        var totalIncome = country.Castles.Sum(c => c.GoldIncome);
        var totalExpenditure = country.Castles.Sum(c => c.GoldComsumption);
        labelTotalIncome.text = $"{totalIncome:0}";
        labelTotalExpenditure.text = $"{totalExpenditure:0}";

        // 総兵力・総将数
        var totalPower = country.Castles.Sum(c => c.SoldierCount);
        var totalGeneralCount = country.Castles.Sum(c => c.Members.Count);
        labelTotalPower.text = $"{totalPower:0}";
        labelTotalGeneralCount.text = $"{totalGeneralCount}";

        // 城数
        labelCastleCount.text = $"{country.Castles.Count}";
    }
    
    private void SetObjectiveComboValue()
    {
        var objectiveText = targetCountry.Objective switch
        {
            CountryObjective.RegionConquest co => $"{co.TargetRegionName}統一",
            CountryObjective.CountryAttack co => $"{co.TargetRulerName}打倒", 
            CountryObjective.StatusQuo => "現状維持",
            _ => "現状維持",
        };
        labelCountryObjective.text = objectiveText;
        comboCountryObjective.value = objectiveText;
    }

    /// <summary>
    /// キャラクター一覧表示を表示します。
    /// </summary>
    private void ShowCharacterListView()
    {
        isCharacterListViewVisible = true;
        
        CountryInfoNormalView.style.display = DisplayStyle.None;
        CountryInfoCharacterListView.style.display = DisplayStyle.Flex;
        
        var allMembers = targetCountry.Castles.SelectMany(c => c.Members).ToList();
        CharacterTable.SetData(allMembers, null);
        
        // 最初のキャラクターをCharacterSummaryに表示する。
        if (allMembers.Count > 0)
        {
            CharacterSummary.SetData(allMembers.First());
        }
    }

    /// <summary>
    /// キャラクター一覧表示を非表示にして元の表示に戻します。
    /// </summary>
    private void HideCharacterListView()
    {
        isCharacterListViewVisible = false;

        CountryInfoCharacterListView.style.display = DisplayStyle.None;
        CountryInfoNormalView.style.display = DisplayStyle.Flex;
        
        CharacterSummary.SetData(characterSummaryTargetDefault);
    }

    /// <summary>
    /// 国目標選択画面を表示します。
    /// </summary>
    private void ShowCountryObjectiveSelectView(string title, List<string> options)
    {
        isObjectiveSelectViewVisible = true;
        
        CountryInfoNormalView.style.display = DisplayStyle.None;
        CountryInfoObjectiveSelectView.style.display = DisplayStyle.Flex;
        
        labelCountryObjectiveSelectTitle.text = title;
        CountryObjectiveSimpleTable.SetData(options, "対象");
    }

    /// <summary>
    /// 国目標選択画面を非表示にします。
    /// </summary>
    private void HideCountryObjectiveSelectView()
    {
        isObjectiveSelectViewVisible = false;

        CountryInfoObjectiveSelectView.style.display = DisplayStyle.None;
        CountryInfoNormalView.style.display = DisplayStyle.Flex;
        
        // ドロップダウンを元の値に戻す
        SetObjectiveComboValue();
    }

    /// <summary>
    /// 国目標が選択された時の処理。
    /// </summary>
    private void OnCountryObjectiveSelected(object selectedItem)
    {
        if (selectedItem == null) return;

        var currentDropdownIndex = comboCountryObjective.index;
        Debug.Log($"Country Objective selected: {selectedItem} (dropdown index: {currentDropdownIndex})");

        switch (currentDropdownIndex)
        {
            case 0: // 地方統一
                targetCountry.Objective = new CountryObjective.RegionConquest { TargetRegionName = (string)selectedItem };
                break;
            case 1: // 勢力打倒
                targetCountry.Objective = new CountryObjective.CountryAttack { TargetRulerName = (string)selectedItem };
                break;
        }
        
        HideCountryObjectiveSelectView();
    }
}