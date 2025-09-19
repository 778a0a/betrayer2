using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

partial class AI
{
    public async Task DoStrategyAction(Character chara)
    {
        var country = chara.Country;
        var castle = chara.Castle;

        // 君主の場合
        if (chara.IsRuler)
        {
            // 四半期ごとの行動がまだなら行う。
            if (!country.QuarterActionDone)
            {
                country.QuarterActionDone = true;
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
            await Diplomacy(country);

            await BonusFromRuler(country);

            // TODO 人員の移動
        }

        // 四半期ごとの行動がまだなら行う。
        if (!castle.QuarterActionDone)
        {
            castle.QuarterActionDone = true;

            // 褒賞を与える。
            await Bonus(castle);

            // 物資を輸送する。
            if (chara.IsRuler)
            {
                await TransportAsDistribution(chara.Country);
            }
            else
            {
                await TransportAsTribute(castle, chara);
            }

            // 採用を行う。
            await HireVassal(chara);

            // 投資を行う。
            await Invest(castle);
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
        await Deploy(castle);
    }

    private async ValueTask Old()
    {
        // 危険軍勢の対処を行う。
        if (core.GameDate.Day % 5 == 0)
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
}
