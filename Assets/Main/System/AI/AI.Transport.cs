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
            // 物資が足りている城は対象外
            if (castle.Gold > 200) continue;
            // 赤字でない城は対象外
            if (castle.GoldBalance >= 0) continue;

            var wealthyCastles = country.Castles
                .Where(c => c != castle)
                .Where(c => c.GoldAmari > 20)
                .OrderByDescending(c => c.GoldAmari);

            var act = core.StrategyActions.Transport;
            foreach (var wealthy in wealthyCastles)
            {
                var needGold = 200 - castle.Gold;
                var gold = needGold.Clamp(0, wealthy.GoldAmari);
                if (gold > 0)
                {
                    var args = act.Args(country.Ruler, wealthy, castle, gold);
                    if (act.CanDo(args))
                    {
                        await act.Do(args);
                        Debug.LogError($"[輸送 - 補充] {wealthy.Boss.Name}が{castle.Name}へ{gold}G を輸送しました。");
                    }
                }
            }
        }
    }
}