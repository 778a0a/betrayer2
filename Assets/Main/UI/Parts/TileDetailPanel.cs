using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class TileDetailPanel
{
    public bool IsVisible => Root.style.display == DisplayStyle.Flex;
    
    public LocalizationManager L { get; set; }

    public void Initialize()
    {
        Root.style.display = DisplayStyle.None;
    }

    public void OnGameCoreAttached()
    {
        InitializeActionButtons();
    }

    public GameMapTile CurrentData { get; private set; }
    public void SetData(GameMapTile tile)
    {
        return;
        CurrentData = tile;
        if (tile == null)
        {
            Root.style.display = DisplayStyle.None;
            return;
        }
        var country = tile.Country;
        if (country == null)
        {
            Root.style.display = DisplayStyle.None;
            return;
        }

        Root.style.display = DisplayStyle.Flex;

        labelTileOwner.text = country.GetTerritoryName();
        imageTileCountryColor.style.backgroundImage = new StyleBackground(country.Sprite);

        var castle = tile.Castle;
        CastleInfo.style.display = Util.Display(castle != null);
        if (castle != null)
        {
            labelGovernor.text = castle.Boss?.Name;
            labelCastleStrength.text = castle.Strength.ToString("0");
            labelCastleGold.text = $"{castle.Gold:0} ({castle.GoldBalance:+0;-0})";
        }

        var town = tile.Town;
        TownInfo.style.display = Util.Display(town != null);
        if (town != null)
        {
            labelTownGoldIncome.text = $"{town.GoldIncome:0} / {town.GoldIncomeMax:0}";
        }
    }


    private ActionButton[] castleGoverningActions;
    private ActionButton[] castleMartialActions;
    private ActionButton[] castleStrategyActions;
    //private ActionButton[] castleDiplomaciesActions;
    private ActionButton[] townGoverningActions;
    private ActionButton[] currentShowingActions;
    private void InitializeActionButtons()
    {
        var core = GameCore.Instance;
        //castleGoverningActions = core.PersonalActions.Governings.Select(a => new ActionButton(this, a)).ToArray();
        //castleMartialActions = core.PersonalActions.Martials.Select(a => new ActionButton(this, a)).ToArray();
        //castleStrategyActions = core.PersonalActions.Strategies.Select(a => new ActionButton(this, a)).ToArray();
        ////castleDiplomaciesActions = core.CastleActions.Diplomacies.Select(a => new ActionButton(this, a)).ToArray();
        //townGoverningActions = core.StrategyActions.Governings.Select(a => new ActionButton(this, a)).ToArray();

        var mmm = new[]
        {
            (L["内政"], buttonCastleGoverning, castleGoverningActions),
            (L["軍事"], buttonCastleMartial, castleMartialActions),
            (L["戦略"], buttonCastleStrategy, castleStrategyActions),
            //(L["外交"], diplomaciesActions),
            (L["内政"], buttonTownGoverning, townGoverningActions),
        };

        ActionMenu.style.display = DisplayStyle.None;
        foreach (var (label, button, actions) in mmm)
        {
            button.clicked += () =>
            {
                if (currentShowingActions == actions)
                {
                    ActionMenu.style.display = DisplayStyle.None;
                    currentShowingActions = null;
                    return;
                }

                ActionMenu.style.display = DisplayStyle.Flex;
                labelActionMenuTitle.text = label;
                currentShowingActions = actions;

                var player = GameCore.Instance.World.Player;
                actionArgs = new ActionArgs
                {
                    actor = player,
                    targetCastle = CurrentData.Castle,
                    targetTown = CurrentData.Town
                };

                ActionButtonContainer.Clear();
                foreach (var act in actions)
                {
                    if (!act.Action.CanSelect(actionArgs)) continue;

                    act.Button.SetEnabled(act.Action.CanDo(actionArgs));
                    ActionButtonContainer.Add(act.Button);
                }
            };
        }
    }
    private ActionArgs actionArgs;

    private async void OnClick(ActionButton button)
    {
        var action = button.Action;
        await action.Do(actionArgs);
        SetData(CurrentData);
        foreach (var act in currentShowingActions)
        {
            if (!act.Action.CanSelect(actionArgs)) continue;

            act.Button.SetEnabled(act.Action.CanDo(actionArgs));
        }
    }

    private class ActionButton
    {
        private TileDetailPanel parent;
        public ActionBase Action { get; }
        public Button Button { get; } = new();
        public ActionButton(TileDetailPanel parent, ActionBase action)
        {
            this.parent = parent;
            Action = action;
            Button.text = action.Label;
            Button.RegisterCallback<ClickEvent>(OnClick);
            Button.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            Button.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        private void OnClick(ClickEvent evt)
        {
            parent.OnClick(this);
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            // TODO コスト表示
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
        }
    }
}

