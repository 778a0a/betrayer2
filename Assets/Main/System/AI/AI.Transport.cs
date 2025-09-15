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
    /// 輸送（上納）
    /// </summary>
    /// <param name="castle"></param>
    /// <param name="boss"></param>
    /// <returns></returns>
    public async ValueTask TransportAsTribute(Castle castle, Character boss)
    {
        var ruler = castle.Country.Ruler;
        Util.IsTrue(boss != ruler, "君主は上納できません。");

        // 赤字の場合は何もしない。
        if (castle.GoldBalance <= 0)
        {
            return;
        }

        // 黒字の場合は、現在の金残高の半分を上納する。
        var gold = castle.Gold / 2;
        if (gold > 1)
        {
            var act = core.StrategyActions.Transport;
            var args = act.Args(castle.Boss, castle, ruler.Castle, gold);
            if (act.CanDo(args))
            {
                await act.Do(args);
                Debug.LogWarning($"[輸送 - 上納] {castle.Boss.Name}が{ruler.Castle}へ{gold}G を輸送しました。");
            }
        }
    }

    /// <summary>
    /// 輸送（君主用）
    /// </summary>
    public async ValueTask TransportAsDistribution(Country country)
    {
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