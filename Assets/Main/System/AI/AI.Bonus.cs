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
    /// 君主用 忠誠の低いメンバーに褒賞を与える。
    /// </summary>
    public async ValueTask Bonus(Country country)
    {
        var ruler = country.Ruler;

        // 忠誠度が100以上ない城主は優先して褒賞を与える。
        var bossWithLowLoyalty = country.Castles
            .Select(c => c.Boss)
            .Where(b => b != null && !b.IsRuler && b.Loyalty < 100)
            .ToList();
        await BonusCore(ruler, bossWithLowLoyalty);

        var targetMembers = country.Members
            .Where(c => !c.IsRuler)
            .OrderBy(c => c.Loyalty);

        while (ruler.ActionPoints > 50 && targetMembers.Take(5).Select(m => m.Loyalty).DefaultIfEmpty(100).Average() < 95)
        {
            var lowLoyaltyMembers = targetMembers.Take(5);
            await BonusCore(ruler, lowLoyaltyMembers);
        }
    }

    public async ValueTask BonusFromBoss(Character boss)
    {
        // 君主なら必ず実行する。
        // 君主以外は、忠誠度90以上なら1あたり10%の確率で実行する。
        var prob = boss.IsRuler ? 1 : ((boss.Loyalty - 90) * 0.1f).MaxWith(1);

        var shouldDo = prob.Chance();
        if (!shouldDo) return;

        // 君主または国主なら、隣接する城も対象にする。
        var memberSource = boss.IsRuler || boss.IsRegionBoss ?
            boss.Castle.Members.Concat(boss.Castle.Neighbors.Where(c => c.Country == boss.Country).SelectMany(c => c.Members)) :
            boss.Castle.Members;

        var bonusCount = (int)((boss.Governing - 50) * 0.1f * prob).MinWith(1);
        var targetMembers = memberSource
            .Where(c => c != boss)
            .Where(c => c.OrderIndex > boss.OrderIndex)
            .OrderBy(c => c.Loyalty)
            .Take(bonusCount);

        await BonusCore(boss, targetMembers);
    }

    private async ValueTask BonusCore(Character boss, IEnumerable<Character> targets)
    {
        foreach (var member in targets)
        {
            var action = core.StrategyActions.Bonus;
            var args = action.Args(boss, member);
            if (action.CanDo(args))
            {
                await action.Do(args);
            }
            else
            {
                return;
            }
        }
    }
}