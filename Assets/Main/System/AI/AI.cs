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
                            Personality.Pacifism => 15,
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
                    if (castle.DangerForcesExists) return 500;
                    if (minRel <= 20) return 50;
                    if (castle.Strength / castle.StrengthMax < 0.5f) return 20;
                    return 10;

                case CastleObjective.Stability:
                    if (castle.Stability < 90) return 10000;
                    if (castle.Stability < 100) return 100;
                    return 0;

                case CastleObjective.Agriculture:
                    if (castle.Stability < 90) return 0;
                    if (castle.FoodIncome == castle.FoodIncomeMax) return 0;
                    if (castle.FoodBalance < 0) return 1000;
                    return 100;

                case CastleObjective.Commerce:
                    if (castle.Stability < 90) return 0;
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
        foreach (var neighbor in neighbors.OrderBy(_ => Random.value))
        {
            void Do()
            {
                var action = core.CastleActions.Goodwill;
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
                case Personality.Pacifism:
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
                    Debug.Log($"{Mathf.Lerp(0.4f, 0.0f, (rel - 40) / 60f)}, {rel}");
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
            Debug.Log($"出撃判定 {castle} 防衛メンバー過少");
            return;
        }

        var targetCands = new List<Castle>();
        var relThresh = castle.Country.Ruler.Personality switch
        {
            Personality.Merchant => 15,
            Personality.Pacifism => 15,
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
            Debug.Log($"出撃判定 {castle} 目標なし");
            return;
        }

        // 敵対国がある場合はそちらを優先する。
        var targetCandsEnemy = targetCands.Where(c => c.Country.IsEnemy(castle.Country)).ToList();
        if (targetCandsEnemy.Count > 0)
        {
            targetCands = targetCandsEnemy;
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
        while (castle.Members.Count(m => m.IsDefendable) > leaveCount)
        {
            var attacker = castle.Members.Where(m => m.IsDefendable).RandomPick();
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

        // 資金・食料が足りない場合は何もしない。
        var balance = castle.GoldBalance + castle.FoodBalance / 50;
        if (balance < 30)
        {
            return;
        }

        // 採用後の収支が心もとないなら何もしない。
        var chara = castle.Frees.RandomPick();
        var newBalance = balance - chara.Salary - chara.FoodConsumption / 50;
        if (newBalance < 20)
        {
            return;
        }

        // 国全体の収支が心もとないなら何もしない。
        var countryBalance = castle.Country.GoldBalance + castle.Country.FoodBalance / 50;
        if (countryBalance < 30)
        {
            return;
        }

        chara.Contribution /= 2;
        chara.ChangeCastle(castle, false);
        Debug.Log($"{chara} が {castle} に採用されました。");
    }

    /// <summary>
    /// 必要量の購入処理
    /// </summary>
    public void TradeNeeds(Castle castle)
    {
        // 食料がマイナスの場合、ゴールドがあれば食料を購入する。
        var buyCount = 0;
        while (castle.Food < 0 && castle.Gold > 0)
        {
            Debug.LogError($"[Trade] {castle} 食料購入");
            // 購入量を計算する。
            var act = core.CastleActions.BuyFood;
            var args = act.Args(castle.Boss, castle, 0);
            var inputGoldMax = act.InputGoldMax(args);
            var needGold = act.InverseGold(args, -castle.Food);
            var inputGold = castle.Gold.MaxWith(needGold).MaxWith(inputGoldMax);
            // 所持金が少なすぎる場合はあまり意味がないので行動しない。
            if (inputGold < 0.1 && inputGold != needGold)
            {
                Debug.LogError($"[Trade] {castle} 食料購入 所持金不足 ({inputGold})");
                break;
            }

            args = act.Args(castle.Boss, castle, inputGold);

            if (act.CanDo(args))
            {
                act.Do(args);
                buyCount++;
            }
            else
            {
                break;
            }
        }
        if (buyCount > 0)
        {
            Debug.LogError($"[Trade] {castle} 食料購入 {buyCount} 回");
        }

        // ゴールドがマイナスの場合、食料があれば売却する。
        var sellCount = 0;
        while (castle.Gold < 0 && castle.Food > 0)
        {
            Debug.LogError($"[Trade] {castle} 借金返済");
            // 売却量を計算する。
            var act = core.CastleActions.SellFood;
            var args = act.Args(castle.Boss, castle, 0);
            var inputFoodMax = act.InputFoodMax(args);
            var needFood = act.InverseFood(args, -castle.Gold);
            var inputFood = castle.Food.MaxWith(needFood).MaxWith(inputFoodMax);
            // 食料が少なすぎる場合はあまり意味がないので行動しない。
            if (inputFood < 5 && inputFood != needFood)
            {
                Debug.LogError($"[Trade] {castle} 借金返済 食料不足 ({needFood})");
                break;
            }

            args = act.Args(castle.Boss, castle, inputFood);
            if (act.CanDo(args))
            {
                act.Do(args);
                sellCount++;
            }
            else
            {
                break;
            }
        }
        if (sellCount > 0)
        {
            Debug.LogError($"[Trade] {castle} 借金返済 {sellCount} 回");
        }
    }

    public async ValueTask Develop(Castle castle)
    {
        // 十分発展していないなら何もしない。
        if (castle.GoldIncomeProgress < 0.75f || castle.FoodIncomeProgress < 0.75f)
        {
            return;
        }

        var goldSurplus = castle.GoldSurplus;
        var foodSurplus = castle.FoodSurplus;
        // 物資が余っていないなら何もしない。
        if (goldSurplus < 0 && foodSurplus < 0)
        {
            return;
        }

        var ruler = castle.Country.Ruler;
        var candActions = new List<(ActionBase act, ActionArgs args, ActionCost cost)>();

        // 町の開発度向上
        foreach (var town in castle.Towns)
        {
            var act = core.CastleActions.Develop;
            var args = act.Args(ruler, town);
            if (act.CanDo(args))
            {
                candActions.Add((act, args, act.Cost(args)));
            }
        }

        // 町建設
        {
            var candTiles = castle.NewTownCandidates(world).ToList();
            if (candTiles.Count > 0)
            {
                var act = core.CastleActions.BuildTown;
                var bestTile = candTiles.OrderByDescending(t =>
                    world.Economy.GetGoldAmount(Town.TileFoodMax(t, castle)) + Town.TileGoldMax(t, castle))
                    .First();
                var args = act.Args(ruler, castle, bestTile.Position);
                if (act.CanDo(args))
                {
                    candActions.Add((act, args, act.Cost(args)));
                }
            }
        }

        // 城壁強化
        {
            var act = core.CastleActions.ImproveCastleStrengthLevel;
            var args = act.Args(ruler, castle);
            if (act.CanDo(args))
            {
                candActions.Add((act, args, act.Cost(args)));
            }
        }

        if (candActions.Count == 0)
        {
            return;
        }

        // 一番コストの低い行動を選択する。
        var best = candActions.OrderBy(a => a.cost.castleGold).FirstOrDefault();
        if (best.act.CanDo(best.args))
        {
            await best.act.Do(best.args);
            Debug.LogError($"{castle}の開発を行いました。({best})");
            core.Pause();
        }

    }
}
