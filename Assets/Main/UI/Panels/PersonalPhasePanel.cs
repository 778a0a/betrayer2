using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class PersonalPhasePanel : IPanel
{
    private ActionButtonHelper[] buttons;
    private Character currentCharacter;

    public void Initialize()
    {
        buttons = new[]
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
            ActionButtonHelper.Common(a => a.FinishTurn),
        };

        foreach (var button in buttons)
        {
            ActionButtons.Add(button.Element);
            button.SetEventHandlers(
                labelCostGold,
                labelActionDescription,
                () => currentCharacter,
                OnActionButtonClicked
            );
        }
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
        SetData(chara, GameCore.Instance.World);
    }


    public void SetData(Character chara, WorldData world)
    {
        currentCharacter = chara;

        // キャラクターサマリーの更新
        CharacterSummary.SetData(chara, world);

        // アクションボタンの状態更新
        foreach (var button in buttons)
        {
            button.SetData(chara);
        }
    }
}

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
            labelCostGold.text = cost.actorGold.ToString();
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