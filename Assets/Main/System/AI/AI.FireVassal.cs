using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class AI
{
    public async ValueTask FireVassal(Character ruler)
    {
        var country = ruler.Country;

        // 収支が大きな赤字でないなら何もしない。
        if (country.GoldBalance >= -30) return;
        // 収支が赤字でも物資が豊富なら何もしない。
        if (country.GoldSurplus >= 0) return;

        // 序列が下位50%のなかで、もっとも非力な配下を解雇する。
        var members = country.Members.ToList();
        var halfCount = (int)Math.Ceiling(members.Count / 2f);
        var candidates = members
            .Where(m => m != ruler)
            .OrderByDescending(m => m.OrderIndex)
            .Take(halfCount)
            .Where(m => !m.IsMoving)
            .Where(m => !m.IsImportant)
            .ToList();

        // 最大3人まで解雇を試みる。
        var count = 3;
        while (country.GoldBalance < -30 && candidates.Count > 0 && count-- > 0)
        {
            var target = candidates
                .OrderBy(m => m.Power)
                .FirstOrDefault();

            if (target == null) return;

            if (target != null)
            {
                var act = StrategyActions.FireVassal;
                var args = act.Args(ruler, target);
                Debug.LogError($"{country} 赤字のため、{target}を解雇します。");
                if (act.CanDo(args))
                {
                    await StrategyActions.FireVassal.Do(args);
                    // 解雇成功失敗にかかわらず対象から外す。
                    candidates.Remove(target);
                }
                else
                {
                    Debug.LogWarning($"{country} 赤字のため、{target}を解雇しようとしましたが実行不可でした。");
                    return;
                }
            }
        }
    }
}