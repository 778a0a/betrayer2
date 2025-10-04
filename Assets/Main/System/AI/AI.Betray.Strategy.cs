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
    /// 裏切り（城主用）
    /// </summary>
    public async ValueTask<bool> BetrayOnStrategyPhase(Character chara)
    {
        // 城主のみ実行可能
        if (chara.IsRuler || !chara.IsBoss) return false;
        // 忠誠90以上は裏切らない。
        if (chara.Loyalty >= 90) return false;

        // 忠誠度に応じた確率で裏切る。野心も加味する。
        var rebelProb = (90 - chara.Loyalty + chara.Ambition) * 0.01f;

        // 他のメンバーの忠誠度も低いなら独立確率を上げる。
        var averageLoyalty = chara.Castle.Members.Where(c => c != chara).Average(c => c.Loyalty);
        rebelProb += (90 - averageLoyalty) * 0.01f;

        // 君主が弱いなら独立確率を上げる。
        if (chara.Country.Ruler.Power < chara.Power) rebelProb *= 1.25f;
        if (chara.Country.Ruler.Attack < chara.Attack) rebelProb *= 1.1f;
        if (chara.Country.Ruler.Defense < chara.Defense) rebelProb *= 1.1f;
        if (chara.Country.Ruler.Intelligence < chara.Intelligence) rebelProb *= 1.1f;
        if (chara.Country.Ruler.Governing < chara.Governing) rebelProb *= 1.1f;

        var shouldRebel = rebelProb.Chance();
        Debug.LogWarning($"{chara.Name} 独立判定: {shouldRebel} ({rebelProb:0.00})");
        if (shouldRebel)
        {
            var action = StrategyActions.BecomeIndependent;
            var args = action.Args(chara);
            if (action.CanDo(args))
            {
                await action.Do(args);
            }
            else
            {
                Debug.LogWarning($"{chara.Name} 独立できず");
            }
        }

        return true;
    }
}