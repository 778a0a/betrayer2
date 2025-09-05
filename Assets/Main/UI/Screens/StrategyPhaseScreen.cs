using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class StrategyPhaseScreen : IScreen
{
    private GameCore Core => GameCore.Instance;
    private ActionButtonHelper[] strategyButtons;
    private ActionButtonHelper[] personalButtons;
    private Character currentCharacter;
    private GameMapTile currentTile;
    private bool isPersonalPhase = false;

    public void Initialize()
    {
        strategyButtons = new[]
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
        };
        
        personalButtons = new[]
        {
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
            OnActionButtonClicked(ActionButtonHelper.Common(a => a.FinishTurn));
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

    /// <summary>
    /// デフォルトのセルクリック動作が行われたときに呼び出されます
    /// </summary>
    public void OnDefaultCellClicked(MapPosition pos)
    {
        Debug.Log($"DefaultClick {pos}");
        currentTile = Core.World.Map.GetTile(pos);
        Render();
    }

    public void Show(Character chara, bool personalPhase = false)
    {
        Core.MainUI.HideAllPanels();
        isPersonalPhase = personalPhase;
        SetData(chara);
        Root.style.display = DisplayStyle.Flex;

        ActionPanelContent.style.display = DisplayStyle.Flex;
        buttonToggleActionPanelFolding.text = "-";
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
        PersonalPhaseHeader.style.display = Util.Display(isPersonalPhase);
        PersonalActionButtons.style.display = Util.Display(isPersonalPhase);
        StrategyPhaseHeader.style.display = Util.Display(!isPersonalPhase);
        StrategyActionButtons.style.display = Util.Display(!isPersonalPhase);
        if (isPersonalPhase)
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
        if (isPersonalPhase)
        {
            ActionPanel.style.display = DisplayStyle.Flex;
            NoActionPanel.style.display = DisplayStyle.None;
        }
        else
        {
            ActionPanel.style.display = Util.Display(targetTile == characterTile);
            NoActionPanel.style.display = Util.Display(targetTile != characterTile);
        }
    }
}