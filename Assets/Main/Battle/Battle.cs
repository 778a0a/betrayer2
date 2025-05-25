using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using UnityEngine;
using Random = UnityEngine.Random;

public class Battle
{
    public CharacterInBattle Attacker { get; set; }
    public CharacterInBattle Defender { get; set; }
    public BattleType Type { get; set; }
    private int TickCount { get; set; }

    private CharacterInBattle Atk => Attacker;
    private CharacterInBattle Def => Defender;

    private BattleWindow UI => GameCore.Instance.MainUI.BattleWindow;
    public bool NeedInteraction => false; // Attacker.IsPlayer || Defender.IsPlayer;
    private bool NeedWatchBattle => false;
    private bool DebugWatch => false && Watch("カリオペ");
    private bool Watch(string name) =>
        Atk.Country.Ruler.Name == name || Def.Country.Ruler.Name == name ||
        Atk.Character.Name == name || Def.Character.Name == name;


    public Battle(CharacterInBattle atk, CharacterInBattle def, BattleType type)
    {
        Attacker = atk;
        Defender = def;
        Type = type;
    }

    public async ValueTask<BattleResult> Do()
    {
        Debug.Log($"[戦闘処理] {Atk}) -> {Def} ({Type}) 攻撃側地形: {Atk.Terrain} 防御側地形: {Def.Terrain}");

        if (NeedWatchBattle || NeedInteraction || DebugWatch)
        {
            UI.Root.style.display = DisplayStyle.Flex;
            UI.SetData(this);

            if (DebugWatch)
            {
                await UI.WaitPlayerClick();
            }
        }

        var result = default(BattleResult);
        while (!Atk.AllSoldiersDead && !Def.AllSoldiersDead)
        {
            // 撤退判断を行う。
            if (NeedWatchBattle || NeedInteraction || DebugWatch)
            {
                UI.SetData(this);
            }
            if (NeedInteraction)
            {
                var shouldContinue = await UI.WaitPlayerClick();
                if (!shouldContinue)
                {
                    result = Atk.IsPlayer ?
                        BattleResult.DefenderWin :
                        BattleResult.AttackerWin;
                    break;
                }
            }
            else if (NeedWatchBattle || DebugWatch)
            {
                await Awaitable.WaitForSecondsAsync(0.025f);
            }

            if (Atk.ShouldRetreat(TickCount, this))
            {
                result = BattleResult.DefenderWin;
                break;
            }
            if (Def.ShouldRetreat(TickCount, this))
            { 
                result = BattleResult.AttackerWin;
                break;
            }

            Tick();
        }

        if (result == BattleResult.None)
        {
            result = Atk.AllSoldiersDead ?
                BattleResult.DefenderWin :
                BattleResult.AttackerWin;
        }
        Debug.Log($"[戦闘処理] 結果: {result}");

        // 画面を更新する。
        if (NeedWatchBattle || NeedInteraction || DebugWatch)
        {
            UI.SetData(this, result);
            if (NeedInteraction)
            {
                await UI.WaitPlayerClick();
            }
            // 自分が君主で配下の戦闘の場合もボタンクリックを待つ。
            else if (Atk.Country.Ruler.IsPlayer || Def.Country.Ruler.IsPlayer)
            {
                await UI.WaitPlayerClick();
            }
            else
            {
                if (DebugWatch) await UI.WaitPlayerClick();
                else await Awaitable.WaitForSecondsAsync(0.45f);
            }
            UI.Root.style.display = DisplayStyle.None;
        }

        // 死んだ兵士のスロットを空にする。
        foreach (var sol in Atk.Soldiers.Concat(Def.Soldiers))
        {
            if (sol.Hp == 0)
            {
                sol.IsEmptySlot = true;
            }
        }

        var (winner, loser) = result == BattleResult.AttackerWin ?
            (Atk, Def) :
            (Def, Atk);

        // 兵士の回復処理を行う。
        CharacterInBattle.Recover(winner, true, 0.5f, 0.15f, winner.InitialSoldierCounts);
        CharacterInBattle.Recover(loser, false, 0.5f, 0.15f, loser.InitialSoldierCounts);
        winner.Character.ConsecutiveBattleCount++;
        loser.Character.ConsecutiveBattleCount++;

        // 回復処理確認用
        if (DebugWatch)
        {
            UI.Root.style.display = DisplayStyle.Flex;
            UI.SetData(this, result);
            await UI.WaitPlayerClick();
            UI.Root.style.display = DisplayStyle.None;
        }

        // 名声の処理を行う。
        var loserPrestigeLoss = loser.Character.Prestige / 3;
        loser.Character.Prestige -= loserPrestigeLoss;
        winner.Character.Prestige += loserPrestigeLoss;
        winner.Character.Prestige += 1;

        // 功績の処理を行う。
        winner.Character.Contribution += 10 * (winner.Character.Castle.Objective == CastleObjective.Attack ? 1.5f : 1f);
        loser.Character.Contribution += 1;

        // 攻城戦の場合は城の耐久力を減らす。
        var castle = Atk.Tile.Castle ?? Def.Tile.Castle;
        if (castle != null && Type == BattleType.Siege)
        {
            castle.Strength *= ((100 - TickCount / 2f * Random.Range(0.5f, 1f)) / 100f).MaxWith(0.99f);
            castle.Stability = (castle.Stability - TickCount / 4f * Random.Range(0.5f, 1f)).Clamp(0, 100);
            //Debug.LogError($"{TickCount}");
        }
        // 戦闘が起きた町の内政値を減らす。
        if (Atk.Tile.Town != null) DamegeTown(Atk.Tile.Town, Type, Atk);
        if (Def.Tile.Town != null) DamegeTown(Def.Tile.Town, Type, Def);
        static void DamegeTown(Town town, BattleType type, Character chara)
        {
            var hasCastle = town.Position == town.Castle.Position;
            // 城外戦の場合は城の中はダメージを受けない。
            if (hasCastle && type == BattleType.Field) return;

            var damageRange = town.Country == chara.Country ? (0.98f, 0.999f) : (0.95f, 0.98f);
            town.GoldIncome *= Random.Range(damageRange.Item1, damageRange.Item2);
        }

        return result;
    }

    public static float TerrainAdjustment(Terrain t) => t switch
    {
        Terrain.LargeRiver => -0.25f,
        Terrain.River => -0.15f,
        Terrain.Plain => 0.00f,
        Terrain.Hill => +0.10f,
        Terrain.Forest => +0.15f,
        Terrain.Mountain => +0.20f,
        _ => 0f,
    };
    public static float TraitsAdjustment(GameMapTile t, Traits traits)
    {
        var adj = 0f;
        if (traits.HasFlag(Traits.Merchant)) adj += -0.05f;
        if (traits.HasFlag(Traits.Knight)) adj += +0.025f;
        switch (t.Terrain)
        {
            case Terrain.LargeRiver:
                if (traits.HasFlag(Traits.Pirate)) adj += +0.50f;
                if (traits.HasFlag(Traits.Admiral)) adj += +0.30f;
                break;
            case Terrain.River:
                if (traits.HasFlag(Traits.Pirate)) adj += +0.40f;
                if (traits.HasFlag(Traits.Admiral)) adj += +0.20f;
                break;
            case Terrain.Plain:
                if (traits.HasFlag(Traits.Pirate)) adj += IsMarineSide(t) ? 0 : -0.15f;
                if (traits.HasFlag(Traits.Admiral)) adj += IsMarineSide(t) ? 0 : -0.05f;
                if (traits.HasFlag(Traits.Mountaineer)) adj += -0.10f;
                if (traits.HasFlag(Traits.Hunter)) adj += -0.05f;
                break;
            case Terrain.Hill:
                if (traits.HasFlag(Traits.Pirate)) adj += IsMarineSide(t) ? 0 : -0.15f;
                if (traits.HasFlag(Traits.Admiral)) adj += IsMarineSide(t) ? 0 : -0.05f;
                if (traits.HasFlag(Traits.Mountaineer)) adj += +0.10f;
                if (traits.HasFlag(Traits.Hunter)) adj += +0.025f;
                break;
            case Terrain.Forest:
                if (traits.HasFlag(Traits.Pirate)) adj += IsMarineSide(t) ? 0 : -0.15f;
                if (traits.HasFlag(Traits.Admiral)) adj += IsMarineSide(t) ? 0 : -0.05f;
                if (traits.HasFlag(Traits.Mountaineer)) adj += +0.05f;
                if (traits.HasFlag(Traits.Hunter)) adj += +0.10f;
                break;
            case Terrain.Mountain:
                if (traits.HasFlag(Traits.Pirate)) adj += IsMarineSide(t) ? 0 : -0.15f;
                if (traits.HasFlag(Traits.Admiral)) adj += IsMarineSide(t) ? 0 : -0.05f;
                if (traits.HasFlag(Traits.Mountaineer)) adj += +0.20f;
                if (traits.HasFlag(Traits.Hunter)) adj += +0.025f;
                break;
        }
        return adj;

        static bool IsMarineSide(GameMapTile tile)
        {
            return tile.Neighbors.Count(t => Util.IsMarine(t.Terrain)) >= 3;
        }
    }

    private void Tick()
    {
        TickCount += 1;

        // 両方の兵士をランダムな順番の配列にいれる。
        var all = Atk.Soldiers.Select(s => (soldier: s, owner: Attacker))
            .Concat(Def.Soldiers.Select(s => (soldier: s, owner: Defender)))
            .Where(x => x.soldier.IsAlive)
            .ToArray()
            .ShuffleInPlace();

        var baseAdjustment = new Dictionary<Character, (float, string)>
        {
            {Attacker, BaseAdjustment(Attacker, TickCount)},
            {Defender , BaseAdjustment(Defender, TickCount)},
        };
        static (float, string) BaseAdjustment(CharacterInBattle chara, int tickCount)
        {
            var sb = new StringBuilder();
            var op = chara.Opponent;
            var isMarine = Util.IsMarine(chara.Terrain) || Util.IsMarine(op.Terrain);
            var adj = 1f;
            adj += Tap("戦闘差", (chara.Strength - op.Strength) / 100f);
            adj += Tap("智謀差", (chara.Intelligence - op.Intelligence) / 100f * Mathf.Min(1, tickCount / 10f));
            adj += Tap("地形差", TerrainAdjustment(chara.Terrain) - TerrainAdjustment(op.Terrain));
            if (Util.IsMarine(chara.Character.Traits) && isMarine) adj += Tap("自特性", +(Mathf.Max(0, TraitsAdjustment(chara.Tile, chara.Character.Traits)) + Mathf.Max(0, TraitsAdjustment(op.Tile, chara.Character.Traits))));
            else adj += Tap("自特性", +(TraitsAdjustment(chara.Tile, chara.Character.Traits) + TraitsAdjustment(op.Tile, chara.Character.Traits)));
            if (Util.IsMarine(op.Character.Traits) && isMarine) adj += Tap("敵特性", -(Mathf.Max(0, TraitsAdjustment(op.Tile, op.Character.Traits)) + Mathf.Max(0, TraitsAdjustment(chara.Tile, op.Character.Traits))));
            else adj += Tap("敵特性", -(TraitsAdjustment(op.Tile, op.Character.Traits) + TraitsAdjustment(chara.Tile, op.Character.Traits)));
            if (chara.IsInCastle) adj += Tap("自城", +chara.Tile.Castle.Strength / 1000 / 2);
            if (op.IsInCastle) adj += Tap("敵城", -op.Tile.Castle.Strength / 1000 / 2);
            if (chara.IsInOwnTerritory) adj += Tap("自領1", +0.05f);
            if (op.IsInEnemyTerritory) adj += Tap("自領2", +0.05f);
            if (chara.IsInEnemyTerritory) adj += Tap("敵地1", -0.05f);
            if (op.IsInOwnTerritory) adj += Tap("敵地2", -0.05f);
            return (adj, sb.ToString());

            float Tap(string label, float v)
            {
                if (v != 0) sb.AppendFormat($"{label}:{v*100:＋00;－00} ");
                return v;
            }
        }
        Debug.Log($"[戦闘処理] 基本調整値: atk:{baseAdjustment[Attacker].Item1:0.00} def:{baseAdjustment[Defender].Item1:0.00}"
            + $"\n{baseAdjustment[Attacker].Item2}\n{baseAdjustment[Defender].Item2}");

        var attackerTotalDamage = 0f;
        var defenderTotalDamage = 0f;
        foreach (var (soldier, owner) in all)
        {
            var opponent = owner.Opponent;
            if (!soldier.IsAlive) continue;
            var target = opponent.Soldiers.Where(s => s.IsAlive).RandomPickDefault();
            if (target == null) continue;

            var adj = baseAdjustment[owner].Item1;
            adj += Random.Range(-0.2f, 0.2f);
            adj += soldier.Level / 10f;

            var damage = Math.Max(0, adj);
            target.HpFloat = (int)Math.Max(0, target.HpFloat - damage);
            soldier.AddExperience(owner.Character);

            if (owner.IsAttacker)
            {
                attackerTotalDamage += damage;
            }
            else
            {
                defenderTotalDamage += damage;
            }
        }

        if (Atk.IsPlayer || Def.IsPlayer)
        {
            Debug.Log($"[戦闘処理] " +
                $"{Atk}の総ダメージ: {attackerTotalDamage} " +
                $"{Def}の総ダメージ: {defenderTotalDamage}");
        }
    }
}
