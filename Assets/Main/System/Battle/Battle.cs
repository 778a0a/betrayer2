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
    
    public CharacterInBattle Player =>
        Attacker.IsPlayer ? Attacker :
        Defender.IsPlayer ? Defender :
        null;

    private BattleWindow UI => GameCore.Instance.MainUI.BattleWindow;
    public bool NeedInteraction => Attacker.IsPlayer || Defender.IsPlayer;
    private bool NeedWatchBattle => false;
    private bool DebugWatch => Watch("アーサー");
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
        //Debug.Log($"[戦闘処理] {Atk}) -> {Def} ({Type}) 攻撃側地形: {Atk.Terrain} 防御側地形: {Def.Terrain}");

        // ゲージをセットする。
        Atk.TacticsGauge = Random.Range(0, 100);
        Def.TacticsGauge = Random.Range(0, 100);
        Atk.RetreatGauge = Random.Range(0, 100);
        Def.RetreatGauge = Random.Range(0, 100);

        // 戦闘開始前
        if (NeedWatchBattle || NeedInteraction || DebugWatch)
        {
            UI.Root.style.display = DisplayStyle.Flex;
            UI.SetData(this);

            if (DebugWatch)
            {
                await UI.WaitPlayerClick();
            }
        }

        // 戦闘ループ
        var result = default(BattleResult);
        while (!Atk.AllSoldiersDead && !Def.AllSoldiersDead)
        {
            // 全滅している列を詰める。
            Atk.CompactSoldierRows();
            Def.CompactSoldierRows();

            // 戦術行動を選択して実行する。
            result = await DoTacticAction();
            if (result != BattleResult.None)
            {
                break;
            }

            // 戦闘を実行する。
            await Tick();

            // ゲージを増加させる。
            Atk.TacticsGauge = (Atk.TacticsGauge + (Atk.Strength + Atk.Intelligence * 2) * 0.085f).MaxWith(100);
            Def.TacticsGauge = (Def.TacticsGauge + (Def.Strength + Def.Intelligence * 2) * 0.085f).MaxWith(100);
            Atk.RetreatGauge = (Atk.RetreatGauge + (Atk.Intelligence * 3 - Def.Intelligence) * 0.09f).Clamp(0, 100);
            Def.RetreatGauge = (Def.RetreatGauge + (Def.Intelligence * 3 - Atk.Intelligence) * 0.09f).Clamp(0, 100);
            // 2列目、3列目を少し回復させる。
            foreach (var sol in Atk.Row2.Concat(Def.Row2).Where(s => s.IsAlive))
            {
                sol.HpFloat = (sol.HpFloat + Random.value * 0.5f).MaxWith(sol.MaxHp);
            }
            foreach (var sol in Atk.Row3.Concat(Def.Row3).Where(s => s.IsAlive))
            {
                sol.HpFloat = (sol.HpFloat + Random.value * 0.9f).MaxWith(sol.MaxHp);
            }
        }

        // 戦闘終了
        // 全滅で終了した場合
        if (result == BattleResult.None)
        {
            result = Atk.AllSoldiersDead ?
                BattleResult.DefenderWin :
                BattleResult.AttackerWin;
        }
        //Debug.Log($"[戦闘処理] {Atk}) -> {Def} ({Type}) 攻撃側地形: {Atk.Terrain} 防御側地形: {Def.Terrain} 結果: {result}");

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

        var (winner, loser) = result == BattleResult.AttackerWin ?
            (Atk, Def) :
            (Def, Atk);

        // 兵士の回復処理を行う。
        CharacterInBattle.Recover(winner, true, winner.InitialSoldierCounts);
        CharacterInBattle.Recover(loser, false, loser.InitialSoldierCounts);

        // 回復処理確認用
        if (true || DebugWatch)
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
        winner.Character.Contribution += 5;
        loser.Character.Contribution += 1;

        // 攻城戦の場合は城の耐久力を減らす。
        var castle = Atk.Tile.Castle ?? Def.Tile.Castle;
        if (castle != null && Type == BattleType.Siege)
        {
            castle.Strength *= ((100 - TickCount / 2f * Random.Range(0.5f, 1f)) / 100f).MaxWith(0.99f);
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

            var damageRange = town.Country == chara.Country ? (0.98f, 0.999f) : (0.97f, 0.985f);
            var damage = Random.Range(damageRange.Item1, damageRange.Item2);
            town.GoldIncome *= damage;
            //town.TotalInvestment *= damage;
        }

        return result;
    }


    private async ValueTask<BattleResult> DoTacticAction()
    {
        if (NeedWatchBattle || NeedInteraction || DebugWatch)
        {
            UI.SetData(this);
        }

        var atkAction = await SelectActionIndiv(Atk);
        var defAction = await SelectActionIndiv(Def);
        UI.DisableButtons();

        if (atkAction == BattleAction.Retreat)
        {
            return BattleResult.DefenderWin;
        }
        if (defAction == BattleAction.Retreat)
        {
            return BattleResult.AttackerWin;
        }
        DoActionIndiv(Atk, atkAction);
        DoActionIndiv(Def, defAction);
        if (NeedWatchBattle || NeedInteraction || DebugWatch)
        {
            var needWait = atkAction != BattleAction.Attack || defAction != BattleAction.Attack;
            if (needWait)
            {
                UI.SetData(this);
                var waitTime = atkAction == BattleAction.Rest || defAction == BattleAction.Rest ? 0.8f : 0.3f;
                await Awaitable.WaitForSecondsAsync(waitTime);
            }
        }

        return BattleResult.None;
    }

    private async ValueTask<BattleAction> SelectActionIndiv(CharacterInBattle chara)
    {
        if (chara.IsPlayer)
        {
            return await UI.WaitPlayerClick();
        }
        return  chara.SelectAction(TickCount, this);
    }

    private void DoActionIndiv(CharacterInBattle chara, BattleAction action)
    {
        switch (action)
        {
            case BattleAction.Swap12:
                chara.Swap12();
                break;
            case BattleAction.Swap23:
                chara.Swap23();
                break;
            case BattleAction.Rest:
                chara.Rest();
                break;
            default:
                break;
        }
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
            return tile.Neighbors.Count(t => Util.IsMarine(t.Terrain)) >= 2;
        }
    }

    private async ValueTask Tick()
    {
        TickCount += 1;

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
            adj += Tap("智謀差", (chara.Intelligence - op.Intelligence) / 100f * (tickCount / 5f).MaxWith(2));
            adj += Tap("地形差", TerrainAdjustment(chara.Terrain) - TerrainAdjustment(op.Terrain));
            if (Util.IsMarine(chara.Character.Traits) && isMarine) adj += Tap("自特性", +(Mathf.Max(0, TraitsAdjustment(chara.Tile, chara.Character.Traits)) + Mathf.Max(0, TraitsAdjustment(op.Tile, chara.Character.Traits))));
            else adj += Tap("自特性", +(TraitsAdjustment(chara.Tile, chara.Character.Traits) + TraitsAdjustment(op.Tile, chara.Character.Traits)));
            if (Util.IsMarine(op.Character.Traits) && isMarine) adj += Tap("敵特性", -(Mathf.Max(0, TraitsAdjustment(op.Tile, op.Character.Traits)) + Mathf.Max(0, TraitsAdjustment(chara.Tile, op.Character.Traits))));
            else adj += Tap("敵特性", -(TraitsAdjustment(op.Tile, op.Character.Traits) + TraitsAdjustment(chara.Tile, op.Character.Traits)));
            if (chara.IsInCastle) adj += Tap("自城", +chara.Tile.Castle.Strength / 100 * 0.7f);
            if (op.IsInCastle) adj += Tap("敵城", -op.Tile.Castle.Strength / 100 * 0.7f);
            if (chara.IsInOwnTerritory) adj += Tap("自領1", +0.05f);
            if (op.IsInEnemyTerritory) adj += Tap("自領2", +0.05f);
            if (chara.IsInEnemyTerritory) adj += Tap("敵地1", -0.05f);
            if (op.IsInOwnTerritory) adj += Tap("敵地2", -0.05f);
            if (chara.IsRemote) adj += Tap("遠征1", -0.20f);
            if (op.IsRemote) adj += Tap("遠征2", +0.20f);
            return (adj, sb.ToString());

            float Tap(string label, float v)
            {
                if (v != 0) sb.AppendFormat($"{label}:{v*100:＋00;－00} ");
                return v;
            }
        }
        if (NeedWatchBattle || NeedInteraction || DebugWatch)
        {
            Debug.Log($"[戦闘処理] 基本調整値: atk:{baseAdjustment[Attacker].Item1:0.00} def:{baseAdjustment[Defender].Item1:0.00}"
                + $"\n{baseAdjustment[Attacker].Item2}\n{baseAdjustment[Defender].Item2}");
        }

        // 攻撃回数
        var attackCount = Random.Range(5, 10);
        // 両方の1列目の兵士
        var all = Atk.Row1.Select(s => (soldier: s, owner: Attacker))
            .Concat(Def.Row1.Select(s => (soldier: s, owner: Defender)))
            .Where(x => x.soldier.IsAlive)
            .ToArray();

        for (int i = 0; i < attackCount; i++)
        {
            // 攻撃順番をランダムにする。
            all.ShuffleInPlace();

            var attackerTotalDamage = 0f;
            var defenderTotalDamage = 0f;
            foreach (var (soldier, owner) in all)
            {
                var opponent = owner.Opponent;
                if (!soldier.IsAlive) continue;
                var target = opponent.Row1.Where(s => s.IsAlive).RandomPickDefault();
                if (target == null) break;

                var adj = baseAdjustment[owner].Item1;
                adj += Random.Range(-0.2f, 0.2f);
                adj += soldier.Level / 10f;

                var damage = Math.Max(0, adj);
                target.HpFloat = (target.HpFloat - damage).MinWith(0);
                // 力尽きた場合
                if (target.HpFloat == 0)
                {
                    // 45%の確率で死亡扱いにする。
                    target.IsDeadInBattle = 0.45f.Chance();
                }

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

            if (NeedWatchBattle || NeedInteraction || DebugWatch)
            {
                UI.SetData(this);
                await Awaitable.WaitForSecondsAsync(0.15f);
            }

            if (Atk.Row1.All(s => !s.IsAlive) || Def.Row1.All(s => !s.IsAlive))
            {
                break;
            }
        }
    }
}
