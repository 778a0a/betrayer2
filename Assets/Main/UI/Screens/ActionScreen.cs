using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ActionScreen : IScreen
{
    private GameCore Core => GameCore.Instance;
    private ActionButtonHelper[] strategyButtons;
    private ActionButtonHelper[] personalButtons;
    private Character currentCharacter;
    private GameMapTile currentTile;
    public bool IsPersonalPhase { get; set; } = false;
    public bool IsStrategyPhase => !IsPersonalPhase;

    public void Initialize()
    {
        strategyButtons = new[]
        {
            ActionButtonHelper.Strategy(a => a.Deploy),
            ActionButtonHelper.Strategy(a => a.Transport),
            ActionButtonHelper.Strategy(a => a.Bonus),
            ActionButtonHelper.Strategy(a => a.HireVassal),
            ActionButtonHelper.Strategy(a => a.FireVassal),
            ActionButtonHelper.Strategy(a => a.Goodwill),
            ActionButtonHelper.Strategy(a => a.Ally),
            ActionButtonHelper.Strategy(a => a.Invest),
            //ActionButtonHelper.Strategy(a => a.BuildTown),
            ActionButtonHelper.Strategy(a => a.DepositCastleGold),
            ActionButtonHelper.Strategy(a => a.WithdrawCastleGold),
            ActionButtonHelper.Strategy(a => a.BecomeIndependent),
        };
        
        personalButtons = new[]
        {
            ActionButtonHelper.Personal(a => a.ChangeDestination),
            ActionButtonHelper.Personal(a => a.BackToCastle),
            ActionButtonHelper.Personal(a => a.HireSoldier),
            ActionButtonHelper.Personal(a => a.TrainSoldiers),
            ActionButtonHelper.Personal(a => a.Develop),
            ActionButtonHelper.Personal(a => a.Invest),
            ActionButtonHelper.Personal(a => a.Fortify),
            ActionButtonHelper.Personal(a => a.Deploy),
            ActionButtonHelper.Personal(a => a.Rebel),
            ActionButtonHelper.Personal(a => a.Resign),
            ActionButtonHelper.Personal(a => a.Relocate),
            ActionButtonHelper.Personal(a => a.Seize),
            ActionButtonHelper.Personal(a => a.GetJob),
        };

        foreach (var button in strategyButtons)
        {
            button.SetEventHandlers(
                labelCost,
                labelActionDescription,
                () => currentCharacter,
                OnActionButtonClicked
            );
        }
        
        foreach (var button in personalButtons)
        {
            button.SetEventHandlers(
                labelCost,
                labelActionDescription,
                () => currentCharacter,
                OnActionButtonClicked
            );
        }

        Reinitialize();
    }

    public void Reinitialize()
    {
        foreach (var button in strategyButtons)
        {
            StrategyActionButtons.Add(button.Element);
        }
        
        foreach (var button in personalButtons)
        {
            PersonalActionButtons.Add(button.Element);
        }
        
        buttonTurnEnd.clicked += () =>
        {
            // ターン終了
            GameCore.Instance.Booter.hold = false;
        };

        buttonMoveToMyCastle.clicked += async () =>
        {
            var map = Core.World.Map;
            var tile = map.GetTile(currentCharacter.Castle);
            currentTile = tile;
            map.ScrollTo(tile);
            Render();
            tile.UI.SetCellBorder(true);
            await Task.Delay(400);
            tile.UI.SetCellBorder(false);
        };

        buttonToggleActionPanelFolding.clicked += () =>
        {
            var show = ActionPanelContent.style.display != DisplayStyle.Flex;
            ActionPanelContent.style.display = Util.Display(show);
            buttonToggleActionPanelFolding.text = show ? "-" : "+";
        };
        
        buttonClose.clicked += () =>
        {
            Root.style.display = DisplayStyle.None;
        };

        CastleInfoPanel.Initialize();
    }

    private async void OnActionButtonClicked(ActionButtonHelper button)
    {
        var chara = currentCharacter;
        var action = button.Action;

        var canPrepare = action.CanUIEnable(chara);
        if (!canPrepare)
        {
            return;
        }

        // アクションを実行する。
        await action.Do(new(chara, selectedTile: currentTile));

        // 表示を更新する。
        Render();
        Root.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// デフォルトのセルクリック動作が行われたときに呼び出されます
    /// </summary>
    public void OnDefaultCellClicked(MapPosition pos)
    {
        Debug.Log($"DefaultClick {pos}");
        var prevTile = currentTile;
        currentTile = Core.World.Map.GetTile(pos);

        // 同じ場所のクリックの場合、タブを次に切り替える
        if (prevTile == currentTile)
        {
            var currentTab = CastleInfoPanel.CurrentTab;

            var nextTab = (CastleInfoTabType)(((int)currentTab + 1) % Util.EnumArray<CastleInfoTabType>().Length);
            CastleInfoPanel.SwitchTab(nextTab);
        }
        else
        {
            switch (CastleInfoPanel.CurrentTab)
            {
                case CastleInfoTabType.Castle:
                case CastleInfoTabType.Country:
                    // 城がなく、軍勢がいるなら軍勢タブにする。
                    if (!currentTile.HasCastle && Core.World.Forces.Any(f => f.Position == currentTile.Position))
                    {
                        CastleInfoPanel.SwitchTab(CastleInfoTabType.Force);
                    }
                    break;
                case CastleInfoTabType.Force:
                    // 軍勢がなく、城があるなら城タブにする。
                    if (!Core.World.Forces.Any(f => f.Position == currentTile.Position) && currentTile.HasCastle)
                    {
                        CastleInfoPanel.SwitchTab(CastleInfoTabType.Castle);
                    }
                    break;
                default:
                    break;
            }
        }

        Render();
    }

    public void Show(Character chara, bool personalPhase = false)
    {
        Core.MainUI.HideAllPanels();
        IsPersonalPhase = personalPhase;
        SetData(chara);
        Root.style.display = DisplayStyle.Flex;

        ActionPanelContent.style.display = DisplayStyle.Flex;
        buttonToggleActionPanelFolding.text = "-";
    }

    public void Show()
    {
        SetData(currentCharacter);
        Root.style.display = DisplayStyle.Flex;
    }

    public void SetData(Character chara)
    {
        currentCharacter = chara;
        currentTile = Core.World.Map.GetTile(chara.Castle.Position);
        Render();
    }

    public void Render()
    {
        // フェーズに応じてヘッダーとボタンを切り替え
        PersonalPhaseHeader.style.display = Util.Display(IsPersonalPhase);
        PersonalActionButtons.style.display = Util.Display(IsPersonalPhase);
        StrategyPhaseHeader.style.display = Util.Display(!IsPersonalPhase);
        StrategyActionButtons.style.display = Util.Display(!IsPersonalPhase);
        if (IsPersonalPhase)
        {
            labelPhaseTitle.text = "個人";
            labelPhaseTitle.style.color = Color.yellow;
            labelPhaseSubtitle.text = "フェーズ";
            labelCurrentPersonalGold.text = currentCharacter.Gold.ToString("0");
            foreach (var button in personalButtons)
            {
                button.SetData(currentCharacter);
            }
        }
        else
        {
            labelPhaseTitle.text = "戦略";
            labelPhaseTitle.style.color = Color.cyan;
            labelPhaseSubtitle.text = "フェーズ";
            labelCurrentCastleGold.text = currentCharacter.Castle.Gold.ToString("0");
            labelCurrentAP.text = currentCharacter.ActionPoints.ToString();
            foreach (var button in strategyButtons)
            {
                button.SetData(currentCharacter);
            }
        }
        
        var characterTile = Core.World.Map.GetTile(currentCharacter.Castle);
        var targetTile = currentTile ?? characterTile;
        var summaryDefault = currentCharacter;
        if (targetTile != characterTile && targetTile.Castle != null)
        {
            summaryDefault = targetTile.Castle.Boss;
        }
        CastleInfoPanel.SetData(targetTile, summaryDefault);

        // 個人フェーズでは常にアクション可能、戦略フェーズでは条件付き
        if (IsPersonalPhase)
        {
            ActionPanel.style.display = DisplayStyle.Flex;
            NoActionPanel.style.display = DisplayStyle.None;
        }
        // 戦略フェーズの場合
        else
        {
            // 城なしの場合はアクションなし。
            if (!targetTile.HasCastle)
            {
                ActionPanel.style.display = DisplayStyle.None;
                NoActionPanel.style.display = DisplayStyle.Flex;
                // 軍勢の指示は軍勢一覧から行ってもらう。
                return;
            }
            // 自身が君主でない場合は自城以外ではアクション不可。
            if (!currentCharacter.IsRuler && targetTile != characterTile)
            {
                ActionPanel.style.display = DisplayStyle.None;
                NoActionPanel.style.display = DisplayStyle.Flex;
                return;
            }

            // 以下は自身が君主の場合
            ActionPanel.style.display = DisplayStyle.Flex;
            NoActionPanel.style.display = DisplayStyle.None;

            // 自城の場合は全てのアクションを実行可能
            if (targetTile == characterTile)
            {
                foreach (var button in strategyButtons)
                {
                    button.Element.style.display = DisplayStyle.Flex;
                }
            }
            // 自国の他の城の場合は、特定のアクションのみ実行可能
            else if (targetTile.Castle.Country == currentCharacter.Country)
            {
                foreach (var button in strategyButtons)
                {
                    var show = button.Action switch
                    {
                        StrategyActions.DeployAction => true,
                        StrategyActions.TranspotAction => true,
                        StrategyActions.BonusAction => true,
                        _ => false
                    };
                    button.Element.style.display = Util.Display(show);
                }
            }
            // 他国の城の場合は、特定のアクションのみ実行可能
            else
            {
                foreach (var button in strategyButtons)
                {
                    var show = button.Action switch
                    {
                        StrategyActions.AllyAction => true,
                        StrategyActions.GoodwillAction => true,
                        _ => false
                    };
                    button.Element.style.display = Util.Display(show);
                }
            }
        }
    }
}