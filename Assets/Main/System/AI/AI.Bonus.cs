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

        do
        {
            var lowLoyaltyMembers = targetMembers.Take(5);
            await BonusCore(ruler, lowLoyaltyMembers);
        }
        while (ruler.ActionPoints > 50 && targetMembers.Take(5).Select(m => m.Loyalty).DefaultIfEmpty(100).Average() < 90);
    }

    public async ValueTask BonusFromBoss(Character boss)
    {
        // 君主なら必ず実行する。
        // 君主以外は、忠誠度90以上なら1あたり10%の確率で実行する。
        var prob = boss.IsRuler ? 1 : (boss.Loyalty - 90) * 0.1f;

        var shouldDo = prob.Chance();
        if (!shouldDo) return;

        var memberCount = Mathf.Min(boss.Castle.Members.Count, boss.Castle.MaxMember);
        var bonusCount = (int)(memberCount * ((boss.Governing - 50) / 50f) * prob).MinWith(1);
        var targetMembers = boss.Castle.Members
            .Where(c => c != boss)
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