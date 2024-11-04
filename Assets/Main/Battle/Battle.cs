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
    private bool NeedWatchBattle => true;

    public Battle(CharacterInBattle atk, CharacterInBattle def, BattleType type)
    {
        Attacker = atk;
        Defender = def;
        Type = type;
    }

    public async ValueTask<BattleResult> Do()
    {
        Debug.Log($"[戦闘処理] {Atk}) -> {Def} ({Type}) 攻撃側地形: {Atk.Terrain} 防御側地形: {Def.Terrain}");

        if (NeedWatchBattle || NeedInteraction)
        {
            UI.Root.style.display = DisplayStyle.Flex;
            UI.SetData(this);

            // デバッグ用
            await UI.WaitPlayerClick();
        }

        var result = default(BattleResult);
        while (!Atk.AllSoldiersDead && !Def.AllSoldiersDead)
        {
            // 撤退判断を行う。
            if (NeedWatchBattle || NeedInteraction)
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
            else if (NeedWatchBattle)
            {
                //await Awaitable.WaitForSecondsAsync(0.025f);
                await Awaitable.WaitForSecondsAsync(0.1f);
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
        if (NeedWatchBattle || NeedInteraction)
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
                //await Awaitable.WaitForSecondsAsync(0.45f);
                await UI.WaitPlayerClick();
            }
            //UI.Root.style.display = DisplayStyle.None;
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
        CharacterInBattle.Recover(winner, true, 0.6f, 0.2f, winner.InitialSoldierCounts);
        CharacterInBattle.Recover(loser, false, 0.6f, 0.2f, loser.InitialSoldierCounts);

        // 回復処理確認用
        {
            UI.SetData(this, result);
            await UI.WaitPlayerClick();
            UI.Root.style.display = DisplayStyle.None;
        }

        // 名声の処理を行う。
        var loserPrestigeLoss = loser.Character.Prestige / 3;
        loser.Character.Prestige -= loserPrestigeLoss;
        winner.Character.Prestige += loserPrestigeLoss;
        winner.Character.Prestige += 1;

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
    public static float TerrainTraitsAdjustment(Terrain t, Traits traits)
    {
        var adj = 1f;
        if (traits.HasFlag(Traits.Merchant)) adj += -0.05f;
        if (traits.HasFlag(Traits.Knight)) adj += +0.025f;
        switch (t)
        {
            case Terrain.LargeRiver:
                if (traits.HasFlag(Traits.Pirate)) adj += +0.25f;
                if (traits.HasFlag(Traits.Admiral)) adj += +0.10f;
                break;
            case Terrain.River:
                if (traits.HasFlag(Traits.Pirate)) adj += +0.15f;
                if (traits.HasFlag(Traits.Admiral)) adj += +0.10f;
                break;
            case Terrain.Plain:
                if (traits.HasFlag(Traits.Pirate)) adj += -0.10f;
                if (traits.HasFlag(Traits.Admiral)) adj += -0.05f;
                if (traits.HasFlag(Traits.Mountaineer)) adj += -0.10f;
                if (traits.HasFlag(Traits.Hunter)) adj += -0.05f;
                break;
            case Terrain.Hill:
                if (traits.HasFlag(Traits.Pirate)) adj += -0.15f;
                if (traits.HasFlag(Traits.Admiral)) adj += -0.05f;
                if (traits.HasFlag(Traits.Mountaineer)) adj += +0.10f;
                if (traits.HasFlag(Traits.Hunter)) adj += +0.025f;
                break;
            case Terrain.Forest:
                if (traits.HasFlag(Traits.Pirate)) adj += -0.15f;
                if (traits.HasFlag(Traits.Admiral)) adj += -0.05f;
                if (traits.HasFlag(Traits.Mountaineer)) adj += +0.05f;
                if (traits.HasFlag(Traits.Hunter)) adj += +0.10f;
                break;
            case Terrain.Mountain:
                if (traits.HasFlag(Traits.Pirate)) adj += -0.15f;
                if (traits.HasFlag(Traits.Admiral)) adj += -0.05f;
                if (traits.HasFlag(Traits.Mountaineer)) adj += +0.20f;
                if (traits.HasFlag(Traits.Hunter)) adj += +0.025f;
                break;
        }
        return adj;
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

        var baseAdjustment = new Dictionary<Character, float>
        {
            {Attacker, BaseAdjustment(Attacker, TickCount)},
            {Defender , BaseAdjustment(Defender, TickCount)},
        };
        static float BaseAdjustment(CharacterInBattle chara, int tickCount)
        {
            var op = chara.Opponent;
            var adj = 1f;
            adj += (chara.Strength - 50) / 100f;
            adj -= (op.Strength - 50) / 100f;
            adj += (chara.Character.Intelligence - 50) / 100f * Mathf.Min(1, tickCount / 10f);
            adj -= (op.Character.Intelligence - 50) / 100f * Mathf.Min(1, tickCount / 10f);
            adj += TerrainAdjustment(chara.Terrain);
            adj -= TerrainAdjustment(op.Terrain);
            adj += TerrainTraitsAdjustment(chara.Terrain, chara.Character.Traits);
            adj += TerrainTraitsAdjustment(op.Terrain, chara.Character.Traits);
            adj -= TerrainTraitsAdjustment(op.Terrain, op.Character.Traits);
            adj -= TerrainTraitsAdjustment(chara.Terrain, op.Character.Traits);
            if (op.IsInCastle) adj -= op.Tile.Castle.Strength / 1000;
            if (chara.IsInOwnTerritory) adj += 0.05f;
            if (op.IsInOwnTerritory) adj -= 0.05f;
            if (chara.IsInEnemyTerritory) adj -= 0.05f;
            if (op.IsInEnemyTerritory) adj += 0.05f;
            return adj;
        }
        Debug.Log($"[戦闘処理] 基本調整値: atk:{baseAdjustment[Attacker]:0.00} def:{baseAdjustment[Defender]:0.00}");

        var attackerTotalDamage = 0f;
        var defenderTotalDamage = 0f;
        foreach (var (soldier, owner) in all)
        {
            var opponent = owner.Opponent;
            if (!soldier.IsAlive) continue;
            var target = opponent.Soldiers.Where(s => s.IsAlive).RandomPickDefault();
            if (target == null) continue;

            var adj = baseAdjustment[owner];
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
