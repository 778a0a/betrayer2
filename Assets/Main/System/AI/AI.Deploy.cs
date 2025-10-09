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
    /// 出撃
    /// </summary>
    public async ValueTask Deploy(Castle castle)
    {
        if (castle.DangerForcesExists) return;
        if (castle.Country.Ruler.IsPlayer && castle.DeployPolicy != CastleDeployPolicy.Allow) return;

        var boss = castle.Boss;
        var neighbors = castle.Neighbors.Where(c => c.Country != castle.Country).ToList();

        // 攻撃するか判定する。
        var shouldAttack = castle.Objective is CastleObjective.Attack ?
            0.3f :
            0.05f;
        if (!shouldAttack.Chance())
        {
            //Debug.Log($"出撃判定 {castle} shouldAttack == false");
            return;
        }

        // 防衛可能なメンバーが少ないなら何もしない。
        if (castle.Members.Count(m => m.IsDefendable) < 2)
        {
            //Debug.Log($"出撃判定 {castle} 防衛メンバー過少");
            return;
        }

        var charaCands = castle.Members
            .Where(m => m.IsDefendable)
            // 兵士数が減っているキャラも除外する。
            .Where(m => 1f * m.Soldiers.SoldierCount / m.Soldiers.SoldierCountMax > 0.8f)
            .ToList();
        if (charaCands.Count < 2)
        {
            //Debug.Log($"出撃判定 {castle} 出撃候補過少");
            return;
        }

        var targetCands = new List<Castle>();
        var relThresh = castle.Country.Ruler.Personality switch
        {
            Personality.Merchant => 15,
            Personality.Pacifist => 31,
            _ => 45,
        };
        foreach (var neighbor in neighbors)
        {
            var rel = neighbor.Country.GetRelation(castle.Country);
            var objectiveAdj = castle.IsAttackTarget(neighbor) ? 10 : 0;
            if (rel >= relThresh + objectiveAdj) continue;
            targetCands.Add(neighbor);
        }

        if (targetCands.Count == 0)
        {
            //Debug.Log($"出撃判定 {castle} 目標なし");
            return;
        }

        // 敵対国がある場合はそちらを優先する。
        var targetCandsEnemy = targetCands.Where(c => c.Country.IsEnemy(castle.Country)).ToList();
        if (targetCandsEnemy.Count > 0)
        {
            targetCands = targetCandsEnemy;
        }

        var minRel = neighbors.Min(n => n.Country.GetRelation(castle.Country));
        var target = targetCands.RandomPickWeighted(neighbor =>
        {
            var val = 100f;
            var rel = neighbor.Country.GetRelation(castle.Country);
            var hateAdj = Mathf.Lerp(100, 400, (40 - rel) / 40f);
            var powerAdj = Mathf.Lerp(0, 200, (castle.Power / (neighbor.Power + 0.01f)) - 1);
            var powerAdj2 = Mathf.Lerp(0, 200, (castle.Power / (neighbor.DefencePower + 0.01f)) - 1);
            val = Mathf.Max(val, hateAdj + powerAdj + powerAdj2);
            if (rel == minRel) val *= 3f;
            if (castle.Objective.IsAttackTarget(neighbor)) val *= 10f;
            if (castle.Country.Objective.IsAttackTarget(neighbor)) val *= 10f;
            return val;
        });

        if (castle.Country.Ruler.Personality != Personality.Chaos)
        {
            // すでに他に敵対国がある場合は、敵対国でない国を攻撃しないようにする。
            var otherEnemyExists = !target.Country.IsEnemy(castle) && castle.Country.Neighbors.Any(castle.Country.IsEnemy);
            if (otherEnemyExists)
            {
                // ただし、攻撃目標に含まれているなら低確率で攻撃する。
                var isAttackTarget = castle.IsAttackTarget(target);
                if (isAttackTarget && 0.1f.Chance())
                {
                    Debug.LogError($"出撃判定 {castle} 目標 {target} は敵対国ではないですが攻撃目標のため出撃します。");
                }
                else
                {
                    Debug.LogError($"出撃判定 {castle} 目標 {target} は敵対国ではないため出撃しません。");
                    return;
                }
            }
        }

        //Debug.Log($"出撃判定 {castle} 出撃します。 目標: {target}");

        // 城に残す人数
        var leaveCount = 0;
        switch (boss.Personality)
        {
            case Personality.Warrior:
            case Personality.Pirate:
            case Personality.Chaos:
                leaveCount = Random.Range(0, 3);
                break;
            default:
                leaveCount = Random.Range(1, 3);
                break;
        }
        while (charaCands.Count > leaveCount)
        {
            var attacker = charaCands.RandomPick();
            var act = core.StrategyActions.Deploy;
            var args = act.Args(boss, attacker, target);

            //Debug.Log($"出撃候補 {attacker}");
            if (act.CanDo(args))
            {
                await act.Do(args);
                charaCands.Remove(attacker);
            }
            else
            {
                leaveCount++;
            }
        }
    }
}