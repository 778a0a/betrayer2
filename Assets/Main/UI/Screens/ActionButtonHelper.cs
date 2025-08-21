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
    private Func<Character> currentCharacterGetter;
    private Action<ActionButtonHelper> clickHandler;
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

    public static ActionButtonHelper Common(Func<CommonActions, CommonActionBase> actionSelector)
    {
        var button = new Button();
        button.AddToClassList("ActionButton");
        return new ActionButtonHelper(button, () => actionSelector(GameCore.Instance.CommonActions));
    }

    public void SetEventHandlers(
        Label labelCostGold,
        Label labelDescription,
        Func<Character> currentCharacterGetter,
        Action<ActionButtonHelper> clickHandler)
    {
        this.labelCostGold = labelCostGold;
        this.labelDescription = labelDescription;
        this.currentCharacterGetter = currentCharacterGetter;
        this.clickHandler = clickHandler;
        
        Element.RegisterCallback<ClickEvent>(OnActionButtonClicked);
        Element.RegisterCallback<PointerEnterEvent>(OnActionButtonPointerEnter);
        Element.RegisterCallback<PointerLeaveEvent>(OnActionButtonPointerLeave);
    }

    private void OnActionButtonPointerEnter(PointerEnterEvent evt)
    {
        var chara = currentCharacterGetter();

        IsMouseOver = true;
        labelDescription.text = Action.Description;
        var cost = Action.Cost(new(chara, estimate: true));
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

    private void OnActionButtonPointerLeave(PointerLeaveEvent evt)
    {
        IsMouseOver = false;
        labelDescription.text = "";
        labelCostGold.text = "";
    }

    private void OnActionButtonClicked(ClickEvent ev)
    {
        clickHandler(this);
    }

    public void SetData(Character chara)
    {
        var canSelect = Action.CanUISelect(chara);
        Element.text = Action.Label;
        Element.style.display = Util.Display(canSelect);
        try
        {
            Element.SetEnabled(Action.CanUIEnable(chara));
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