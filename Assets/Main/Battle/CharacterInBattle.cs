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
    /// 戦闘前の兵士数
    /// </summary>
    public float[] InitialSoldierCounts = Character.Soldiers.Select(s => s.HpFloat).ToArray();

    /// <summary>
    /// 戦闘に利用する戦闘能力値
    /// </summary>
    public int Strength => IsInOwnTerritory ? Character.Defense : Character.Attack;
    public int Intelligence => Character.Intelligence;

    public Soldiers Soldiers = Character.Soldiers;
    public bool IsPlayer = Character.IsPlayer;
    public bool AllSoldiersDead => Soldiers.All(s => !s.IsAlive);

    public static implicit operator Character(CharacterInBattle c) => c.Character;

    public bool ShouldRetreat(int tickCount, Battle battle)
    {
        // プレーヤーの場合はUIで判断しているので処理不要。
        if (IsPlayer) return false;

        // TODO
        //// 私闘の場合は撤退しない。
        //if (battle.Type is MartialActions.PrivateFightAction) return false;

        // 戦闘開始直後は撤退しない。
        if (tickCount < 3) return false;
        // 敵より智謀が低いなら追加で撤退不可にしてみる。
        // TODO プレーヤーの場合も同様に制限する。
        if (Opponent.Character.Intelligence > Character.Intelligence)
        {
            var limit = 5;
            limit += (Opponent.Character.Intelligence - Character.Intelligence) / 10;
            if (tickCount < limit) return false;
        }

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

        if (IsInCastle && IsDefender)
        {
            // 君主か忠誠100の家臣で自国の最後の領土の防衛なら撤退しない。
            var lastCastle = Country.Castles.Count == 1;
            var loyal = Character == Country.Ruler || true; // TODO
            if (lastCastle && loyal) return false;
        }

        // 撤退する。
        return true;
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
        Debug.Log($"{chara.Name} adj:{adj} win:{winAdj} intelli:{intelliAdj} tired: {tiredAdj} ({chara.ConsecutiveBattleCount})");
    }

    public override string ToString() => $"{Character?.Name}({Character?.Power})";
}
