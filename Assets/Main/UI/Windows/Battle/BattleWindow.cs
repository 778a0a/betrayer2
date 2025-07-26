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

        if (result != null)
        {
            buttonAttack.style.display = DisplayStyle.None;
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
        else
        {
            buttonAttack.style.display = Util.Display(battle.NeedInteraction);
            buttonRetreat.style.display = Util.Display(battle.NeedInteraction);
            buttonResult.style.display = DisplayStyle.None;
            Root.RemoveFromClassList("attacker-win");
            Root.RemoveFromClassList("attacker-lose");
            Root.RemoveFromClassList("defender-win");
            Root.RemoveFromClassList("defender-lose");
        }

        // デバッグ用
        buttonAttack.style.display = DisplayStyle.Flex;

        AttackerName.text = attacker.Name;
        DefenderName.text = defender.Name;

        for (var i = 0; i < attacker.Soldiers.Count; i++)
        {
            var soldier = attacker.Soldiers[i];
            _attackerSoldiers[i].SetData(soldier);
        }
        for (var i = 0; i < defender.Soldiers.Count; i++)
        {
            var soldier = defender.Soldiers[i];
            _defenderSoldiers[i].SetData(soldier);
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

    public ValueTask<bool> WaitPlayerClick()
    {
        var tcs = new ValueTaskCompletionSource<bool>();

        buttonAttack.clicked += buttonAttackClicked;
        void buttonAttackClicked()
        {
            tcs.SetResult(true);
            buttonAttack.clicked -= buttonAttackClicked;
            buttonRetreat.clicked -= buttonRetreatClicked;
            buttonResult.clicked -= buttonResultClicked;
        }

        buttonRetreat.clicked += buttonRetreatClicked;
        void buttonRetreatClicked()
        {
            tcs.SetResult(false);
            buttonAttack.clicked -= buttonAttackClicked;
            buttonRetreat.clicked -= buttonRetreatClicked;
            buttonResult.clicked -= buttonResultClicked;
        }

        buttonResult.clicked += buttonResultClicked;
        void buttonResultClicked()
        {
            tcs.SetResult(true);
            buttonAttack.clicked -= buttonAttackClicked;
            buttonRetreat.clicked -= buttonRetreatClicked;
            buttonResult.clicked -= buttonResultClicked;
        }

        return tcs.Task;
    }
}