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
    /// 裏切り（一般用）
    /// </summary>
    public async ValueTask<bool> BetrayOnPersonalPhase(Character chara)
    {
        // 城主は独立アクションがあるので反乱は使用不可。
        if (chara.IsBoss) return false;
        // 移動中も不可。
        if (chara.IsMoving) return false;
        // 忠誠90以上は裏切らない。
        if (chara.Loyalty >= 90) return false;

        // 忠誠度に応じた確率で裏切る。野心も加味する。
        var betrayProb = (90 - chara.Loyalty + chara.Ambition / 2f) * 0.01f;
        if (!(1.25f * betrayProb / 12).Chance()) return false;

        Debug.LogWarning($"{chara.Name} 裏切り処理開始({betrayProb:0.00}) loyalty: {chara.Loyalty}");

        // 反乱確率（放浪確率の逆）
        var rebelProb = 0.5f;

        // 野心が少ないなら反乱確率を下げる。
        if (chara.Ambition < 7) rebelProb -= 0.1f * (7 - chara.Ambition);
        else rebelProb += 0.3f;

        // 忠誠度が低いほど反乱確率を上げる。
        rebelProb += (90 - chara.Loyalty) * 0.01f;

        // 君主または城主が弱いなら反乱確率を上げる。
        if (chara.Castle.Boss.Power < chara.Power || chara.Country.Ruler.Power < chara.Power)
        {
            rebelProb += 0.2f;
        }

        // 他のメンバーの忠誠度も低いなら反乱確率を上げる。
        var averageLoyalty = chara.Castle.Members
            .Where(c => c != chara && !c.IsRuler && !c.IsPlayer)
            .Select(c => c.Loyalty)
            .DefaultIfEmpty(90)
            .Average();
        rebelProb += (90 - averageLoyalty) * 0.01f;

        var shouldRebel = rebelProb.Chance();
        Debug.LogWarning($"{chara.Name} 反乱判定: {shouldRebel} ({rebelProb:0.00})");
        // 反乱
        if (shouldRebel)
        {
            var action = PersonalActions.Rebel;
            var args = action.Args(chara);
            // 物資欠乏でも実行できるようにしたいので、こっそり所持金を増やす。
            chara.Gold += action.GoldCost;
            if (action.CanDo(args))
            {
                await action.Do(args);
            }
            else
            {
                Debug.LogWarning($"{chara.Name} 反乱できず");
            }
        }
        // 放浪
        else
        {
            var action = PersonalActions.Resign;
            var args = action.Args(chara);
            if (action.CanDo(args))
            {
                await action.Do(args);
            }
            else
            {
                Debug.LogWarning($"{chara.Name} 放浪できず");
            }
        }

        return true;
    }
}