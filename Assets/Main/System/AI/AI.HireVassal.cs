using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class AI
{
    public async ValueTask HireVassal(Character boss)
    {
        var castle = boss.Castle;
        var country = castle.Country;

        // すでに所属上限なら何もしない。
        if (castle.Members.Count >= 6)
        {
            return;
        }

        // 自城に向かっている自軍勢があり、それを加えると上限に達するなら何もしない。
        var incomingCharacters = World.Forces
            .Where(f => f.Country == country)
            .Where(f => f.Destination == castle)
            .ToList();
        if (castle.Members.Count + incomingCharacters.Count >= 6)
        {
            Debug.Log($"自城に向かっている軍勢があるため、採用は行いません。 {string.Join(", ", incomingCharacters.Select(f => f.Character.Name))}");
            return;
        }

        // 採用候補を選択する。
        var candidates = StrategyActions.HireVassalAction.SearchCandidates(boss);
        if (candidates.Count == 0)
        {
            return;
        }
        
        // 最も強力な人材を選ぶ。
        var target = candidates.OrderByDescending(c => c.Power).First();

        // 採用のコストを見積もる。
        var monthlyCost = (target.Salary * 1.25f).MaxWith(30) * 3;

        // 毎月の収支が赤字になるなら採用しない。
        if (castle.GoldBalance - monthlyCost < 0)
        {
            return;
        }
        // 国が収入不足の場合も採用しない。
        if (country.GoldBalance - monthlyCost < 0)
        {
            return;
        }

        // 前線でないなら採用確率を下げる。
        var prob = castle.IsFrontline ? 0.75f : 0.1f;
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
}