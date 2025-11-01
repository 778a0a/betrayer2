using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class ActionButtonHelper
{
    public Button Element { get; private set; }
    public ActionBase Action => actionGetter();
    
    private readonly Func<ActionBase> actionGetter;
    private Label labelCostGold;
    private Label labelDescription;
    private Func<(Character, GameMapTile)> currentContextGetter;
    private Action<ActionButtonHelper, EventBase> clickHandler;
    private bool IsMouseOver;

    private ActionButtonHelper(Button el, Func<ActionBase> actionGetter)
    {
        Element = el;
        this.actionGetter = actionGetter;
    }

    public static ActionButtonHelper Personal(Func<PersonalActions, PersonalActionBase> actionSelector)
    {
        var button = new Button();
        button.AddToClassList("ActionButton");
        return new ActionButtonHelper(button, () => actionSelector(GameCore.Instance.PersonalActions));
    }

    public static ActionButtonHelper Strategy(Func<StrategyActions, StrategyActionBase> actionSelector)
    {
        var button = new Button();
        button.AddToClassList("ActionButton");
        return new ActionButtonHelper(button, () => actionSelector(GameCore.Instance.StrategyActions));
    }

    public void SetEventHandlers(
        Label labelCostGold,
        Label labelDescription,
        Func<(Character, GameMapTile)> currentContextGetter,
        Action<ActionButtonHelper, EventBase> clickHandler)
    {
        this.labelCostGold = labelCostGold;
        this.labelDescription = labelDescription;
        this.currentContextGetter = currentContextGetter;
        this.clickHandler = clickHandler;

        // なぜかMouseDownEventだと左クリックが受け取れないのでClickEventも購読する。
        Element.RegisterCallback<MouseDownEvent>(OnActionButtonMouseDown);
        Element.RegisterCallback<ClickEvent>(OnActionButtonMouseDown);
        Element.RegisterCallback<PointerEnterEvent>(OnActionButtonPointerEnter);
        Element.RegisterCallback<PointerLeaveEvent>(OnActionButtonPointerLeave);
    }

    private async void OnActionButtonPointerEnter(PointerEnterEvent evt)
    {
        await Awaitable.NextFrameAsync();
        var (chara, selectedTile) = currentContextGetter();

        IsMouseOver = true;
        labelDescription.text = Action.Description;
        var cost = Action.Cost(new(chara, selectedTile: selectedTile, estimate: true));
        if (cost.IsVariable)
        {
            labelCostGold.text = "---";
        }
        else
        {
            var costs = new List<string>();
            if (cost.actorGold > 0)
            {
                costs.Add($"所持金 <color=#ffff00>{cost.actorGold}</color>");
            }
            if (cost.castleGold > 0)
            {
                costs.Add($"城資金 <color=#ffff00>{cost.castleGold}</color>");
            }
            if (cost.actionPoints > 0)
            {
                costs.Add($"采配P <color=red>{cost.actionPoints}</color>");
            }
            if (costs.Count == 0)
            {
                costs.Add("なし");
            }
            labelCostGold.text = string.Join(", ", costs);
        }
    }

    private async void OnActionButtonPointerLeave(PointerLeaveEvent evt)
    {
        await Awaitable.NextFrameAsync();
        IsMouseOver = false;
        labelDescription.text = "";
        labelCostGold.text = "";
    }

    private void OnActionButtonMouseDown(EventBase evt)
    {
        clickHandler(this, evt);
        evt.StopPropagation();
    }

    public void SetData(Character chara, GameMapTile tile)
    {
        Element.text = Action.Label;
        Element.style.display = Util.Display(Action.Visible(chara, tile));
        try
        {
            Element.SetEnabled(Action.Enabled(chara, tile));
        }
        catch (Exception ex)
        {
            Debug.LogError($"アクション有効判定でエラー: {Action.Label} on character: {chara?.Name ?? "null"}");
            Debug.LogException(ex);
            Element.SetEnabled(false);
        }
        if (IsMouseOver)
        {
            OnActionButtonPointerEnter(null);
        }
    }
}