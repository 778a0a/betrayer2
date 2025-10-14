using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

partial class AI
{
    public async Task DoStrategyAction(Character chara)
    {
        var country = chara.Country;
        var castle = chara.Castle;

        // 君主の場合
        if (chara.IsRuler)
        {
            // 赤字の場合は不要な人員を解雇する。
            await FireVassal(chara);

            // 四半期ごとの行動がまだなら行う。
            if (!country.QuarterActionDone)
            {
                country.QuarterActionDone = true;

                // 物資を輸送する。
                await Transport(chara);

                // 褒賞を与える。
                await Bonus(country);
            }

            // 人員を移動させる。
            await MoveVassal(chara);

            // 外交を行う。
            await Diplomacy(country);
        }
        // 城主の場合
        else
        {
            // 低確率で反乱を起こす。
            var betrayed = await BetrayOnStrategyPhase(chara);
            if (betrayed)
            {
                // 反乱を起こしたら行動終了。
                return;
            }
        }

        // 四半期ごとの行動がまだなら行う。
        if (!castle.QuarterActionDone)
        {
            castle.QuarterActionDone = true;

            // 褒賞を与える。
            await BonusFromBoss(chara);

            // 採用を行う。
            await HireVassal(chara);

            // 投資を行う。
            await Invest(castle);
        }

        // 防衛
        await Defence(castle);
        // 同盟国への救援
        await DefenceForAlly(castle);
        // 進軍を行うか判定する。
        await Deploy(castle);
    }
}
