using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
            .Select(n => world.Countries.GetRelation(n.Country, country))
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
                        var rel = world.Countries.GetRelation(neighbor.Country, country);
                        if (rel <= 40)
                        {
                            var hateAdj = Mathf.Lerp(100, 400, (40 - rel) / 40f);
                            var powerAdj = Mathf.Lerp(0, 200, (castle.Power / (neighbor.Power + 0.01f)) - 1);
                            var powerAdj2 = Mathf.Lerp(0, 200, (castle.Power / (neighbor.DefencePower + 0.01f)) - 1);
                            val = Mathf.Max(val, hateAdj + powerAdj + powerAdj2);
                            var memberAdj = castle.Members.Count <= 1;
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
                    if (castle.Stability < 90) return 500;
                    if (castle.Stability < 100) return 100;
                    return 0;

                case CastleObjective.Agriculture:
                    if (castle.Stability < 90) return 0;
                    if (castle.FoodIncome == castle.FoodIncomeMax) return 0;
                    if (castle.FoodBalance < 0) return 500;
                    return 100;

                case CastleObjective.Commerce:
                    if (castle.Stability < 100) return 0;
                    if (castle.GoldIncome == castle.GoldIncomeMax) return 0;
                    if (castle.GoldBalance < 0) return 500;
                    return 100;
                default:
                    return 0;
            }
        });
    }
}
