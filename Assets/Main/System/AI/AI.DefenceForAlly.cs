using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class AI
{
    /// <summary>
    /// 同盟国への救援
    /// </summary>
    public async ValueTask DefenceForAlly(Castle castle)
    {
        // 危険軍勢がいるなら何もしない。
        if (castle.DangerForcesExists) return;
        if (castle.Country.Ruler.IsPlayer && castle.DeployPolicy == CastleDeployPolicy.Prohibited) return;

        // 危険軍勢が存在する近隣の同盟国の城を探す。
        var dangerAllyCastles = castle.Neighbors
            .Where(n => n.Country != castle.Country && n.Country.IsAlly(castle.Country))
            .Where(n => n.DangerForcesExists)
            .Select(n => (castle: n, dangers: n.DangerForces(World.Forces).ToArray()))
            // 危険軍勢が全て自国の同盟国の場合は除外する。
            .Where(x => x.dangers.Any(d => !d.IsAlly(castle)))
            // 危険な城を優先する。
            .OrderByDescending(x => x.dangers.Sum(d => d.Character.Power) - x.castle.DefenceAndReinforcementPower(World.Forces))
            .ToList();
        if (dangerAllyCastles.Count == 0) return;

        Debug.LogError($"[救援処理] {castle}");

        foreach (var (allyCastle, dangers) in dangerAllyCastles)
        {
            var dangerPower = dangers.Sum(f => f.Character.Power);
            var defPower = allyCastle.DefenceAndReinforcementPower(World.Forces);

            // まず、危険城の近くにいる帰還中の軍勢を向かわせる。
            var candForces = castle.Members
                // 帰還中
                .Where(m =>
                    m.IsMoving &&
                    !m.Force.IsPlayerDirected &&
                    m.Force.Mode == ForceMode.Reinforcement &&
                    m.Force.Destination.Position == m.Castle.Position)
                // 損耗が少ない
                .Where(m => m.Soldiers.All(s => s.Hp > 25))
                // 救援先の近くにいる。
                .Where(m =>
                    World.Forces.ETADays(m, m.Force.Position, allyCastle, ForceMode.Reinforcement) <
                    World.Forces.ETADays(m, m.Castle.Position, allyCastle, ForceMode.Reinforcement))
                .ToList();
            foreach (var f in candForces)
            {
                Debug.LogError($"救援帰還中の{f.Name}が{castle}へ援軍として転向します。");
                f.Force.ReinforcementOriginalTarget = castle;
                f.Force.SetDestination(castle);
                f.Force.ReinforcementWaitDays = 90;
                defPower += f.Power;
                if (defPower >= dangerPower)
                {
                    Debug.Log($"救援兵力が十分なためこれ以上転向しません。");
                    break;
                }
            }

            if (defPower >= dangerPower)
            {
                Debug.Log($"救援兵力が十分なためこれ以上出撃しません。");
                continue;
            }

            // 在城中のメンバーを出撃させる。
            var cands = castle.Members
                .Where(m => m.IsDefendable)
                .Select(m => (chara: m, eta: World.Forces.ETADays(m, m.Castle.Position, allyCastle, ForceMode.Reinforcement)))
                // 損耗が少ない
                .Where(x => x.chara.Soldiers.All(s => s.Hp > 25))
                .ToList();
            // 援軍候補がない場合は何もしない。
            if (cands.Count == 0) return;

            var maxETA = cands.Select(x => x.eta).Max();
            //Debug.LogWarning($"cands:\n{string.Join("\n", cands)}");
            while (dangerPower > defPower && cands.Count > 0)
            {
                var defendables = castle.Members.Count(m => m.IsDefendable);
                if (defendables <= 1)
                {
                    Debug.Log($"防衛兵力が1人以下になるためこれ以上出撃しません。");
                    break;
                }

                var target = cands.RandomPickWeighted(x => Mathf.Pow(10 + maxETA - x.eta, 2), true);
                var (member, eta) = target;

                var action = StrategyActions.DeployAsReinforcement;
                var args = action.Args(castle.Boss, member, allyCastle);
                if (action.CanDo(args))
                {
                    await action.Do(args);
                    defPower += member.Power;
                    Debug.LogWarning($"{member.Name}が{allyCastle}へ援軍として出撃しました。");
                }
                cands.Remove(target);
            }
        }
    }
}