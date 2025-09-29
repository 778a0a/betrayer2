using System;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ForceTableRowItem
{
    public event EventHandler<Force> MouseMove;
    public event EventHandler<Force> MouseDown;

    public Force Force { get; private set; }

    private CharacterInfoSoldierIcon[] soldierIcons;

    private bool IsStrategyPhase => GameCore.Instance.MainUI.ActionScreen.IsStrategyPhase;

    public void Initialize()
    {
        Root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        ForceTableRowItemRoot.RegisterCallback<ClickEvent>(OnMouseDown);
        
        // 変更ボタン
        buttonChangeDestination.clicked += async () =>
        {
            if (!IsStrategyPhase) return;
            var canOrder = Force?.Character?.CanOrder ?? false;
            if (!canOrder) return;
            var args = new ActionArgs(GameCore.Instance.World.Player, targetCharacter: Force.Character);
            await GameCore.Instance.StrategyActions.ChangeDestination.Do(args);
            GameCore.Instance.MainUI.ActionScreen.Root.style.display = DisplayStyle.Flex;
            GameCore.Instance.MainUI.ActionScreen.Render();
        };

        // 撤退ボタン
        buttonBackToCastle.clicked += async () =>
        {
            if (!IsStrategyPhase) return;
            var canOrder = Force?.Character?.CanOrder ?? false;
            if (!canOrder) return;
            var args = new ActionArgs(GameCore.Instance.World.Player, targetCharacter: Force.Character);
            await GameCore.Instance.StrategyActions.BackToCastle.Do(args);
            GameCore.Instance.MainUI.ActionScreen.Root.style.display = DisplayStyle.Flex;
            GameCore.Instance.MainUI.ActionScreen.Render();
        };
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        MouseMove?.Invoke(this, Force);
    }

    private void OnMouseDown(ClickEvent evt)
    {
        MouseDown?.Invoke(this, Force);
    }

    public void SetData(Force force, bool isClickable)
    {
        Force = force;
        var chara = force?.Character;
        if (chara == null)
        {
            Root.style.visibility = Visibility.Hidden;
            return;
        }
        Root.style.visibility = Visibility.Visible;

        ForceTableRowItemRoot.EnableInClassList("clickable", isClickable);
        
        iconCharacter.image = Static.GetFaceImage(chara);
        iconCountry.visible = !chara.IsFree;
        if (!chara.IsFree) iconCountry.style.backgroundImage = new(Static.GetCountrySprite(chara.Country.ColorIndex));
        labelName.text = chara.Name;
        labelName.style.color = GameCore.Instance.World.Countries.GetRelationColor(chara.Country);

        // 目的地
        labelDestination.text = force.Destination switch
        {
            Castle castle => castle.Name,
            _ => "不明"
        };
        // ETA
        labelETA.text = $"{force.ETADays:0}日";
        
        // 指揮官能力値
        labelAttack.text = chara.Attack.ToString();
        labelDefense.text = chara.Defense.ToString();
        labelIntelligence.text = chara.Intelligence.ToString();
        labelSoldiers.text = chara.Soldiers.SoldierCount.ToString();
        labelSoldiersMax.text = chara.Soldiers.SoldierCountMax.ToString();
        // 特性表示
        traitKnight.style.display = Util.Display(chara.Traits.HasFlag(Traits.Knight));
        traitDrillmaster.style.display = Util.Display(chara.Traits.HasFlag(Traits.Drillmaster));
        traitPirate.style.display = Util.Display(chara.Traits.HasFlag(Traits.Pirate));
        traitAdmiral.style.display = Util.Display(chara.Traits.HasFlag(Traits.Admiral));
        traitHunter.style.display = Util.Display(chara.Traits.HasFlag(Traits.Hunter));
        traitMountaineer.style.display = Util.Display(chara.Traits.HasFlag(Traits.Mountaineer));
        traitMerchant.style.display = Util.Display(chara.Traits.HasFlag(Traits.Merchant));
        traitDivineSpeed.style.display = Util.Display(chara.Traits.HasFlag(Traits.DivineSpeed));

        // 兵士情報
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

        // アクションボタン
        var actions = GameCore.Instance.StrategyActions;
        var args = new ActionArgs(GameCore.Instance.World.Player, targetCharacter: chara);
        var showAction = IsStrategyPhase && chara.CanOrder;
        ActionContainer.style.display = Util.Display(showAction);
        buttonChangeDestination.enabledSelf = showAction && actions.ChangeDestination.CanDo(args);
        buttonBackToCastle.enabledSelf = showAction && actions.BackToCastle.CanDo(args);
    }
}