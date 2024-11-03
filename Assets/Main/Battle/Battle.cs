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
    public ActionBase Type { get; set; }
    private int TickCount { get; set; }

    private CharacterInBattle Atk => Attacker;
    private CharacterInBattle Def => Defender;

    private BattleWindow UI => GameCore.Instance.MainUI.BattleWindow;
    public bool NeedInteraction => Attacker.IsPlayer || Defender.IsPlayer;
    private bool NeedWatchBattle => Test.Instance.showOthersBattle;

    public Battle(CharacterInBattle atk, CharacterInBattle def, ActionBase type)
    {
        Attacker = atk;
        Defender = def;
        Type = type;
    }

    public async ValueTask<BattleResult> Do()
    {
        Debug.Log($"[戦闘処理] {Atk}) -> {Def} at {Atk.Area.Position} -> {Def.Area.Position}");
        Debug.Log($"[戦闘処理] 攻撃側地形: {Atk.Terrain} 防御側地形: {Def.Terrain}");
        if (Def.Character == null)
        {
            Debug.Log($"[戦闘処理] 防御側がいないので侵攻側の勝利です。");
            Atk.Character.Prestige += 1;
            return BattleResult.AttackerWin;
        }

        if (NeedWatchBattle || NeedInteraction)
        {
            UI.Root.style.display = DisplayStyle.Flex;
            UI.SetData(this);
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
                await Awaitable.WaitForSecondsAsync(0.45f);
            }
            UI.Root.style.display = DisplayStyle.None;
        }

        // 死んだ兵士のスロットを空にする。
        foreach (var sol in Atk.Force.Soldiers.Concat(Def.Force.Soldiers))
        {
            if (sol.Hp == 0)
            {
                sol.IsEmptySlot = true;
            }
        }

        var winner = result == BattleResult.AttackerWin ? Atk : Def;
        var loser = result == BattleResult.AttackerWin ? Def : Atk;

        // 兵士の回復処理を行う。
        CharacterInBattle.Recover(winner, true, 0.1f, 0.05f);
        CharacterInBattle.Recover(loser, false, 0.1f, 0.05f);

        // 名声の処理を行う。
        var loserPrestigeLoss = loser.Character.Prestige / 3;
        loser.Character.Prestige -= loserPrestigeLoss;
        winner.Character.Prestige += loserPrestigeLoss;
        winner.Character.Prestige += 1;

        return result;
    }

    public static float TerrainDamageAdjustment(Terrain t) => t switch
    {
        Terrain.LargeRiver => 0.25f,
        Terrain.River => 0.15f,
        Terrain.Plain => 0.00f,
        Terrain.Hill => -0.10f,
        Terrain.Forest => -0.15f,
        Terrain.Mountain => -0.20f,
        Terrain.Fort => -0.25f,
        _ => 0f,
    };

    private void Tick()
    {
        TickCount += 1;

        // 両方の兵士をランダムな順番の配列にいれる。
        var all = Atk.Force.Soldiers.Select(s => (soldier: s, owner: Attacker))
            .Concat(Def.Force.Soldiers.Select(s => (soldier: s, owner: Defender)))
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
            adj -= TerrainDamageAdjustment(chara.Terrain);
            adj += TerrainDamageAdjustment(op.Terrain);
            return adj;
        }
        Debug.Log($"[戦闘処理] 基本調整値: atk:{baseAdjustment[Attacker]:0.00} def:{baseAdjustment[Defender]:0.00}");

        var attackerTotalDamage = 0f;
        var defenderTotalDamage = 0f;
        foreach (var (soldier, owner) in all)
        {
            var opponent = owner.Opponent;
            if (!soldier.IsAlive) continue;
            var target = opponent.Force.Soldiers.Where(s => s.IsAlive).RandomPickDefault();
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
