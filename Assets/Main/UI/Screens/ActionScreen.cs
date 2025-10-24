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
            ActionButtonHelper.Strategy(a => a.Subdue),
            ActionButtonHelper.Strategy(a => a.Goodwill),
            ActionButtonHelper.Strategy(a => a.Ally),
            ActionButtonHelper.Strategy(a => a.BreakAlliance),
            ActionButtonHelper.Strategy(a => a.Invest),
            //ActionButtonHelper.Strategy(a => a.BuildTown),
            ActionButtonHelper.Strategy(a => a.DepositCastleGold),
            ActionButtonHelper.Strategy(a => a.WithdrawCastleGold),
            ActionButtonHelper.Strategy(a => a.BecomeIndependent),
            ActionButtonHelper.Strategy(a => a.Resign),
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
            ActionButtonHelper.Personal(a => a.SowDiscord),
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
                () => (currentCharacter, currentTile),
                OnActionButtonClicked
            );
        }
        
        foreach (var button in personalButtons)
        {
            button.SetEventHandlers(
                labelCost,
                labelActionDescription,
                () => (currentCharacter, currentTile),
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
            EndTurn();
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

    private bool IsRightClickEnabledAction(ActionBase action)
    {
        return action switch
        {
            PersonalActions.DevelopAction => true,
            PersonalActions.FortifyAction => true,
            PersonalActions.HireSoldierAction => true,
            PersonalActions.InvestAction => true,
            PersonalActions.TrainSoldiersAction => true,
            _ => false,
        };
    }

    private async void OnActionButtonClicked(ActionButtonHelper button, EventBase evt)
    {
        var chara = currentCharacter;
        var action = button.Action;

        ValueTask DoAction(bool isSpecial = false)
        {
            var canPrepare = action.Enabled(chara, currentTile);
            if (!canPrepare)
            {
                return default;
            }
            return action.Do(new(chara, selectedTile: currentTile, isSpecial: isSpecial));
        }

        var isRightClick = ((evt as MouseDownEvent)?.button ?? (evt as ClickEvent)?.button ?? 0) == 1;
        // 褒賞の場合は特殊実行を行う。
        if (isRightClick && action is StrategyActions.BonusAction bonus)
        {
            await DoAction(true);
        }
        // 連打が多いアクションは右クリックで10回実行できるようにする。
        else if (isRightClick && IsRightClickEnabledAction(action))
        {
            for (int i = 0; i < 10; i++)
            {
                await DoAction();
            }
        }
        else
        {
            await DoAction();
        }

        // 表示を更新する。
        Render();
        Root.style.display = DisplayStyle.Flex;

        // 放浪、独立、反乱を実行した場合はターンを終了する。
        var isResign = action is PersonalActions.ResignAction or StrategyActions.ResignAction && chara.IsFree;
        var isIndependent = action is StrategyActions.BecomeIndependentAction a && !a.IsCancelled;
        var isRebel = action is PersonalActions.RebelAction a2 && !a2.IsCancelled;
        if (isResign || isIndependent || isRebel)
        {
            Debug.Log("ターンを強制終了します。");
            EndTurn();
        }
    }

    private void EndTurn()
    {
        GameCore.Instance.Booter.hold = false;
    }

    /// <summary>
    /// デフォルトのセルクリック動作が行われたときに呼び出されます
    /// </summary>
    public void OnDefaultCellClicked(MapPosition pos)
    {
        //Debug.Log($"DefaultClick {pos}");
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
        if (currentCharacter != chara)
        {
            SetData(chara);
        }
        else
        {
            Render();
        }
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
        currentCharacter ??= Core.World.Characters.First();
        Root.style.display = DisplayStyle.Flex;

        var characterTile = Core.World.Map.GetTile(currentCharacter.Castle);
        var targetTile = currentTile ?? characterTile;

        // フェイズに応じてヘッダーとボタンを切り替える。
        PersonalPhaseHeader.style.display = Util.Display(IsPersonalPhase);
        PersonalActionButtons.style.display = Util.Display(IsPersonalPhase);
        StrategyPhaseHeader.style.display = Util.Display(!IsPersonalPhase);
        StrategyActionButtons.style.display = Util.Display(!IsPersonalPhase);
        if (IsPersonalPhase)
        {
            labelPhaseTitle.text = "個人";
            labelPhaseTitle.style.color = Color.yellow;
            labelCurrentPersonalGold.text = currentCharacter.Gold.ToString("0");
            foreach (var button in personalButtons)
            {
                button.SetData(currentCharacter, targetTile);
            }
        }
        else
        {
            labelPhaseTitle.text = "戦略";
            labelPhaseTitle.style.color = Color.cyan;
            labelCurrentCastleGold.text = currentCharacter.Castle.Gold.ToString("0");
            labelCurrentAP.text = currentCharacter.ActionPoints.ToString();
            foreach (var button in strategyButtons)
            {
                button.SetData(currentCharacter, targetTile);
            }
        }
        
        var summaryDefault = currentCharacter;
        if (targetTile != characterTile && targetTile.Castle != null)
        {
            summaryDefault = targetTile.Castle.Boss;
        }
        CastleInfoPanel.SetData(targetTile, summaryDefault);

        // 個人フェイズ
        if (IsPersonalPhase)
        {
            var noAction = personalButtons.All(b => b.Element.style.display.value == DisplayStyle.None);
            ActionPanel.style.display = Util.Display(!noAction);
            NoActionPanel.style.display = Util.Display(noAction);
        }
        // 戦略フェイズ
        else
        {
            var noAction = strategyButtons.All(b => b.Element.style.display.value == DisplayStyle.None);
            ActionPanel.style.display = Util.Display(!noAction);
            NoActionPanel.style.display = Util.Display(noAction);
        }
    }
}