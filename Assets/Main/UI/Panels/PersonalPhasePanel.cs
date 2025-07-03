using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public partial class PersonalPhasePanel
{
    private ActionButtonHelper[] buttons;
    private Character currentCharacter;

    public void Initialize()
    {
        buttons = new[]
        {
            ActionButtonHelper.Personal(buttonHireSoldier, a => a.HireSoldier),
            ActionButtonHelper.Personal(buttonTrainSoldier, a => a.TrainSoldiers),
            ActionButtonHelper.Personal(buttonDevelop, a => a.Develop),
            ActionButtonHelper.Personal(buttonInvest, a => a.Invest),
            ActionButtonHelper.Personal(buttonFortify, a => a.Fortify),
            ActionButtonHelper.Personal(buttonDeploy, a => a.Deploy),
            ActionButtonHelper.Personal(buttonRebel, a => a.Rebel),
            ActionButtonHelper.Personal(buttonResign, a => a.Resign),
            ActionButtonHelper.Personal(buttonMove, a => a.Relocate),
            ActionButtonHelper.Personal(buttonSeize, a => a.Seize),
            ActionButtonHelper.Personal(buttonGetJob, a => a.GetJob),
            ActionButtonHelper.Common(buttonFinishTurn, a => a.FinishTurn),
        };

        foreach (var button in buttons)
        {
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

    private CharacterInfoSoldierIcon[] soldierIcons;

    public void SetData(Character chara, WorldData world)
    {
        currentCharacter = chara;

        // 基本情報の更新
        imagePlayerFace.image = Static.Instance.GetFaceImage(chara);
        labelPlayerName.text = chara.Name;
        labelPlayerTitle.text = chara.GetTitle(GameCore.Instance.MainUI.L);
        labelMoving.visible = chara.IsMoving;
        // 能力値の更新
        labelAttack.text = chara.Attack.ToString();
        labelDefense.text = chara.Defense.ToString();
        labelIntelligence.text = chara.Intelligence.ToString();
        labelGoverning.text = chara.Governing.ToString();
        // 資産情報の更新
        labelPlayerGold.text = chara.Gold.ToString();
        labelPlayerContribution.text = chara.Contribution.ToString("0");
        labelPlayerPrestige.text = chara.Prestige.ToString("0");


        // 兵士情報の更新
        soldierIcons ??= new[] { soldier00, soldier01, soldier02, soldier03, soldier04, soldier05, soldier06, soldier07, soldier08, soldier09, soldier10, soldier11, soldier12, soldier13, soldier14 };
        for (int i = 0; i < soldierIcons.Length; i++)
        {
            if (chara.Soldiers.Count <= i)
            {
                soldierIcons[i].Root.style.visibility = Visibility.Hidden;
                continue;
            }
            soldierIcons[i].Root.style.visibility = Visibility.Visible;
            soldierIcons[i].SetData(chara.Soldiers[i]);
        }
        
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

    public static ActionButtonHelper Personal(Button button, Func<PersonalActions, PersonalActionBase> actionSelector)
    {
        return new ActionButtonHelper(button, () => actionSelector(GameCore.Instance.PersonalActions));
    }

    public static ActionButtonHelper Common(Button button, Func<CommonActions, CommonActionBase> actionSelector)
    {
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
        Debug.Log($"{chara.Name}: {Action} OK?: {canSelect}");
        Element.style.display = Util.Display(canSelect);
        Element.SetEnabled(Action.CanUIEnable(chara));
        if (IsMouseOver)
        {
            OnActionButtonPointerEnter(null);
        }
    }
}