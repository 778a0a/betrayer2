using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public partial class BattleWindow// : IWindow
{
    private IBattleSoldierIcon[] _attackerSoldiers;
    private IBattleSoldierIcon[] _defenderSoldiers;

    public void Initialize()
    {
        Root.style.display = DisplayStyle.None;

        _attackerSoldiers = new[]
        {
            AttackerSoldier00, AttackerSoldier01, AttackerSoldier02, AttackerSoldier03, AttackerSoldier04, AttackerSoldier05, AttackerSoldier06, AttackerSoldier07, AttackerSoldier08, AttackerSoldier09, AttackerSoldier10, AttackerSoldier11, AttackerSoldier12, AttackerSoldier13, AttackerSoldier14,
        };
        _defenderSoldiers = new[]
        {
            DefenderSoldier00, DefenderSoldier01, DefenderSoldier02, DefenderSoldier03, DefenderSoldier04, DefenderSoldier05, DefenderSoldier06, DefenderSoldier07, DefenderSoldier08, DefenderSoldier09, DefenderSoldier10, DefenderSoldier11, DefenderSoldier12, DefenderSoldier13, DefenderSoldier14,
        };
    }

    public void SetData(Battle battle, BattleResult? result = null)
    {
        var attacker = battle.Attacker.Character;
        var defender = battle.Defender.Character;
        var attackerTerrain = battle.Attacker.Terrain;
        var defenderTerrain = battle.Defender.Terrain;

        // 戦闘終了後の場合
        if (result != null)
        {
            buttonAttack.style.display = DisplayStyle.None;
            buttonSwap12.style.display = DisplayStyle.None;
            buttonSwap23.style.display = DisplayStyle.None;
            buttonRest.style.display = DisplayStyle.None;
            buttonRetreat.style.display = DisplayStyle.None;
            buttonResult.style.display = DisplayStyle.Flex;
            buttonResult.text = result == BattleResult.AttackerWin ? "攻撃側の勝利" : "防衛側の勝利";
            if (result == BattleResult.AttackerWin)
            {
                Root.AddToClassList("attacker-win");
                Root.AddToClassList("defender-lose");
            }
            else
            {
                Root.AddToClassList("attacker-lose");
                Root.AddToClassList("defender-win");
            }
        }
        // 戦闘中の場合
        else
        {
            buttonAttack.style.display = Util.Display(battle.NeedInteraction);
            buttonSwap12.style.display = Util.Display(battle.NeedInteraction);
            buttonSwap23.style.display = Util.Display(battle.NeedInteraction);
            buttonRest.style.display = Util.Display(battle.NeedInteraction);
            buttonRetreat.style.display = Util.Display(battle.NeedInteraction);
            buttonResult.style.display = DisplayStyle.None;
            if (battle.NeedInteraction)
            {
                buttonSwap12.enabledSelf = battle.Player.CanSwap12;
                buttonSwap23.enabledSelf = battle.Player.CanSwap23;
                buttonRest.enabledSelf = battle.Player.CanRest;
                buttonRetreat.enabledSelf = battle.Player.CanRetreat;
            }

            Root.RemoveFromClassList("attacker-win");
            Root.RemoveFromClassList("attacker-lose");
            Root.RemoveFromClassList("defender-win");
            Root.RemoveFromClassList("defender-lose");
        }

        // デバッグ用
        buttonAttack.style.display = DisplayStyle.Flex;

        AttackerName.text = attacker.Name;
        DefenderName.text = defender.Name;

        var asols = battle.Attacker.OrderedSoldiers.Select((s, i) => (s, i));
        foreach (var (sol, index) in asols)
        {
            _attackerSoldiers[index].SetData(sol);
        }
        var dsols = battle.Defender.OrderedSoldiers.Select((s, i) => (s, i));
        foreach (var (sol, index) in dsols)
        {
            _defenderSoldiers[index].SetData(sol);
        }

        imageAttacker.style.backgroundImage = Static.GetFaceImage(attacker);
        labelAttackerAttack.text = battle.Attacker.Strength.ToString();
        labelAttackerIntelligense.text = attacker.Intelligence.ToString();
        labelAttackerTerrain.text = attackerTerrain.ToString();

        imageDefender.style.backgroundImage = Static.GetFaceImage(defender);
        labelDefenderDefence.text = battle.Defender.Strength.ToString();
        labelDefenderIntelligense.text = defender.Intelligence.ToString();
        labelDefenderTerrain.text = defenderTerrain.ToString();
    }

    public ValueTask<BattleAction> WaitPlayerClick()
    {
        var tcs = new ValueTaskCompletionSource<BattleAction>();

        void RemoveAllHandlers()
        {
            buttonAttack.clicked -= buttonAttackClicked;
            buttonSwap12.clicked -= buttonSwap12Clicked;
            buttonSwap23.clicked -= buttonSwap23Clicked;
            buttonRest.clicked -= buttonRestClicked;
            buttonRetreat.clicked -= buttonRetreatClicked;
            buttonResult.clicked -= buttonResultClicked;
        }

        buttonAttack.clicked += buttonAttackClicked;
        void buttonAttackClicked()
        {
            tcs.SetResult(BattleAction.Attack);
            RemoveAllHandlers();
        }

        buttonSwap12.clicked += buttonSwap12Clicked;
        void buttonSwap12Clicked()
        {
            tcs.SetResult(BattleAction.Swap12);
            RemoveAllHandlers();
        }

        buttonSwap23.clicked += buttonSwap23Clicked;
        void buttonSwap23Clicked()
        {
            tcs.SetResult(BattleAction.Swap23);
            RemoveAllHandlers();
        }

        buttonRest.clicked += buttonRestClicked;
        void buttonRestClicked()
        {
            tcs.SetResult(BattleAction.Rest);
            RemoveAllHandlers();
        }

        buttonRetreat.clicked += buttonRetreatClicked;
        void buttonRetreatClicked()
        {
            tcs.SetResult(BattleAction.Retreat);
            RemoveAllHandlers();
        }

        buttonResult.clicked += buttonResultClicked;
        void buttonResultClicked()
        {
            tcs.SetResult(BattleAction.Result);
            RemoveAllHandlers();
        }

        return tcs.Task;
    }
}

public enum BattleAction
{
    Attack,
    Swap12,
    Swap23,
    Rest,
    Retreat,
    Result,
}
