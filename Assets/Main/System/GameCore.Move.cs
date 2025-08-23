using System;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using System.Linq;
using System.Buffers;

partial class GameCore
{
    /// <summary>
    /// キャラクターの行動を行う。
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private async ValueTask OnCharacterMove(Character player)
    {
        // 戦略フェイズの処理を行う。
        var bosses = World.Castles
            .Select(c => c.Boss)
            .Where(b => b != null && b.StrategyActionGauge >= 100)
            .Shuffle();
        foreach (var boss in bosses)
        {
            boss.StrategyActionGauge = 0;
            await DoStrategyAction(boss);
        }

        // 個人フェイズの処理を行う。
        foreach (var chara in World.Characters.Where(c => c.PersonalActionGauge >= 100).Shuffle())
        {
            chara.PersonalActionGauge = 0;
            await DoPersonalAction(chara);
        }
    }

    /// <summary>
    /// 戦略行動を行う。
    /// </summary>
    private async ValueTask DoStrategyAction(Character chara)
    {
        // TODO プレーヤーの場合
        if (chara.IsPlayer)
        {
            Booter.hold = true;
            MainUI.StrategyPhaseScreen.Show(chara);
            await Booter.HoldIfNeeded();
            return;
        }

        var country = chara.Country;
        var castle = chara.Castle;

        // 君主の場合
        if (chara.IsRuler)
        {
            // 四半期ごとの行動がまだなら行う。
            if (!country.QuarterActionDone)
            {
                country.QuarterActionDone = true;

                foreach (var rulingCastle in country.Castles)
                {
                    // 各城の方針を設定する。
                    var prevObjective = rulingCastle.Objective;
                    rulingCastle.Objective = AI.SelectCastleObjective(chara, rulingCastle);
                    if (prevObjective != rulingCastle.Objective)
                    {
                        Debug.Log($"方針: 更新 {rulingCastle.Objective} <- {prevObjective} at {rulingCastle}");
                    }
                    else
                    {
                        Debug.Log($"方針継続: {rulingCastle.Objective} at {rulingCastle}");
                    }
                }
            }

            // 赤字で物資も乏しい場合は序列の低いメンバーを解雇する。
            if (country.GoldBalance < -30 && country.GoldSurplus < 0)
            {
                var target = country.Members
                    .Where(m => !m.IsMoving)
                    .Where(m => !m.IsImportant)
                    .OrderByDescending(m => m.OrderIndex)
                    .FirstOrDefault();
                if (target != null)
                {
                    var act = StrategyActions.FireVassal;
                    var args = act.Args(chara, target);
                    Debug.LogError($"{country} 赤字のため、{target}を解雇します。");
                    if (act.CanDo(args))
                    {
                        await StrategyActions.FireVassal.Do(args);
                    }
                    else
                    {
                        Debug.LogWarning($"{country} 赤字のため、{target}を解雇しようとしましたが実行不可でした。");
                    }
                }
            }

            // 外交を行う。
            await AI.Diplomacy(country);

            await AI.BonusFromRuler(country);

            // TODO 人員の移動
        }

        // 四半期ごとの行動がまだなら行う。
        if (!castle.QuarterActionDone)
        {
            castle.QuarterActionDone = true;

            // 褒賞を与える。
            await AI.Bonus(castle);

            // 物資を輸送する。
            if (chara.IsRuler)
            {
                await AI.TransportAsDistribution(chara.Country);
            }
            else
            {
                await AI.TransportAsTribute(castle, chara);
            }

            // 採用を行う。
            await AI.HireVassal(castle);

            // 投資を行う。
            await AI.Invest(castle);
        }

        // 防衛
        if (castle.DangerForcesExists)
        {
            var dangers = castle.DangerForces(World.Forces).ToArray();
            var dangerPower = dangers.Sum(f => f.Character.Power);
            var defPower = castle.DefenceAndReinforcementPower(World.Forces);
            // 防衛兵力が少ないなら退却させる。
            if (dangerPower > defPower)
            {
                // 出撃中の軍勢について
                var castleForces = castle.Members
                    .Where(m => m.IsMoving)
                    .Select(m => m.Force)
                    .Where(f => f.Destination.Position != castle.Position)
                    .ShuffleAsArray();
                foreach (var myForce in castleForces)
                {
                    if (dangerPower < defPower)
                    {
                        Debug.Log($"防衛戦力が十分なため退却しません。{myForce}");
                        continue;
                    }
                    if (myForce.Position == castle.Position)
                    {
                        World.Forces.Unregister(myForce);
                        // TODO 帰還アクションを使う。
                    }
                    else
                    {
                        myForce.SetDestination(myForce.Character.Castle);
                        Debug.LogWarning($"危険軍勢がいるため退却します。{myForce}");
                        // TODO 帰還アクションを使う。
                    }
                    defPower += myForce.Character.Power;
                }
            }
        }

        // 進軍を行うか判定する。
        await AI.Deploy(castle);
    }

    private async ValueTask DoPersonalAction(Character chara)
    {
        // TODO プレーヤーの場合
        if (chara.IsPlayer)
        {
            Booter.hold = true;
            MainUI.PersonalPhaseScreen.Show(chara);
            await Booter.HoldIfNeeded();
            return;
        }

        // 未所属の場合
        if (chara.IsFree)
        {
            // ランダムに拠点を移動する。
            if (0.2f.Chance())
            {
                var oldCastle = chara.Castle;
                var newCastle = oldCastle.Neighbors.RandomPick();
                chara.ChangeCastle(newCastle, true);
                //Debug.Log($"{chara.Name}が{oldCastle}から{newCastle}に移動しました。");
                // TODO 移動アクションを使う。
            }

            // TODO 奪取を行う。
            // TODO 仕官を行う。

            // 所持金が無くなるまで雇兵・訓練を行う。
            var args = new ActionArgs() { actor = chara };
            while (true)
            {
                var action = (ActionBase)(chara.Soldiers.HasEmptySlot ?
                    PersonalActions.HireSoldier :
                    PersonalActions.TrainSoldiers);
                if (!action.CanDo(args)) break;
                await action.Do(args);
            }
        }
        // 所属ありの場合
        else
        {
            // TODO 反乱を起こすか判定する。
            // TODO 出撃中の場合
            if (chara.IsMoving)
            {
                var force = chara.Force;

                // 救援モードの場合
                if (force.Mode == ForceMode.Reinforcement)
                {
                    // 救援が完了した場合は帰還する。
                    if (force.Destination.Position == force.Position &&
                        force.ReinforcementWaitDays <= 0)
                    {
                        // TODO 帰還アクションを使う。
                        force.SetDestination(force.Character.Castle);
                        Debug.Log($"軍勢 救援が完了したため帰還します。 {force}");
                        return;
                    }

                    // 救援先が危険でなくなったら本拠地に戻る。
                    var home = chara.Castle;
                    var targetCastle = (Castle)force.Destination;
                    if (!targetCastle.DangerForcesExists && targetCastle != home)
                    {
                        // TODO 帰還アクションを使う。
                        force.SetDestination(home);
                        if (force.Position == home.Position)
                        {
                            World.Forces.Unregister(force);
                        }
                        Debug.Log($"軍勢 救援先が危険でなくなったため帰還します。 {force}");
                        return;
                    }
                }

                return;
            }

            // 後方から移動する（適当）TODO
            var castle = chara.Castle;
            var isSafe = castle.Neighbors.All(n => !castle.IsAttackable(n)) && !castle.DangerForcesExists;
            if (isSafe && castle.Members.Count > 2)
            {
                var cands = castle.Members
                    .Where(m => m != chara)
                    .Where(m => m.IsDefendable)
                    .ToList();
                var moveTarget = cands.RandomPickDefault();
                var moveCastle = castle.Neighbors.Where(n => castle.IsSelf(n)).RandomPickDefault();
                if (moveTarget != null && moveCastle != null && 0.5f.Chance())
                {
                    var move = StrategyActions.Deploy;
                    var moveArgs = move.Args(chara, moveTarget, moveCastle);
                    await move.Do(moveArgs);
                }
            }

            // 所持金の半分までを予算とする。
            var budget = chara.Gold / 2;

            var args = new ActionArgs();
            args.actor = chara;
            args.targetCastle = chara.Castle;

            var action = default(ActionBase);
            // 空きスロットがあれば雇兵する。
            if (chara.Soldiers.HasEmptySlot)
            {
                action = PersonalActions.HireSoldier;
                budget = chara.Gold;
            }
            // 基本的には方針通りの行動を行う。
            else if ((chara.Fealty.MinWith(chara.Ambition) / 10f - 0.1f).Chance())
            {
                switch (chara.Castle.Objective)
                {
                    case CastleObjective.Fortify:
                        action = PersonalActions.Fortify;
                        break;
                    case CastleObjective.Develop:
                        var investChance = Mathf.Pow(chara.Castle.GoldIncomeProgress, 2);
                        action = investChance.Chance() ? PersonalActions.Invest : PersonalActions.Develop;
                        break;
                    case CastleObjective.Train:
                        action = PersonalActions.TrainSoldiers;
                        break;
                    case CastleObjective.None:
                    case CastleObjective.Attack:
                    default:
                        break;
                }
            }
            // アクション未選択か、選択したアクションが実行不可ならランダムに行動する。
            if (!action?.CanDo(args) ?? true)
            {
                action ??= vassalActions.Value.Where(a => a.CanDo(args)).RandomPickDefault();
            }
            // できることがないなら何もしない。
            if (action == null)
            {
                //Debug.LogWarning($"{chara.Name} はできることがありません。");
                return;
            }

            // 予算到達までアクションを実行する。
            while (budget > 0)
            {
                if (!action.CanDo(args)) break;
                budget -= action.Cost(args).actorGold;
                await action.Do(args);
            }
        }
    }

    private async ValueTask Old()
    {
        // 危険軍勢の対処を行う。
        if (GameDate.Day % 5 == 0)
        {
            foreach (var castle in World.Castles.Where(c => c.DangerForcesExists))
            {
                var dangerForces = castle.DangerForces(World.Forces).ToList();
                if (dangerForces.Count == 0)
                {
                    castle.DangerForcesExists = false;
                    continue;
                }
                var dangerPower = dangerForces.Sum(f => f.Character.Power);
                var defPower = castle.DefenceAndReinforcementPower(World.Forces);
                // 防衛兵力が十分なら何もしない。
                if (dangerPower < defPower) continue;

                void ProcessRainforcements(Country reinSourceCountry)
                {
                    var isAlly = castle.Country != reinSourceCountry;
                    // 隣接する城が危険でなければ、援軍を送る。
                    // 救援出撃して戦闘せず帰還している軍勢があれば優先して向かわせる。
                    var candForces = castle.Neighbors
                        .Where(n => n.Country == reinSourceCountry)
                        .Where(n => !n.DangerForcesExists)
                        .SelectMany(n => n.Members)
                        // 帰還中
                        .Where(m =>
                            m.IsMoving &&
                            m.Force.Mode == ForceMode.Reinforcement &&
                            m.Force.Destination.Position == m.Castle.Position)
                        // 損耗が少ない
                        .Where(m => m.Soldiers.AttritionRate < 0.5f)
                        // 救援先の近くにいる。
                        .Where(m =>
                            World.Forces.ETADays(m, m.Force.Position, castle, ForceMode.Reinforcement) <
                            World.Forces.ETADays(m, m.Castle.Position, castle, ForceMode.Reinforcement))
                        .ToList();
                    foreach (var f in candForces)
                    {
                        Debug.LogError($"救援帰還中の{f.Name}が{castle}へ援軍として転向します。{(isAlly ? "(同盟国)" : "")}");
                        f.Force.ReinforcementOriginalTarget = castle;
                        f.Force.SetDestination(castle);
                        f.Force.ReinforcementWaitDays = 90;
                        defPower += f.Power;
                    }
                }

                async Task ProcessDefendables(Country reinSourceCountry)
                {
                    var isAlly = castle.Country != reinSourceCountry;
                    // 城内にいるキャラ
                    var cands = castle.Neighbors
                        .Where(n => n.Country == reinSourceCountry)
                        .Where(n => !n.DangerForcesExists)
                        .Where(n => n.Members.Count(m => m.IsDefendable) > 1)
                        .SelectMany(n => n.Members)
                        .Where(m => m.IsDefendable)
                        .Select(m => (chara: m, eta: World.Forces.ETADays(m, m.Castle.Position, castle, ForceMode.Reinforcement)))
                        .ToList();
                    // 援軍候補がない場合は何もしない。
                    if (cands.Count == 0) return;

                    var maxETA = cands.Select(x => x.eta).Max();
                    //Debug.LogWarning($"cands:\n{string.Join("\n", cands)}");
                    while (dangerPower > defPower && cands.Count > 0)
                    {
                        var target = cands.RandomPickWeighted(x => Mathf.Pow(10 + maxETA - x.eta, 2), true);
                        var (member, eta) = target;
                        var defendables = member.Castle.Members.Count(m => m.IsDefendable);
                        if (defendables <= 1)
                        {
                            cands.Remove(target);
                            continue;
                        }
                        var action = StrategyActions.DeployAsReinforcement;
                        var args = action.Args(reinSourceCountry.Ruler, member, castle);
                        if (action.CanDo(args))
                        {
                            await action.Do(args);
                            defPower += member.Power;
                            Debug.LogWarning($"{member.Name}が{castle}へ援軍として出撃しました。{(isAlly ? "(同盟国)" : "")}");
                        }
                        cands.Remove(target);
                    }
                }

                // 救援から帰還中の軍勢
                ProcessRainforcements(castle.Country);
                if (dangerPower < defPower) continue;
                // 駐在中のキャラ
                await ProcessDefendables(castle.Country);
                if (dangerPower < defPower) continue;

                // まだ足りない場合は同盟国から援軍を出す。
                var allyCountries = castle.Country.Neighbors
                    .Where(n => n.IsAlly(castle.Country))
                    .Distinct()
                    // ただし危険軍勢がすべて同盟国の同盟国なら出さない。
                    .Where(n => !dangerForces.All(d => n.IsAlly(d.Country)))
                    .ToList();
                foreach (var ally in allyCountries)
                {
                    if (dangerPower < defPower) break;
                    ProcessRainforcements(ally);
                }
                foreach (var ally in allyCountries)
                {
                    if (dangerPower < defPower) break;
                    await ProcessDefendables(ally);
                }
            }
        }
    }

    public void Pause()
    {
        Booter.hold = true;
    }

    private readonly Lazy<ActionBase[]> vassalActions = new(() => new ActionBase[]
    {
        Instance.PersonalActions.Develop,
        Instance.PersonalActions.Fortify,
        Instance.PersonalActions.TrainSoldiers,
        Instance.PersonalActions.Invest,
    });
}