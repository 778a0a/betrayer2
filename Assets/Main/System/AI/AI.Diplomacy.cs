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
            async ValueTask Do()
            {
                var action = core.StrategyActions.Goodwill;
                var args = action.Args(country.Ruler, neighbor);
                if (action.CanDo(args))
                {
                    await action.Do(args);
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
                await Do();
            }
        }
    }
}