using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

public partial class PersonalPhasePanel : MainUIComponent, IPanel
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
        SetData(chara);
    }

    public void Show(Character chara)
    {
        UI.HideAllPanels();
        Root.style.display = DisplayStyle.Flex;
        SetData(chara);
    }

    public void SetData(Character chara)
    {
        currentCharacter = chara;
        CharacterSummary.SetData(chara);
        foreach (var button in buttons)
        {
            button.SetData(chara);
        }
    }
}
