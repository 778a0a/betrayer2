using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class AI
{
    private GameCore core;
    private WorldData world;
    public AI(GameCore core)
    {
        this.core = core;
        world = core.World;
    }

    /// <summary>
    /// 城の方針を決定します。
    /// </summary>
    public CastleObjective SelectCastleObjective(Character ruler, Castle castle)
    {
        var country = castle.Country;
        var neighbors = castle.Neighbors.Where(c => c.Country != country);
        var minRel = neighbors
            .Select(n => n.Country.Relation(country))
            .DefaultIfEmpty(100)
            .Min();
        return Util.EnumArray<CastleObjective>().RandomPickWeighted(o =>
        {
            switch (o)
            {
                // 攻撃方針 戦闘+
                // ・近隣に友好度の低い国がある
                // ・近隣に友好的でなく戦力の低い城がある
                // ・近隣に在城戦力の低い城がある
                case CastleObjective.Attack:
                    var val = 0f;
                    foreach (var neighbor in neighbors)
                    {
                        var rel = neighbor.Country.Relation(country);
                        if (rel <= 40)
                        {
                            var hateAdj = Mathf.Lerp(100, 400, (40 - rel) / 40f);
                            var powerAdj = Mathf.Lerp(0, 200, (castle.Power / (neighbor.Power + 0.01f)) - 1);
                            var powerAdj2 = Mathf.Lerp(0, 200, (castle.Power / (neighbor.DefencePower + 0.01f)) - 1);
                            val = Mathf.Max(val, hateAdj + powerAdj + powerAdj2);
                            var memberAdj = castle.Members.Count > 2;
                            val *= memberAdj ? 1 : 0.1f;
                        }
                    }
                    return val;

                case CastleObjective.Train:
                    if (minRel <= 20) return 300;
                    if (minRel < 50) return 200;
                    if (minRel >= 80) return 0;
                    return 100;

                case CastleObjective.CastleStrength:
                    if (castle.Strength == castle.StrengthMax) return 0;
                    if (minRel <= 20) return 200;
                    if (minRel < 50) return 100;
                    return 50;

                case CastleObjective.Stability:
                    if (castle.Stability < 90) return 700;
                    if (castle.Stability < 100) return 100;
                    return 0;

                case CastleObjective.Agriculture:
                    if (castle.Stability < 90) return 0;
                    if (castle.FoodIncome == castle.FoodIncomeMax) return 0;
                    if (castle.FoodBalance < 0) return 500;
                    return 100;

                case CastleObjective.Commerce:
                    if (castle.Stability < 90) return 0;
                    if (castle.GoldIncome == castle.GoldIncomeMax) return 0;
                    if (castle.GoldBalance < 0) return 500;
                    return 100;
                default:
                    return 0;
            }
        }, false);
    }

    /// <summary>
    /// 外交を行います。
    /// </summary>
    public async Awaitable Diplomacy(Country country)
    {
        var neighbors = country.Neighbors.ToList();

        // 同盟
        foreach (var neighbor in neighbors)
        {
            var rel = country.Relation(neighbor);
            if (rel == Country.AllyRelation) continue;
            if (rel < 80) continue;

            var prob = Mathf.Lerp(0.3f, 0.8f, (rel - 80) / 20f);
            if ((prob / 12).Chance())
            {
                // 同盟を申し込む。
                var act = core.CastleActions.Ally;
                var args = act.Args(country.Ruler, neighbor);
                if (act.CanDo(args))
                {
                    await act.Do(args);
                }
                else
                {
                    Debug.Log($"前提不足のため同盟申し込みできませんでした。{args}");
                }
            }
        }

        // 親善
        // TODO
    }

    /// <summary>
    /// 出撃
    /// </summary>
    public void Deploy(Castle castle)
    {
        var boss = castle.Boss;
        var neighbors = castle.Neighbors.Where(c => c.Country != castle.Country).ToList();

        // 攻撃するか判定する。
        var shouldAttack = castle.Objective == CastleObjective.Attack ?
            0.3f :
            0.1f;
        if (!shouldAttack.Chance())
        {
            Debug.Log($"出撃判定 {castle} shouldAttack == false");
            return;
        }

        // 防衛可能なメンバーが少ないなら何もしない。
        if (castle.Members.Count(m => m.IsDefenceable) < 2)
        {
            Debug.Log($"出撃判定 {castle} 防衛メンバー過少");
            return;
        }

        var targetCands = new List<Castle>();
        foreach (var neighbor in neighbors)
        {
            var rel = neighbor.Country.Relation(castle.Country);
            if (rel >= 50) continue;
            targetCands.Add(neighbor);
        }

        if (targetCands.Count == 0)
        {
            Debug.Log($"出撃判定 {castle} 目標なし");
            return;
        }

        var target = targetCands.RandomPickWeighted(neighbor =>
        {
            var val = 100f;
            var rel = neighbor.Country.Relation(castle.Country);
            var hateAdj = Mathf.Lerp(100, 400, (40 - rel) / 40f);
            var powerAdj = Mathf.Lerp(0, 200, (castle.Power / (neighbor.Power + 0.01f)) - 1);
            var powerAdj2 = Mathf.Lerp(0, 200, (castle.Power / (neighbor.DefencePower + 0.01f)) - 1);
            val = Mathf.Max(val, hateAdj + powerAdj + powerAdj2);
            var memberAdj = castle.Members.Count > 2;
            val *= memberAdj ? 1 : 0.1f;
            return Mathf.Lerp(0, 100, (50 - rel) / 50f);
        });

        Debug.Log($"出撃判定 {castle} 出撃します。 目標: {target}");

        // 城に残す人数
        var leaveCount = Random.Range(1, 3);
        while (castle.Members.Count(m => m.IsDefenceable) > leaveCount)
        {
            var attacker = castle.Members.Where(m => m.IsDefenceable).RandomPick();
            var act = core.CastleActions.Move;
            var args = act.Args(boss, attacker, target);

            Debug.Log($"出撃候補 {attacker}");
            if (act.CanDo(args))
            {
                act.Do(args);
            }
            else
            {
                leaveCount++;
            }
        }
    }
}
