using System;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using System.Linq;
using System.Buffers;

partial class GameCore
{
    /// <summary>
    /// キャラクターの行動を行う。
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private async ValueTask OnCharacterMove(Character player)
    {
        // 戦略フェイズの処理を行う。
        var bosses = World.Castles
            .Select(c => c.Boss)
            .Where(b => b != null && b.StrategyActionGauge >= 100)
            .Shuffle();
        foreach (var boss in bosses)
        {
            boss.StrategyActionGauge = 0;
            await DoStrategyAction(boss);
        }

        // 個人フェイズの処理を行う。
        foreach (var chara in World.Characters.Where(c => c.PersonalActionGauge >= 100).Shuffle())
        {
            chara.PersonalActionGauge = 0;
            await DoPersonalAction(chara);
        }
    }

    /// <summary>
    /// 個人フェーズを実行します。
    /// </summary>
    private async ValueTask DoPersonalAction(Character chara)
    {
        // AIの場合
        if (!chara.IsPlayer)
        {
            await AI.DoPersonalAction(chara);
            return;
        }

        // プレーヤーの場合
        Pause();
        MainUI.ActionScreen.Show(chara, personalPhase: true);
        await Booter.HoldIfNeeded();
    }

    /// <summary>
    /// 戦略行動を行う。
    /// </summary>
    private async ValueTask DoStrategyAction(Character chara)
    {
        // AIの場合
        if (!chara.IsPlayer)
        {
            await AI.DoStrategyAction(chara);
            return;
        }

        // プレーヤーの場合
        Pause();
        MainUI.ActionScreen.Show(chara);
        await Booter.HoldIfNeeded();
    }

    public void Pause()
    {
        Booter.hold = true;
    }
}