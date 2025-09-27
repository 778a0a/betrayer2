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
        labelAttackerAttackCaption.text = battle.Attacker.UseAttack ? "攻撃" : "防衛";
        labelAttackerAttack.text = battle.Attacker.Strength.ToString();
        labelAttackerIntelligense.text = attacker.Intelligence.ToString();
        labelAttackerTerrain.text = attackerTerrain.ToString();

        imageDefender.style.backgroundImage = Static.GetFaceImage(defender);
        labelDefenderDefenceCaption.text = battle.Defender.UseAttack ? "攻撃" : "防衛";
        labelDefenderDefence.text = battle.Defender.Strength.ToString();
        labelDefenderIntelligense.text = defender.Intelligence.ToString();
        labelDefenderTerrain.text = defenderTerrain.ToString();

        // TacticsGaugeとRetreatGaugeの設定
        SetTacticsGaugeValue(AttackerTacticsBar1, AttackerTacticsBar2, AttackerTacticsBar3, battle.Attacker.TacticsGauge);
        SetRetreatGaugeValue(AttackerRetreatBar, battle.Attacker.RetreatGauge);
        SetTacticsGaugeValue(DefenderTacticsBar1, DefenderTacticsBar2, DefenderTacticsBar3, battle.Defender.TacticsGauge);
        SetRetreatGaugeValue(DefenderRetreatBar, battle.Defender.RetreatGauge);

        labelBattleType.text = battle.Type == BattleType.Field ? "野戦" : "攻城戦";

        // 城レベル表示 (攻城戦時のみ)
        DefenderCastleLevelContainer.style.display = Util.Display(battle.Type == BattleType.Siege);
        if (battle.Type == BattleType.Siege)
        {
            labelDefenderCastleLevel.text = battle.Defender.Tile.Castle.Strength.ToString();
        }
        // 自国領・敵国領表示
        AttackerOwnTerritoryContainer.style.display = Util.Display(battle.Attacker.IsInOwnTerritory);
        DefenderOwnTerritoryContainer.style.display = Util.Display(battle.Defender.IsInOwnTerritory);
        AttackerEnemyTerritoryContainer.style.display = Util.Display(battle.Attacker.IsInEnemyTerritory);
        DefenderEnemyTerritoryContainer.style.display = Util.Display(battle.Defender.IsInEnemyTerritory);
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

    private void SetTacticsGaugeValue(VisualElement bar1, VisualElement bar2, VisualElement bar3, float value)
    {
        // 0-100の値を0-33, 33-66, 66-100の3つの区間に分けて表示
        float clampedValue = Mathf.Clamp(value, 0f, 100f);

        // 第1区間 (0-33): 白色
        if (clampedValue <= 33f)
        {
            bar1.style.width = Length.Percent(clampedValue / 33f * 33.33f);
            bar2.style.width = Length.Percent(0f);
            bar3.style.width = Length.Percent(0f);
        }
        // 第2区間 (33-66): 薄い水色
        else if (clampedValue <= 66f)
        {
            bar1.style.width = Length.Percent(33.33f);
            bar2.style.width = Length.Percent((clampedValue - 33f) / 33f * 33.33f);
            bar3.style.width = Length.Percent(0f);
        }
        // 第3区間 (66-100): 青色
        else
        {
            bar1.style.width = Length.Percent(33.33f);
            bar2.style.width = Length.Percent(33.33f);
            bar3.style.width = Length.Percent((clampedValue - 66f) / 34f * 33.34f);
        }
    }

    private void SetRetreatGaugeValue(VisualElement bar, float value)
    {
        float clampedValue = Mathf.Clamp(value, 0f, 100f);
        bar.style.width = Length.Percent(clampedValue);
        bar.style.backgroundColor = clampedValue == 100f ? Color.orange : Color.darkOrange;
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
