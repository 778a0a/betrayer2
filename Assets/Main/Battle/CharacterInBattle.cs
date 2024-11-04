using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public record CharacterInBattle(
    Character Character,
    Terrain Terrain,
    bool IsAttacker)
{
    public CharacterInBattle Opponent { get; set; }
    public Country Country = Character.Country; // TODO 奪取の場合
    public bool IsDefender = !IsAttacker;

    /// <summary>
    /// 戦闘の強さ
    /// 攻撃側ならAttack、防御側ならDefense
    /// </summary>
    public int Strength => IsAttacker ? Character.Attack : Character.Defense;

    public Soldiers Soldiers = Character.Soldiers;
    public bool IsPlayer = Character.IsPlayer;
    public bool AllSoldiersDead => Soldiers.All(s => !s.IsAlive);

    public static implicit operator Character(CharacterInBattle c) => c.Character;

    public bool ShouldRetreat(int tickCount, Battle battle)
    {
        // TODO
        //// プレーヤーの場合はUIで判断しているので処理不要。
        //if (IsPlayer) return false;

        // TODO
        //// 私闘の場合は撤退しない。
        //if (battle.Type is MartialActions.PrivateFightAction) return false;

        // 戦闘開始直後は撤退しない。
        if (tickCount < 3) return false;

        // まだ損耗が多くないなら撤退しない。
        // （現在の戦闘で全滅した兵士も数えるために、IsAliveではなく!IsEmptySlotを使う）
        var manyLoss = Character.Soldiers.Where(s => !s.IsEmptySlot).Count(s => s.Hp < 10) >= 3;
        if (!manyLoss) return false;

        // 敵よりも兵力が多いなら撤退しない。
        var myPower = Soldiers.Power;
        var opPower = Opponent.Soldiers.Power;
        if (myPower > opPower) return false;

        // 敵に残り数の少ない兵士がいるなら撤退しない。
        var opAboutToDie = Opponent.Soldiers.Any(s => s.IsAlive && s.Hp <= 3);
        if (opAboutToDie) return false;

        // 自国の最後の領土の防衛なら撤退しない。
        var lastArea = Country.Castles.Count == 1;
        if (lastArea && IsDefender) return false;

        // 撤退する。
        return true;
    }

    /// <summary>
    /// 戦闘後の回復処理
    /// </summary>
    public static void Recover(Character chara, bool win, float winRate, float loseRate)
    {
        if (chara == null) return;

        foreach (var s in chara.Soldiers)
        {
            if (!s.IsAlive) continue;

            var baseAmount = s.MaxHp * (win ? winRate : loseRate);
            var adj = Mathf.Max(0, (chara.Intelligence - 80) / 100f / 2);
            var amount = (int)(baseAmount * (1 + adj));
            s.Hp = Mathf.Min(s.MaxHp, s.Hp + amount);
        }
    }

    public override string ToString() => $"{Character?.Name}({Character?.Power})";
}
