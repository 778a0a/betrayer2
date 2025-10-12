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
            // 赤字でない城は対象外
            if (castle.GoldBalance >= 0) continue;
            // 物資が足りている城は対象外
            if (castle.Gold > 200 && castle.GoldAmari > 0) continue;

            var wealthyCastles = country.Castles
                .Where(c => c != castle)
                .Where(c => c.GoldAmari > 20)
                .OrderByDescending(c => c.GoldAmari);

            var act = core.StrategyActions.Transport;
            foreach (var wealthy in wealthyCastles)
            {
                var needGold = 200 - castle.GoldAmari;
                var gold = needGold.Clamp(0, wealthy.GoldAmari);
                if (gold > 0)
                {
                    var args = act.Args(country.Ruler, wealthy, castle, gold);
                    if (act.CanDo(args))
                    {
                        await act.Do(args);
                        Debug.LogError($"[輸送 - 補充] {wealthy.Boss?.Name ?? wealthy.Name}が{castle.Name}へ{gold}G を輸送しました。");
                    }
                }
            }
        }

        if (ruler.ActionPoints < 50)
        {
            Debug.LogError($"[輸送] 君主の行動力が50未満のため、輸送を終了します。{ruler.Name}, AP: {ruler.ActionPoints}");
            return;
        }

        // まだ物資が余っている城があれば、貧しい城へ輸送する。
        foreach (var castle in country.Castles)
        {
            if (castle.Gold < 800) continue;
            if (castle.GoldBalance < 50) continue;
            var poorCastles = country.Castles
                .Where(c => c != castle)
                .Where(c => c.GoldAmari < 300)
                .OrderBy(c => c.GoldAmari)
                .FirstOrDefault();
            if (poorCastles == null) continue;

            var act = core.StrategyActions.Transport;
            var needGold = 300 - poorCastles.GoldAmari;
            var gold = needGold.Clamp(0, castle.GoldAmari);
            if (gold > 0)
            {
                var args = act.Args(country.Ruler, castle, poorCastles, gold);
                if (act.CanDo(args))
                {
                    act.NeedPayCost = false; // コストは払わない。
                    await act.Do(args);
                    act.NeedPayCost = true;
                    Debug.LogError($"[輸送 - 調整] {castle.Boss?.Name ?? castle.Name}が{poorCastles.Name}へ{gold}G を輸送しました。");
                }
            }
        }
    }
}