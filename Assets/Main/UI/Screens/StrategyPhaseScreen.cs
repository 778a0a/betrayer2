using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class StrategyPhaseScreen : IScreen
{
    private GameCore Core => GameCore.Instance;
    private ActionButtonHelper[] buttons;
    private Character currentCharacter;
    private GameMapTile currentTile;

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
            //ActionButtonHelper.Common(a => a.FinishTurn),
        };

        foreach (var button in buttons)
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
        foreach (var button in buttons)
        {
            ActionButtons.Add(button.Element);
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

    public void Show(Character chara)
    {
        Core.MainUI.HideAllPanels();
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
        labelCurrentCastleGold.text = currentCharacter.Castle.Gold.ToString("0");
        labelCurrentAP.text = currentCharacter.ActionPoints.ToString();

        foreach (var button in buttons)
        {
            button.SetData(currentCharacter);
        }
        
        var characterTile = Core.World.Map.GetTile(currentCharacter.Castle);
        var targetTile = currentTile ?? characterTile;
        CastleInfoPanel.SetData(targetTile, currentCharacter);

        ActionPanel.style.display = Util.Display(targetTile == characterTile);
        NoActionPanel.style.display = Util.Display(targetTile != characterTile);
    }
}