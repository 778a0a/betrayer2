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
                        var relThresh = country.Ruler.Personality switch
                        {
                            Personality.Merchant => 15,
                            Personality.Pacifist => 31,
                            _ => 40,
                        };
                        if (rel <= relThresh)
                        {
                            var hateAdj = Mathf.Lerp(100, 400, (relThresh - rel) / relThresh);
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
                    return 50;

                case CastleObjective.CastleStrength:
                    if (castle.Strength == castle.StrengthMax) return 0;
                    //if (castle.DangerForcesExists) return 500;
                    if (minRel <= 20) return 50;
                    return 10;

                case CastleObjective.Commerce:
                    if (castle.GoldIncome == castle.GoldIncomeMax) return 0;
                    if (castle.GoldBalance < 0) return 1000;
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
            if (rel < 75) continue;

            var prob = Mathf.Lerp(0.3f, 0.8f, (rel - 75) / 20f);
            if ((prob / 12).Chance())
            {
                // 同盟を申し込む。
                var act = core.StrategyActions.Ally;
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
        foreach (var neighbor in neighbors.OrderBy(_ => Random.value))
        {
            void Do()
            {
                var action = core.StrategyActions.Goodwill;
                var args = action.Args(country.Ruler, neighbor);
                if (action.CanDo(args))
                {
                    action.Do(args);
                }
            }

            var castle = country.Ruler.Castle;
            var rel = country.GetRelation(neighbor);

            // 自城が豊かなら+
            var probGold = castle.GoldBalance > 0;
            // 敵対国と敵対しているなら+
            var probEnemyEnemy = neighbor.Neighbors
                .Where(n => n != country)
                .Any(n => neighbor.GetRelation(n) < 20 && country.GetRelation(n) < 20);
            // 相手が強いほど+
            var probTargetStrong = country.Members.Sum(m => m.Power) < neighbor.Members.Sum(m => m.Power);

            var prob = 0f;
            switch (country.Ruler.Personality)
            {
                case Personality.Conqueror:
                    // 自城が豊かなら+
                    prob += probGold ? 0.1f : 0;
                    // 敵対国と敵対しているなら+
                    prob += probEnemyEnemy ? 0.1f : 0;
                    // 他に敵対国がなくて一番仲の悪い国とは行わない
                    if (neighbors.Except(new[] { neighbor }).All(n => n.GetRelation(country) >= 50)) continue;
                    // 友好度45以上なら+
                    if (rel < 45) continue;
                    // 友好度が高すぎるなら-
                    if (rel >= 90) prob *= 0.5f;
                    break;
                case Personality.Leader:
                    // 自城が豊かなら+
                    prob += probGold ? 0.1f : 0;
                    // 敵対国と敵対しているなら+
                    prob += probEnemyEnemy ? 0.2f : 0;
                    // 友好度40以上なら+
                    if (rel < 40) continue;
                    // 友好度が高すぎるなら-
                    if (rel >= 90) prob *= 0.5f;
                    // 隣接国なら+
                    prob *= country.Neighbors.Contains(neighbor) ? 1 : 0.5f;
                    break;
                case Personality.Pacifist:
                    // 自城が豊かなら+
                    prob += probGold ? 0.1f : 0;
                    // 友好度30以上で友好度が高いほど+
                    prob += Mathf.Lerp(0.1f, 0.2f, (rel - 30) / 70f);
                    // 敵対国と敵対しているなら+
                    prob += probEnemyEnemy ? 0.1f : 0;
                    // 相手が強いほど+
                    prob += probTargetStrong ? 0.1f : 0;
                    // 友好度40以上なら+
                    if (rel < 40) continue;
                    break;
                case Personality.Merchant:
                    // 自城が豊かなら+
                    prob += probGold ? 0.2f : 0;
                    // 友好度40以上で友好度が低いほど+
                    prob += Mathf.Lerp(0.4f, 0.0f, (rel - 40) / 60f);
                    //Debug.Log($"{Mathf.Lerp(0.4f, 0.0f, (rel - 40) / 60f)}, {rel}");
                    if (rel < 55) prob += 0.2f;
                    // 敵対国と敵対しているなら+
                    prob += probEnemyEnemy ? 0.2f : 0;
                    // 友好度40以上なら+
                    if (rel < 40) continue;
                    // 友好度が高すぎるなら-
                    if (rel >= 80) prob *= 0.4f;
                    // 隣接国なら+
                    prob *= country.Neighbors.Contains(neighbor) ? 1 : 0.5f;
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
                    prob += probGold ? 0.1f : 0;
                    // 友好度40以上なら+
                    if (rel < 40) continue;
                    // 友好度が高すぎるなら-
                    if (rel >= 90) prob *= 0.5f;
                    break;
            }

            if ((prob / 12).Chance())
            {
                Do();
            }
        }
    }

    /// <summary>
    /// 出撃
    /// </summary>
    public void Deploy(Castle castle)
    {
        if (castle.DangerForcesExists) return;

        var boss = castle.Boss;
        var neighbors = castle.Neighbors.Where(c => c.Country != castle.Country).ToList();

        // 攻撃するか判定する。
        var shouldAttack = castle.Objective == CastleObjective.Attack ?
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
            if (rel >= relThresh) continue;
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
            return val;
        });

        if (castle.Country.Ruler.Personality != Personality.Chaos)
        {
            // すでに他に敵対国がある場合は、敵対国でない国を攻撃しないようにする。
            if (!target.Country.IsEnemy(castle) && castle.Country.Neighbors.Any(castle.Country.IsEnemy))
            {
                Debug.LogError($"出撃判定 {castle} 目標 {target} は敵対国ではないため出撃しません。");
                return;
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
        while (castle.Members.Count(m => m.IsDefendable) > leaveCount)
        {
            var attacker = castle.Members.Where(m => m.IsDefendable).RandomPick();
            var act = core.StrategyActions.Deploy;
            var args = act.Args(boss, attacker, target);

            //Debug.Log($"出撃候補 {attacker}");
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

    public async ValueTask HireVassal(Castle castle)
    {
        // 未所属キャラがいないなら何もしない。
        if (castle.Frees.Count == 0)
        {
            return;
        }
        
        var country = castle.Country;

        // 国の城の数*5を採用上限にする。
        if (country.Castles.Count * 5 <= country.Members.Count())
        {
            return;
        }

        // 採用対象のキャラを決める。
        var target = castle.Frees.RandomPickWeighted(c => c.Power);
        var salaryEstimate = target.Salary;

        // 前線でないなら採用確率を下げる。
        var prob = castle.IsFrontline ? 0.8f : 0.1f;

        if (!castle.IsFrontline)
        {
            // 採用後の収支が心もとないなら何もしない。
            var newBalance = castle.GoldBalance - salaryEstimate;
            var expenceEstimate = Mathf.Max(-newBalance, salaryEstimate) * 12;
            if (newBalance < 10 && castle.GoldSurplus < expenceEstimate)
            {
                return;
            }
        }

        // 国全体の収支が心もとないなら何もしない。
        var newCountryBalance = country.GoldBalance - salaryEstimate;
        var countryExpenceEstimate = Mathf.Max(-newCountryBalance, salaryEstimate) * 12;
        if (newCountryBalance < 10 && (country.GoldSurplus < countryExpenceEstimate))
        {
            return;
        }

        if (!prob.Chance())
        {
            return;
        }

        var action = core.StrategyActions.HireVassal;
        var args = action.Args(castle.Boss, target);
        if (action.CanDo(args))
        {
            await action.Do(args);
        }
        else
        {
            Debug.Log($"前提不足のため人材募集できませんでした。{args}");
        }
    }

    public async ValueTask Invest(Castle castle)
    {
        // 物資が余っていないなら何もしない。
        if (castle.GoldSurplus < 0)
        {
            return;
        }
        var actor = castle.Boss;
        var args = core.StrategyActions.Invest.Args(actor);
        var act = core.StrategyActions.Invest;

        var budget = (castle.GoldIncome * 0.5f).Clamp(0, castle.Gold);
        var count = 0;
        var cost = act.Cost(args).castleGold;
        while (budget > cost)
        {
            await act.Do(args);
            budget -= cost;
            count++;
            if (count > 10)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 輸送（上納）
    /// </summary>
    /// <param name="castle"></param>
    /// <param name="boss"></param>
    /// <returns></returns>
    public async ValueTask TransportAsTribute(Castle castle, Character boss)
    {
        var ruler = castle.Country.Ruler;
        Util.IsTrue(boss != ruler, "君主は上納できません。");

        // 赤字の場合は何もしない。
        if (castle.GoldBalance <= 0)
        {
            return;
        }

        // 黒字の場合は、現在の金残高の半分を上納する。
        var gold = castle.Gold / 2;
        if (gold > 1)
        {
            var act = core.StrategyActions.Transport;
            var args = act.Args(castle.Boss, castle, ruler.Castle, gold);
            if (act.CanDo(args))
            {
                await act.Do(args);
                Debug.LogWarning($"[輸送 - 上納] {castle.Boss.Name}が{ruler.Castle}へ{gold}G を輸送しました。");
            }
        }

    }

    /// <summary>
    /// 輸送（君主用）
    /// </summary>
    public async ValueTask TransportAsDistribution(Country country)
    {
        // 物資が不足している城へ豊かな城から輸送する。
        foreach (var castle in country.Castles)
        {
            // 誰もいない場合は対象外
            if (castle.Boss == null) continue;
            // 物資が足りている城は対象外
            if (castle.Gold > 0) continue;

            var wealthyCastles = country.Castles
                .Where(c => c != castle && c.Boss != null)
                .Where(c => c.Gold > 0)
                .OrderByDescending(c => c.Gold);

            var act = core.StrategyActions.Transport;
            foreach (var wealthy in wealthyCastles)
            {
                var needGold = -castle.Gold;
                if (needGold <= 0) break;

                var gold = needGold.Clamp(0, wealthy.Gold);
                if (gold > 0)
                {
                    var args = act.Args(country.Ruler, wealthy, castle, gold);
                    if (act.CanDo(args))
                    {
                        await act.Do(args);
                        Debug.LogError($"[輸送 - 補充] {wealthy.Boss.Name}が{castle}へ{gold}G を輸送しました。");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 忠誠の低いメンバーに褒賞を与える。
    /// </summary>
    /// <param name="castle"></param>
    public async ValueTask Bonus(Castle castle)
    {
        // TODO
        //// 実行可能なら褒賞を与えて忠誠を上げる。
        //var act = core.CastleActions.Bonus;
        //var args = act.Args(castle.Boss, target);
        //if (act.CanDo(args))
        //{
        //    await act.Do(args);
        //}
    }

    /// <summary>
    /// 君主用 忠誠の低いメンバーに褒賞を与える。
    /// </summary>
    public async ValueTask BonusFromRuler(Country country)
    {
        // TODO
        //throw new NotImplementedException();
    }
}
