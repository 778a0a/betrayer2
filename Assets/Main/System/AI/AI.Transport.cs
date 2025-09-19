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
    /// 輸送（君主用）
    /// </summary>
    public async ValueTask Transport(Character ruler)
    {
        var country = ruler.Country;

        // 物資が不足している城へ豊かな城から輸送する。
        foreach (var castle in country.Castles)
        {
            // 誰もいない場合は対象外
            if (castle.Boss == null) continue;
            // 物資が足りている城は対象外
            if (castle.Gold > 0) continue;

            var wealthyCastles = country.Castles
                .Where(c => c != castle && c.Boss != null)
                .Where(c => c.Gold > 0)
                .OrderByDescending(c => c.Gold);

            var act = core.StrategyActions.Transport;
            foreach (var wealthy in wealthyCastles)
            {
                var needGold = -castle.Gold;
                if (needGold <= 0) break;

                var gold = needGold.Clamp(0, wealthy.Gold);
                if (gold > 0)
                {
                    var args = act.Args(country.Ruler, wealthy, castle, gold);
                    if (act.CanDo(args))
                    {
                        await act.Do(args);
                        Debug.LogError($"[輸送 - 補充] {wealthy.Boss.Name}が{castle}へ{gold}G を輸送しました。");
                    }
                }
            }
        }
    }
}