using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public record CountryInBattle(
    Country Country,
    bool IsAttacker)
{
    public CountryInBattle Opponent { get; set; }
    public bool IsDefender => !IsAttacker;
    public Member[] Members { get; private set; }
    public bool HasPlayer => Members.Any(m => m.Character.IsPlayer);

    public void Initialize()
    {
        Members = Country.Members.Select(c => new Member(this, c)).ToArray();
    }

    public bool NoSoldierExists => !Members
        .Where(m => m.State == KessenMemberState.Alive)
        .Any(m => m.Character.Force.Soldiers.Any(s => s.IsAlive));

    /// <summary>
    /// 戦闘後の回復処理
    /// </summary>
    public void Recover(bool win)
    {
        foreach (var member in Members)
        {
            CharacterInBattle.Recover(member.Character, win, 0.5f, 0.1f);
        }
    }

    public override string ToString() => $"{Country?.Name}({Country?.Power})";


    public record Member(CountryInBattle Country, Character Character)
    {
        /// <summary>
        /// 戦闘の強さ
        /// </summary>
        public int Strength => Math.Max(Character.Attack, Character.Defense);

        public KessenMemberState State { get; set; } = KessenMemberState.Alive;

        public bool ShouldRetreat(int tickCount)
        {
            // プレーヤーの場合はUIで判断しているので処理不要。
            if (Character.IsPlayer) return false;

            // 戦闘開始直後は撤退しない。
            if (tickCount < 10) return false;

            // まだ損耗が多くないなら撤退しない。
            // （現在の戦闘で全滅した兵士も数えるために、IsAliveではなく!IsEmptySlotを使う）
            var manyLoss = Character.Force.Soldiers.Where(s => !s.IsEmptySlot).Count(s => s.Hp < 10) >= 3;
            if (!manyLoss) return false;

            // 敵よりも兵力が多いなら撤退しない。
            var myPower = Country.Country.Power;
            var opPower = Country.Opponent.Country.Power;
            if (myPower > opPower) return false;

            // 撤退する。
            return true;
        }
    }
}

public enum KessenMemberState
{
    Alive,
    Retreated,
}
