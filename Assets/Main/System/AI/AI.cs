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
            .Select(n => n.Country.GetRelation(country))
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
                        var rel = neighbor.Country.GetRelation(country);
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
    public async ValueTask Diplomacy(Country country)
    {
        var neighbors = country.DiplomacyTargets.ToList();

        // 同盟
        foreach (var neighbor in neighbors)
        {
            var rel = country.GetRelation(neighbor);
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
        foreach (var neighbor in neighbors)
        {
            void Do()
            {
            }

            switch (country.Ruler.Personality)
            {
                case Personality.Conqueror:
                    // 自城が豊かなら+
                    // 敵対国と敵対しているなら+
                    // 他に敵対国がなくて一番仲の悪い国とは行わない
                    // 友好度50以上なら+
                    break;
                case Personality.Leader:
                    // 自城が豊かなら+
                    // 貧しい同盟国なら+
                    // 敵対国と敵対しているなら+
                    // 友好度40以上なら+
                    break;
                case Personality.Pacifism:
                    // 自城が豊かなら+
                    // 友好度30以上で友好度が高いほど+
                    // 敵対国と敵対しているなら+
                    // 相手が強いほど+
                    break;
                case Personality.Merchant:
                    // 自城が豊かなら+
                    // 友好度40以上で友好度が低いほど+
                    // 敵対国と敵対しているなら+
                    break;
                case Personality.Warrior:
                case Personality.Pirate:
                case Personality.Chaos:
                    // 行わない
                    break;
                case Personality.Knight:
                case Personality.Normal:
                default:
                    // 自城が豊かなら+
                    // 友好度40以上なら+
                    break;
            }
        }
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
            var rel = neighbor.Country.GetRelation(castle.Country);
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
            var rel = neighbor.Country.GetRelation(castle.Country);
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

    public void HireVassal(Castle castle)
    {
        // 未所属キャラがいないなら何もしない。
        if (castle.Frees.Count == 0)
        {
            return;
        }

        // 人数が少ない場合
        if (castle.Members.Count <= 2)
        {
            var chara = castle.Frees.RandomPick();
            castle.Members.Add(chara);
            castle.Frees.Remove(chara);
            Debug.Log($"{chara} が {castle} に採用されました。");
        }

        // 人数が十分いる場合は、十分に豊かな場合のみ採用する。
        // TODO
    }
}
