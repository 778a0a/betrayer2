using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class AI
{
    public CountryObjective SelectCountryObjective(Country country, CountryObjective prev)
    {
        var candTypes = CountryObjective.Candidates(country);
        // まず目標の種類を選ぶ。
        var cands = candTypes.RandomPickWeighted(list =>
        {
            if (list.Count == 0) return 0;
            var sample = list[0];
            var prevIsSameType = prev != null && prev.GetType() == sample.GetType();
            var sameAdj = prevIsSameType ? 5 : 1;
            switch (sample)
            {
                case CountryObjective.RegionConquest:
                    var personalityAdj = country.Ruler.Personality switch
                    {
                        Personality.Conqueror => 1000,
                        Personality.Chaos => 1000,
                        Personality.Warrior => 500,
                        Personality.Pacifist => 50,
                        Personality.Merchant => 50,
                        _ => 100,
                    };
                    return personalityAdj * sameAdj;
                case CountryObjective.CountryAttack:
                    // 敵対国があるなら優先する。
                    if (country.Neighbors.Any(c => c.IsEnemy(country))) return 1000 * sameAdj;
                    // 前回と同じ敵が候補にあれば優先する。
                    var clist = list.Cast<CountryObjective.CountryAttack>();
                    if (clist.Any(o => o == prev && World.Countries.First(o.IsAttackTarget).GetRelation(country) < 50)) return 1000 * sameAdj;
                    if (country.Neighbors.All(c => c.GetRelation(country) >= 50)) return 0;
                    var minRelAdj = (100 - country.Neighbors.Min(c => c.GetRelation(country))) * 10;
                    return minRelAdj * sameAdj;
                case CountryObjective.StatusQuo:
                    personalityAdj = country.Ruler.Personality switch
                    {
                        Personality.Pacifist => 1000,
                        Personality.Merchant => 1000,
                        Personality.Warrior => 50,
                        Personality.Conqueror => 10,
                        Personality.Chaos => 0,
                        _ => 100,
                    };
                    return personalityAdj * sameAdj;
                default: throw new Exception("Unknown CountryObjective type: " + sample.GetType().Name);
            }
        });

        // 次に具体的な目標を選ぶ。
        return cands.RandomPickWeighted(o =>
        {
            var prevIsSame = prev != null && prev == o;
            var sameAdj = prevIsSame ? 4 : 1;
            switch (o)
            {
                case CountryObjective.RegionConquest co:
                    var targetCastles = World.Castles.Where(co.IsAttackTarget).ToList();
                    // 統一済みなら選ばない。
                    if (targetCastles.All(country.IsSelfOrAlly)) return 0;
                    // 自国が含まれない地方の場合
                    if (targetCastles.All(country.IsAttackable))
                    {
                        // 他の地方が未統一の場合は選ばない。
                        var countryRegions = country.Castles.Select(c => c.Region).Distinct();
                        if (!prevIsSame && countryRegions.Any(r => World.Castles.Where(c => c.Region == r).Any(country.IsAttackable))) return 0;
                    }
                    // 未統一の地方の場合
                    var myCountAdj = targetCastles.Count(c => country.IsSelfOrAlly(c)) + 1;
                    var enemyCountAdj = targetCastles.Count(c => !country.IsSelf(c) && country.IsEnemy(c)) + 1;
                    var weakCountAdj = targetCastles.Where(country.IsAttackable).Max(c => c.Country.Power) < country.Power ? 3 : 1;
                    var closeAdj = country.Castles.Sum(c => c.Neighbors.Where(country.IsAttackable).Count(n => n.Region == co.TargetRegionName)) + 1;
                    return myCountAdj + enemyCountAdj * weakCountAdj * closeAdj * sameAdj;
                case CountryObjective.CountryAttack co:
                    var target = World.Countries.First(co.IsAttackTarget);
                    var enemyAdj = country.IsEnemy(target) ? 10 : 1;
                    var powerAdj = target.Power < country.Power ? 10 : 1;
                    var relAdj = Mathf.Lerp(1, 5, (100 - country.GetRelation(target)) / 100f);
                    var goodRelAdj = target.GetRelation(country) > 50 ? 0.1f : 1;
                    return enemyAdj * powerAdj * relAdj * goodRelAdj * sameAdj;
                case CountryObjective.StatusQuo:
                    return 1;
                default: throw new Exception("Unknown CountryObjective type: " + o.GetType().Name);
            }
        });
    }

    /// <summary>
    /// 城の方針を決定します。
    /// </summary>
    public CastleObjective SelectCastleObjective(Castle castle)
    {
        var country = castle.Country;
        var countryObjective = country.Objective;
        var neighbors = castle.Neighbors.Where(c => c.Country != country);
        var minRel = neighbors
            .Select(n => n.Country.GetRelation(country))
            .DefaultIfEmpty(100)
            .Min();

        var cands = CastleObjective.Candidates(castle);
        return cands.RandomPickWeighted(o =>
        {
            switch (o)
            {
                case CastleObjective.Attack atk:
                    var targetCastle = World.Castles.First(atk.IsAttackTarget);
                    var rel = country.GetRelation(targetCastle.Country);
                    var val = 0f;
                    var relThresh = country.Ruler.Personality switch
                    {
                        Personality.Merchant => 15,
                        Personality.Pacifist => 31,
                        _ => 40,
                    };

                    var countryObjectiveAdj = 1f;
                    switch (countryObjective)
                    {
                        case CountryObjective.RegionConquest co:
                            if (co.TargetRegionName == targetCastle.Region) countryObjectiveAdj = 2;
                            else countryObjectiveAdj = 0.1f;
                            relThresh += 10;
                            break;
                        case CountryObjective.CountryAttack co:
                            if (co.TargetRulerName == targetCastle.Country.Ruler.Name) countryObjectiveAdj = 2;
                            else countryObjectiveAdj = 0.1f;
                            relThresh += 10;
                            break;
                    }

                    if (rel <= relThresh)
                    {
                        var hateAdj = Mathf.Lerp(100, 400, (relThresh - rel) / relThresh);
                        var powerAdj = Mathf.Lerp(0, 200, (castle.Power / (targetCastle.Power + 0.01f)) - 1);
                        var powerAdj2 = Mathf.Lerp(0, 200, (castle.Power / (targetCastle.DefencePower + 0.01f)) - 1);
                        val = Mathf.Max(val, hateAdj + powerAdj + powerAdj2);
                        var memberAdj = castle.Members.Count > 2;
                        val *= memberAdj ? 1 : 0.1f;
                        val *= countryObjectiveAdj;
                    }
                    return val;
                case CastleObjective.Transport tran:
                    // 物資が不足している城を優先する。
                    targetCastle = castle.Country.Castles.FirstOrDefault(c => c.Name == tran.TargetCastleName);
                    if (targetCastle.GoldSurplus < 0) return 300 - targetCastle.GoldSurplus;
                    return castle.Members.Count * 10 + 50;
                case CastleObjective.Train:
                    if (minRel <= 20) return 300;
                    if (minRel < 50) return 200;
                    if (minRel >= 80) return 0;
                    return 50;

                case CastleObjective.Fortify:
                    if (castle.Strength == castle.StrengthMax) return 0;
                    //if (castle.DangerForcesExists) return 500;
                    if (minRel <= 20) return 50;
                    return 10;

                case CastleObjective.Develop:
                    if (castle.GoldIncome == castle.GoldIncomeMax) return 0;
                    if (castle.GoldSurplus < 0) return 1000;
                    return 100;
                default:
                    return 0;
            }
        }, false);
    }
}