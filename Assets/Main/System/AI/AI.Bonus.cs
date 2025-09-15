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
    /// 忠誠の低いメンバーに褒賞を与える。
    /// </summary>
    /// <param name="castle"></param>
    public async ValueTask Bonus(Castle castle)
    {
        // TODO
        //// 実行可能なら褒賞を与えて忠誠を上げる。
        //var act = core.CastleActions.Bonus;
        //var args = act.Args(castle.Boss, target);
        //if (act.CanDo(args))
        //{
        //    await act.Do(args);
        //}
    }

    /// <summary>
    /// 君主用 忠誠の低いメンバーに褒賞を与える。
    /// </summary>
    public async ValueTask BonusFromRuler(Country country)
    {
        // TODO
        //throw new NotImplementedException();
    }
}