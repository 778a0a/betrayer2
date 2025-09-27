using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public record CharacterInBattle(
    Character Character,
    GameMapTile Tile,
    bool IsAttacker,
    bool IsInCastle)
{
    public CharacterInBattle Opponent { get; set; }
    public Country Country = Character.Country; // TODO 奪取の場合
    public bool IsDefender = !IsAttacker;
    public bool IsInOwnTerritory = Tile.Country == Character.Country;
    public bool IsInEnemyTerritory => Tile.Country == Opponent.Country;
    public Terrain Terrain = Tile.Terrain;

    /// <summary>
    /// 戦術ゲージ（0～100）
    /// </summary>
    public float TacticsGauge { get; set; } = 0;
    /// <summary>
    /// 撤退ゲージ（0～100）
    /// </summary>
    public float RetreatGauge { get; set; } = 0;

    /// <summary>
    /// 12列交代が可能ならtrue
    /// </summary>
    public bool CanSwap12
    {
        get
        {
            // ゲージが33未満ならNG
            if (TacticsGauge < 33) return false;
            // 1列または2列が全滅ならNG
            if (Row1.All(s => !s.IsAlive) || Row2.All(s => !s.IsAlive)) return false;
            
            return true;
        }
    }

    public void Swap12(bool consumeTactics = true)
    {
        var row1Index = Array.IndexOf(SoldierPositions, SoldierPosition.Row1);
        var row2Index = Array.IndexOf(SoldierPositions, SoldierPosition.Row2);
        (SoldierPositions[row1Index], SoldierPositions[row2Index]) =
            (SoldierPositions[row2Index], SoldierPositions[row1Index]);
        if (consumeTactics) TacticsGauge -= 33;
    }

    /// <summary>
    /// 23列交代が可能ならtrue
    /// </summary>
    public bool CanSwap23
    {
        get
        {
            // ゲージが33未満ならNG
            if (TacticsGauge < 33) return false;
            // 2列または3列が全滅ならNG
            if (Row2.All(s => !s.IsAlive) || Row3.All(s => !s.IsAlive)) return false;
            return true;
        }
    }

    public void Swap23(bool consumeTactics = true)
    {
        var row2Index = Array.IndexOf(SoldierPositions, SoldierPosition.Row2);
        var row3Index = Array.IndexOf(SoldierPositions, SoldierPosition.Row3);
        (SoldierPositions[row2Index], SoldierPositions[row3Index]) =
            (SoldierPositions[row3Index], SoldierPositions[row2Index]);
        if (consumeTactics) TacticsGauge -= 33;
    }

    /// <summary>
    /// 休息が可能ならtrue
    /// </summary>
    public bool CanRest => TacticsGauge >= 66;

    public void Rest()
    {
        foreach (var s in Character.Soldiers)
        {
            if (!s.IsAlive) continue;
            var amount = Random.value * 2;
            s.HpFloat = (s.HpFloat + amount).MaxWith(s.MaxHp);
        }
        TacticsGauge -= 66;
    }

    /// <summary>
    /// 撤退が可能ならtrue
    /// </summary>
    public bool CanRetreat => RetreatGauge >= 100;

    private SoldierPosition[] SoldierPositions { get; set; } = new[] { SoldierPosition.Row1, SoldierPosition.Row2, SoldierPosition.Row3 };
    private SoldierPosition SoldierPosition1To5 => SoldierPositions[0];
    private SoldierPosition SoldierPosition6To10 => SoldierPositions[1];
    private SoldierPosition SoldierPosition11To15 => SoldierPositions[2];
    /// <summary>
    /// 戦列の位置
    /// </summary>
    private enum SoldierPosition
    {
        Row1 = 0,
        Row2 = 1,
        Row3 = 2,
    };

    public IEnumerable<Soldier> Row1 => Character.Soldiers.Skip(RowStartIndex(SoldierPosition.Row1)).Take(5);
    public IEnumerable<Soldier> Row2 => Character.Soldiers.Skip(RowStartIndex(SoldierPosition.Row2)).Take(5);
    public IEnumerable<Soldier> Row3 => Character.Soldiers.Skip(RowStartIndex(SoldierPosition.Row3)).Take(5);
    private int RowStartIndex(SoldierPosition pos)
    {
        if (pos == SoldierPosition1To5) return 0;
        if (pos == SoldierPosition6To10) return 5;
        return 10;
    }

    /// <summary>
    /// 全滅している列を詰めます。 
    /// </summary>
    public void CompactSoldierRows()
    {
        // 1列目
        if (Row1.All(s => !s.IsAlive))
        {
            Swap12(false);
            Swap23(false);
            // 2列目も全滅しているかもしれないので、その場合はもう一度行う。
            if (Row1.All(s => !s.IsAlive))
            {
                Swap12(false);
            }
        }
        // 2列目
        else if (Row2.All(s => !s.IsAlive))
        {
            Swap23(false);
        }
    }

    /// <summary>
    /// 戦闘前の兵士数
    /// </summary>
    public float[] InitialSoldierCounts = Character.Soldiers.Select(s => s.HpFloat).ToArray();

    /// <summary>
    /// 戦闘に利用する戦闘能力値
    /// </summary>
    public int Strength => UseAttack ? Character.Attack : Character.Defense;
    public bool UseAttack => !IsInOwnTerritory;
    public int Intelligence => Character.Intelligence;

    public IEnumerable<Soldier> OrderedSoldiers => Row1.Concat(Row2).Concat(Row3);
    public Soldiers Soldiers = Character.Soldiers;
    public bool IsPlayer = Character.IsPlayer;
    public bool AllSoldiersDead => Soldiers.All(s => !s.IsAlive);

    public static implicit operator Character(CharacterInBattle c) => c.Character;

    public BattleAction SelectAction(int tickCount, Battle battle)
    {
        // まず撤退の判断を行う。
        do 
        {
            // 撤退不可なら続行する。
            if (!CanRetreat) break;

            // TODO
            //// 私闘の場合は撤退しない。
            //if (battle.Type is MartialActions.PrivateFightAction) break;

            // 1列目がまだ余裕なら撤退しない。
            if (Row1.All(s => s.Hp > 12)) break;

            // 1列目が敵の1・2列目よりも強いなら撤退しない。
            var myRow1Power = Row1.Sum(s => s.Hp);
            var opRow1Power = Opponent.Row1.Sum(s => s.Hp);
            var opRow2Power = Opponent.Row2.Sum(s => s.Hp);
            if (myRow1Power > Mathf.Max(opRow1Power, opRow2Power)) break;

            // まだ損耗が多くないなら撤退しない。
            // （現在の戦闘で全滅した兵士も数えるために、IsAliveではなく!IsEmptySlotを使う）
            var manyLoss = Character.Soldiers.Where(s => !s.IsEmptySlot).Count(s => s.Hp < 10) >= 3;
            if (!manyLoss) break;

            // 兵士が十分残っている列があるなら撤退しない。
            var hasHealthyRow = new[] { Row1, Row2, Row3 }
                .Any(row => row.Count(s => s.IsAlive) > 3 && row.All(s => s.Hp == 0 || s.Hp >= 25));
            if (hasHealthyRow) break;

            // 敵よりも兵力が多いなら撤退しない。
            var myPower = Soldiers.Power;
            var opPower = Opponent.Soldiers.Power;
            var powerAdj = IsInCastle && IsDefender ? 1.1f : 1f;
            powerAdj += IsInCastle && IsDefender && Character.IsLoyal ? 0.1f : 0;
            if (myPower * powerAdj > opPower) break;

            //// 敵に残り数の少ない兵士がいるなら撤退しない。
            //var opAboutToDie = Opponent.Soldiers.Any(s => s.IsAlive && s.Hp <= 3);
            //if (opAboutToDie) break;

            if (IsInCastle && IsDefender)
            {
                // 君主か忠誠な家臣で自国の最後の領土の防衛なら撤退しない。
                var lastCastle = Country.Castles.Count == 1;
                if (lastCastle && Character.IsLoyal) break;
            }

            // 撤退する。
            return BattleAction.Retreat;
        } while (false);

        // 1列目の交代判断を行う。
        if (CanSwap12)
        {
            // 1列に体力の低い兵士がいる場合
            if (Row1.Any(s => s.IsAlive && s.Hp < 10))
            {
                // 2列に余裕があれば交代する。
                if (!Row2.Any(s => s.IsAlive && s.Hp < 10))
                {
                    return BattleAction.Swap12;
                }
            }
        }
        // 2列目の交代判断を行う。
        if (CanSwap23)
        {
            // 2列に体力の低い兵士がいる場合
            if (Row2.Any(s => s.IsAlive && s.Hp < 10))
            {
                // 3列に余裕があれば交代する。
                if (!Row3.Any(s => s.IsAlive && s.Hp < 10))
                {
                    return BattleAction.Swap23;
                }
            }
        }
        // 休息の判断を行う。
        if (CanRest)
        {
            // 2列以上体力が減っている兵士がいる場合
            var count = 0;
            if (Row1.Any(s => s.IsAlive && s.Hp < s.MaxHp)) count++;
            if (Row2.Any(s => s.IsAlive && s.Hp < s.MaxHp)) count++;
            if (Row3.Any(s => s.IsAlive && s.Hp < s.MaxHp)) count++;
            if (count > 1)
            {
                return BattleAction.Rest;
            }
        }

        // 続行する。
        return BattleAction.Attack;
    }

    /// <summary>
    /// 戦闘後の回復処理
    /// </summary>
    public static void Recover(Character chara, bool win, float winRate, float loseRate, float[] maxRecoveryCounts)
    {
        if (chara == null) return;

        var tiredAdj = Mathf.Pow(0.8f, chara.ConsecutiveBattleCount + 1);
        var intelliAdj = Mathf.Max(0, (chara.Intelligence - 80) / 100f / 2) * (win ? 1 : 0.5f);
        var winAdj = win ? winRate : loseRate;
        var adj = (winAdj + intelliAdj) * tiredAdj;
        for (int i = 0; i < chara.Soldiers.Count; i++)
        {
            var s = chara.Soldiers[i];
            if (!s.IsAlive) continue;
            var baseAmount = maxRecoveryCounts[i];
            var newHp = s.HpFloat + (baseAmount * adj);
            newHp = Mathf.Min(maxRecoveryCounts[i], newHp);
            s.HpFloat = Mathf.Min(s.MaxHp, newHp);
        }
        //Debug.Log($"{chara.Name} adj:{adj} win:{winAdj} intelli:{intelliAdj} tired: {tiredAdj} ({chara.ConsecutiveBattleCount})");
    }

    public override string ToString() => $"{Character?.Name}({Character?.Power})";
}
