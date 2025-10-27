using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class AI
{
    public ValueTask Invest(Castle castle)
    {
        return default;
        //// 物資が余っていないなら何もしない。
        //if (castle.GoldSurplus < 0)
        //{
        //    return;
        //}
        //var actor = castle.Boss;
        //var args = core.StrategyActions.Invest.Args(actor);
        //var act = core.StrategyActions.Invest;

        //var budget = (castle.GoldIncome * 0.5f).Clamp(0, castle.Gold);
        //var count = 0;
        //var cost = act.Cost(args).castleGold;
        //while (act.CanDo(args))
        //{
        //    await act.Do(args);
        //    budget -= cost;
        //    count++;
        //    if (count > 10)
        //    {
        //        break;
        //    }
        //}
    }
}