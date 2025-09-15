using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class AI
{
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
        var salaryEstimate = target.Salary * 3;

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
}