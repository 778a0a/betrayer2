using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CastleDetailTab
{
    private GameCore Core => GameCore.Instance;
    private Castle targetCastle;
    private Character characterSummaryTarget;
    private Character characterSummaryTargetDefault;

    private bool showMemberListIconView = true;
    private bool showObjectiveSelection = false;

    public void Initialize()
    {
        MemberListViewTable.Initialize();
        ObjectiveSelectionTargetTable.Initialize();

        //Root.RegisterCallback<MouseLeaveEvent>(evt =>
        //{
        //    CharacterSummary.SetData(characterSummaryTargetDefault);
        //});

        // 将一覧のアイコン・リスト表示切替時
        buttonToggleMemberListAndIcon.clicked += () =>
        {
            showMemberListIconView = !showMemberListIconView;
            Render();
        };

        // キャラリストの行マウスオーバー時
        MemberListViewTable.RowMouseMove += (sender, chara) =>
        {
            ShowCharacterSummary(chara);
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
                case 0: // 拠点攻略
                case 4: // 輸送
                    var castle = (Castle)ObjectiveSelectionTargetTable.Items[index];
                    var tile = Core.World.Map.GetTile(castle.Position);
                    tile.UI.SetFocusHighlight(true);
                    break;
            }
        };
        // 目標選択の行マウスリーブ時
        ObjectiveSelectionTargetTable.RowMouseLeave += (sender, index) =>
        {
            switch (comboObjective.index)
            {
                case 0: // 拠点攻略
                case 4: // 輸送
                    var castle = (Castle)ObjectiveSelectionTargetTable.Items[index];
                    var tile = Core.World.Map.GetTile(castle.Position);
                    tile.UI.SetFocusHighlight(false);
                    break;
            }
        };

        // 目標選択の選択確定時
        ObjectiveSelectionTargetTable.ItemSelected += (sender, selectedItem) =>
        {
            OnObjectiveSelected(selectedItem);
        };
    }

    public void SetData(Castle castle, Character characterSummaryTargetDefault)
    {
        targetCastle = castle;
        characterSummaryTarget = characterSummaryTargetDefault;
        this.characterSummaryTargetDefault = characterSummaryTargetDefault;

        showObjectiveSelection = false;

        Render();
    }

    public void Render()
    {
        SetCastleData(targetCastle);

        CharacterSummary.SetData(characterSummaryTarget);
    }

    /// <summary>
    /// 城情報を設定します。
    /// </summary>
    private void SetCastleData(Castle castle)
    {
        if (castle == null)
        {
            Root.style.display = DisplayStyle.None;
            return;
        }
        Root.style.display = DisplayStyle.Flex;

        // 城名・地方・方針
        iconCountry.style.backgroundImage = new(Static.GetCountrySprite(castle.Country.ColorIndex));
        labelCastleName.text = castle.Name;
        SetRelation(castle.Country);

        labelCastleRegion.text = castle.Region;
        ObjectiveContainer.style.display = Util.Display(Core.World.Player?.Country == castle.Country);
        var canOrder = (castle.Boss?.IsPlayer ?? false) || castle.Country.Ruler.IsPlayer;
        labelObjective.style.display = Util.Display(!canOrder);
        comboObjective.style.display = Util.Display(canOrder);

        // 方針
        NormalView.style.display = Util.Display(!showObjectiveSelection);
        ObjectiveSelectionView.style.display = Util.Display(showObjectiveSelection);
        var objectiveText = targetCastle.Objective switch
        {
            CastleObjective.Attack o => $"{o.TargetCastleName}攻略",
            CastleObjective.Train => $"訓練",
            CastleObjective.Fortify => $"防備",
            CastleObjective.Develop => $"開発",
            CastleObjective.Transport o => $"{o.TargetCastleName}輸送",
            _ => "なし",
        };
        labelObjective.text = objectiveText;
        // 余計な変更イベントが起きないように、目標選択中はコンボボックスの値を変更しない。
        if (!showObjectiveSelection)
        {
            comboObjective.value = objectiveText;
        }

        // 城主
        if (castle.Boss != null)
        {
            CastleBossImage.style.backgroundImage = new(Static.GetFaceImage(castle.Boss));
        }
        else
        {
            CastleBossImage.style.backgroundImage = null;
        }

        // パラメーター等
        labelGold.text = castle.Gold.ToString("0");
        var balance = castle.GoldBalance;
        labelBalance.text = balance > 0 ? $"+{(int)balance}" : $"{(int)balance}";
        labelBalance.style.color = balance >= 0 ? Color.green : Color.red;
        labelIncome.text = $"{castle.GoldIncome:0}";
        labelMaxIncome.text = $"{castle.GoldIncomeMax:0}";
        labelExpenditure.text = $"{castle.GoldComsumption:0}";
        labelDevLevel.text = $"{castle.DevLevel}";
        labelTotalInvestment.text = $"{castle.TotalInvestment:0}";
        labelCastleStrength.text = $"{castle.Strength:0}";
        labelTotalPower.text = $"{castle.SoldierCount:0}";
        labelTotalPowerMax.text = $"{castle.SoldierCountMax:0}";
        
        // 収入バー
        const float IncomeBarMax = 800f;
        MaxIncomeBar.style.width = Length.Percent(Mathf.Clamp01(castle.GoldIncomeMax / IncomeBarMax) * 100f);
        CurrentIncomeBar.style.width = Length.Percent(Mathf.Clamp01(castle.GoldIncome / IncomeBarMax) * 100f);

        // アイコン表示 / 一覧表示
        MemberIconView.style.display = Util.Display(showMemberListIconView);
        MemberListView.style.display = Util.Display(!showMemberListIconView);

        // アイコン表示
        if (showMemberListIconView)
        {
            // 在城中キャラ一覧
            var inCastle = castle.Members.Where(m => !m.IsMoving).OrderBy(c => c.OrderIndex).ToList();
            labelInCastleMemberCount.text = $"({inCastle.Count}名)";
            labelInCastleSoldierCount.text = $"{inCastle.Sum(c => c.Soldiers.SoldierCount):0}";
            labelInCastleSoldierCountMax.text = $"{inCastle.Sum(c => c.Soldiers.SoldierCountMax):0}";
            ShowCharacterIcons(inCastle, InCastleMemberIconsContainer);
            // 出撃中キャラ一覧
            var deployed = castle.Members.Where(m => m.IsMoving).OrderBy(c => c.OrderIndex).ToList();
            labelDeployedMemberCount.text = $"({deployed.Count}名)";
            labelDeployedSoldierCount.text = $"{deployed.Sum(c => c.Soldiers.SoldierCount):0}";
            labelDeployedSoldierCountMax.text = $"{deployed.Sum(c => c.Soldiers.SoldierCountMax):0}";
            ShowCharacterIcons(deployed, DeployedMemberIconsContainer);
        }
        // リスト表示
        else
        {
            MemberListViewTable.SetData(targetCastle.Members, null);
        }
    }

    /// <summary>
    /// 方針コンボボックスの値が変更されたときに呼ばれます。
    /// </summary>
    private void OnObjectiveComboBoxSelectionChanged(ChangeEvent<string> evt)
    {
        Debug.Log($"Objective changed: {evt.newValue}");
        if (targetCastle == null) return;
        switch (comboObjective.index)
        {
            // 拠点攻略
            case 0:
                var targets = targetCastle.Neighbors
                    .Where(c => targetCastle.IsAttackable(c))
                    .ToList();
                Core.World.Map.SetEnableHighlight(targets);
                Core.World.Map.SetCustomEventHandler(tile =>
                {
                    var isInTargets = targets.Contains(tile.Castle);
                    if (isInTargets) OnObjectiveSelected(tile.Castle);
                });
                ShowObjectiveSelectView("攻略目標を選択してください", targets, c => c.Name);
                break;
            // 訓練
            case 1:
                targetCastle.Objective = new CastleObjective.Train();
                HideObjectiveSelectView();
                break;
            // 防備
            case 2:
                targetCastle.Objective = new CastleObjective.Fortify();
                HideObjectiveSelectView();
                break;
            // 開発
            case 3:
                targetCastle.Objective = new CastleObjective.Develop();
                HideObjectiveSelectView();
                break;
            // 輸送
            case 4:
                targets = targetCastle.Country.Castles
                    .Where(c => c != targetCastle)
                    .ToList();
                Core.World.Map.SetEnableHighlight(targets);
                Core.World.Map.SetCustomEventHandler(tile =>
                {
                    var isInTargets = targets.Contains(tile.Castle);
                    if (isInTargets) OnObjectiveSelected(tile.Castle);
                });
                ShowObjectiveSelectView("輸送目標を選択してください", targets, c => c.Name);
                break;
            // なし
            case 5:
                targetCastle.Objective = new CastleObjective.None();
                HideObjectiveSelectView();
                break;
        }
    }

    /// <summary>
    /// 目標選択画面を表示します。
    /// </summary>
    private void ShowObjectiveSelectView<T>(string title, IReadOnlyList<T> options, Func<T, string> toString)
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
            case 0: // 拠点攻略
                var castle = (Castle)selectedItem;
                targetCastle.Objective = new CastleObjective.Attack() { TargetCastleName = castle.Name, };

                var tile = Core.World.Map.GetTile(castle.Position);
                tile.UI.SetFocusHighlight(false);
                break;
            case 4: // 輸送
                castle = (Castle)selectedItem;
                targetCastle.Objective = new CastleObjective.Transport() { TargetCastleName = castle.Name, };

                tile = Core.World.Map.GetTile(castle.Position);
                tile.UI.SetFocusHighlight(false);
                break;
        }

        HideObjectiveSelectView();
    }

    /// <summary>
    /// キャラクターのアイコンを表示します。
    /// </summary>
    private void ShowCharacterIcons(IEnumerable<Character> characters, VisualElement iconContainer)
    {
        iconContainer.Clear();
        foreach (var character in characters)
        {
            var icon = CreateCharacterIcon(character);
            iconContainer.Add(icon);
        }
    }

    /// <summary>
    /// キャラクターのアイコンを作成します。
    /// </summary>
    private VisualElement CreateCharacterIcon(Character character)
    {
        var faceImage = new VisualElement();
        faceImage.AddToClassList("SmallCharacterIcon");
        faceImage.style.backgroundImage = new(Static.GetFaceImage(character));
        faceImage.style.opacity = character.IsIncapacitated ? 0.3f : 1.0f;
        faceImage.Register<MouseOverEvent>(_ => ShowCharacterSummary(character));
        return faceImage;
    }

    private void ShowCharacterSummary(Character character)
    {
        if (character == characterSummaryTarget) return;
        characterSummaryTarget = character;
        CharacterSummary.SetData(character);
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

}