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

    private bool showCastleList;
    private bool showMemberList;
    private bool showDiplomacyList;
    private bool showObjectiveSelection;

    public void Initialize()
    {
        MemberListViewTable.Initialize();
        CastleListViewTable.Initialize();
        ObjectiveSelectionTargetTable.Initialize();

        // 城一覧表示ボタン押下時
        buttonShowCastleList.clicked += () =>
        {
            showCastleList = true;
            showMemberList = false;
            showDiplomacyList = false;
            Render();
        };

        // 人物一覧表示ボタン押下時
        buttonShowMemberList.clicked += () =>
        {
            showCastleList = false;
            showMemberList = true;
            showDiplomacyList = false;
            Render();
        };

        // 外交表示ボタン押下時
        buttonShowDiplomacy.clicked += () =>
        {
            showCastleList = false;
            showMemberList = false;
            showDiplomacyList = true;
            Render();
        };

        // キャラリストの行マウスオーバー時
        MemberListViewTable.RowMouseMove += (sender, chara) =>
        {
            if (chara == characterSummaryTarget) return;
            characterSummaryTarget = chara;
            CharacterSummary.SetData(chara);
        };

        // キャラリストの行クリック時
        MemberListViewTable.RowMouseDown += (sender, chara) =>
        {
            // 所属城へスクロールする。
            var tile = Core.World.Map.GetTile(chara.Castle);
            Core.World.Map.ScrollTo(tile);
        };

        // 城一覧のクリック時
        CastleListViewTable.RowMouseDown += (sender, castle) =>
        {
            // 城タイルへスクロールする。
            var tile = Core.World.Map.GetTile(castle.Position);
            Core.World.Map.ScrollTo(tile);
        };

        // 方針コンボボックス選択時
        comboObjective.RegisterCallback<ChangeEvent<string>>(OnObjectiveComboBoxSelectionChanged);

        // 目標選択画面のキャンセル時
        buttonCancelObjectiveSelection.clicked += () =>
        {
            HideObjectiveSelectView();
        };

        // 目標選択の行マウスオーバー時
        ObjectiveSelectionTargetTable.RowMouseEnter += (sender, index) =>
        {
            switch (comboObjective.index)
            {
                case 0: // 地方攻略
                    var region = (string)ObjectiveSelectionTargetTable.Items[index];
                    var castles = Core.World.Castles.Where(c => c.Region == region).ToList();
                    foreach (var castle in castles)
                    {
                        var tile = Core.World.Map.GetTile(castle.Position);
                        tile.UI.SetFocusHighlight(true);
                    }
                    break;
                case 1: // 勢力攻略
                    var country = (Country)ObjectiveSelectionTargetTable.Items[index];
                    foreach (var castle in country.Castles)
                    {
                        var tile = Core.World.Map.GetTile(castle.Position);
                        tile.UI.SetFocusHighlight(true);
                    }
                    break;
            }
        };
        // 目標選択の行マウスリーブ時
        ObjectiveSelectionTargetTable.RowMouseLeave += (sender, index) =>
        {
            switch (comboObjective.index)
            {
                case 0: // 地方攻略
                    var region = (string)ObjectiveSelectionTargetTable.Items[index];
                    var castles = Core.World.Castles.Where(c => c.Region == region).ToList();
                    foreach (var castle in castles)
                    {
                        var tile = Core.World.Map.GetTile(castle.Position);
                        tile.UI.SetFocusHighlight(false);
                    }
                    break;
                case 1: // 勢力攻略
                    var country = (Country)ObjectiveSelectionTargetTable.Items[index];
                    foreach (var castle in country.Castles)
                    {
                        var tile = Core.World.Map.GetTile(castle.Position);
                        tile.UI.SetFocusHighlight(false);
                    }
                    break;
            }
        };

        // 目標選択の選択確定時
        ObjectiveSelectionTargetTable.ItemSelected += (sender, selectedItem) =>
        {
            OnObjectiveSelected(selectedItem);
        };
    }

    public void SetData(Country country)
    {
        targetCountry = country;
        characterSummaryTarget = null;
        Render();
    }

    public void Render()
    {
        var country = targetCountry;
        if (country == null)
        {
            Root.style.display = DisplayStyle.None;
            return;
        }
        Root.style.display = DisplayStyle.Flex;

        // 統治者
        iconCountry.style.backgroundImage = new(Static.GetCountrySprite(country.ColorIndex));
        labelCountryRulerName.text = country.Ruler.Name;
        CountryRulerImage.style.backgroundImage = new(Static.GetFaceImage(country.Ruler));

        // 方針
        NormalView.style.display = Util.Display(!showObjectiveSelection);
        ObjectiveSelectionView.style.display = Util.Display(showObjectiveSelection);
        // 国方針はプレイヤーが統治者の国のみ設定可能
        ObjectiveContainer.style.display = Util.Display(Core.World.Player?.Country == country);
        var canOrder = Core.World.Player?.Country == country;
        labelObjective.style.display = Util.Display(!canOrder);
        comboObjective.style.display = Util.Display(canOrder);
        var objectiveText = targetCountry.Objective switch
        {
            CountryObjective.RegionConquest co => $"{co.TargetRegionName}攻略",
            CountryObjective.CountryAttack co => $"{co.TargetRulerName}打倒",
            CountryObjective.StatusQuo => "現状維持",
            _ => "現状維持",
        };
        labelObjective.text = objectiveText;
        // 余計な変更イベントが起きないように、目標選択中はコンボボックスの値を変更しない。
        if (!showObjectiveSelection)
        {
            comboObjective.value = objectiveText;
        }

        // 外交関係表示
        SetRelation(country);

        // 総資金・総収支
        var totalGold = country.Castles.Sum(c => c.Gold);
        var totalBalance = country.Castles.Sum(c => c.GoldBalance);
        labelTotalGold.text = totalGold.ToString("F0");
        labelTotalBalance.text = totalBalance > 0 ? $"+{(int)totalBalance}" : $"{(int)totalBalance}";
        labelTotalBalance.style.color = totalBalance >= 0 ? Color.green : Color.red;
        // 総収入・総支出
        var totalIncome = country.Castles.Sum(c => c.GoldIncome);
        var totalExpenditure = country.Castles.Sum(c => c.GoldComsumption);
        labelTotalIncome.text = $"{totalIncome:0}";
        labelTotalExpenditure.text = $"{totalExpenditure:0}";
        // 総兵力・総将数
        var totalPower = country.Castles.Sum(c => c.SoldierCount);
        var totalPowerMax = country.Castles.Sum(c => c.SoldierCountMax);
        var totalGeneralCount = country.Castles.Sum(c => c.Members.Count);
        labelTotalPower.text = $"{totalPower:0}";
        labelTotalPowerMax.text = $"{totalPowerMax:0}";
        labelTotalGeneralCount.text = $"{totalGeneralCount}";
        // 城数
        labelCastleCount.text = $"{country.Castles.Count}";

        // 城一覧
        CastleListView.style.display = Util.Display(showCastleList);
        if (showCastleList)
        {
            var castles = targetCountry.Castles.OrderBy(c => c.Boss?.OrderIndex ?? -1).ToList();
            CastleListViewTable.SetData(castles, true);
        }

        // 人物一覧
        MemberListView.style.display = Util.Display(showMemberList);
        if (showMemberList)
        {
            var all = targetCountry.Castles.SelectMany(c => c.Members)
                .OrderBy(c => c.OrderIndex)
                .ToList();
            MemberListViewTable.SetData(all, null);
            characterSummaryTarget ??= all.FirstOrDefault();
            CharacterSummary.SetData(characterSummaryTarget);
        }
        
        // 外交
        DiplomacyView.style.display = Util.Display(showDiplomacyList);
        if (showDiplomacyList)
        {
            SetDiplomacyData(targetCountry);
        }

        // ボタンのハイライト状態を更新する。
        buttonShowCastleList.EnableInClassList("active", showCastleList);
        buttonShowMemberList.EnableInClassList("active", showMemberList);
        buttonShowDiplomacy.EnableInClassList("active", showDiplomacyList);
    }

    /// <summary>
    /// 方針コンボボックスの値が変更されたときに呼ばれます。
    /// </summary>
    private void OnObjectiveComboBoxSelectionChanged(ChangeEvent<string> evt)
    {
        //Debug.Log($"Objective changed: {evt.newValue}");
        if (targetCountry == null) return;
        switch (comboObjective.index)
        {
            // 地方攻略
            case 0:
                var regions = targetCountry.Castles
                    .Concat(targetCountry.Castles.SelectMany(c => c.Neighbors))
                    .Select(c => c.Region)
                    .Distinct()
                    .Where(r => Core.World.Castles.Where(c => c.Region == r).Any(c => c.Country != targetCountry))
                    .ToList();
                var targetCastles = Core.World.Castles.Where(c => regions.Contains(c.Region)).ToList();
                Core.World.Map.SetEnableHighlight(targetCastles);
                Core.World.Map.SetCustomEventHandler(tile =>
                {
                    var isInTargets = regions.Contains(tile.Castle?.Region);
                    if (isInTargets) OnObjectiveSelected(tile.Castle.Region);
                });
                ShowObjectiveSelectView("攻略目標を選択してください", regions);
                break;
            // 勢力攻略
            case 1:
                var enemies = targetCountry.Neighbors
                    .Where(n => targetCountry.IsAttackable(n))
                    .ToList();
                targetCastles = enemies.SelectMany(c => c.Castles).ToList();
                Core.World.Map.SetEnableHighlight(targetCastles);
                Core.World.Map.SetCustomEventHandler(tile =>
                {
                    var isInTargets = enemies.Contains(tile.Castle?.Country);
                    if (isInTargets) OnObjectiveSelected(tile.Castle.Country);
                });
                ShowObjectiveSelectView("打倒目標を選択してください", enemies, c => c.Ruler.Name);
                break;
            // 現状維持
            case 2:
                targetCountry.Objective = new CountryObjective.StatusQuo();
                HideObjectiveSelectView();
                break;

        }
    }

    /// <summary>
    /// 目標選択画面を表示します。
    /// </summary>
    private void ShowObjectiveSelectView<T>(string title, IReadOnlyList<T> options, Func<T, string> toString = null)
    {
        showObjectiveSelection = true;
        labelObjectiveSelectionTitle.text = title;
        ObjectiveSelectionTargetTable.SetData(options, "対象", toString);
        Render();
    }

    /// <summary>
    /// 目標選択画面を非表示にして元の表示に戻します。
    /// </summary>
    private void HideObjectiveSelectView()
    {
        showObjectiveSelection = false;
        Core.World.Map.ClearAllEnableHighlight();
        Core.World.Map.ClearCustomEventHandler();
        Render();
    }

    /// <summary>
    /// 目標選択で目標が選択されたときに呼ばれます。
    /// </summary>
    private void OnObjectiveSelected(object selectedItem)
    {
        if (selectedItem == null) return;

        var index = comboObjective.index;
        Debug.Log($"Objective selected: {selectedItem} (dropdown index: {index})");
        switch (index)
        {
            case 0: // 地方攻略
                var region = (string)selectedItem;
                targetCountry.Objective = new CountryObjective.RegionConquest { TargetRegionName = region };
                break;
            case 1: // 勢力打倒
                var country = (Country)selectedItem;
                targetCountry.Objective = new CountryObjective.CountryAttack { TargetRulerName = country.Ruler.Name };
                break;
        }

        foreach (var castle in Core.World.Castles)
        {
            var tile = Core.World.Map.GetTile(castle.Position);
            tile.UI.SetFocusHighlight(false);
        }

        HideObjectiveSelectView();
    }

    /// <summary>
    /// 友好度を設定します。
    /// </summary>
    private void SetRelation(Country country)
    {
        // 自身の国なら表示しない。
        var playerCountry = Core.World.Player?.Country;
        if (playerCountry == country || playerCountry == null)
        {
            DiplomaticRelationsContainer.style.display = DisplayStyle.None;
            return;
        }
        DiplomaticRelationsContainer.style.display = DisplayStyle.Flex;

        labelDiplomaticRelations.text = Core.World.Countries.GetRelationText(playerCountry, country);
        DiplomaticRelationsContainer.style.color = Core.World.Countries.GetRelationColor(playerCountry, country);
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
        relationLabel.style.color = Util.RelationToColor(relation);
        item.Add(faceImage);
        item.Add(nameLabel);
        item.Add(statusLabel);
        item.Add(relationLabel);
        
        return item;
    }
}