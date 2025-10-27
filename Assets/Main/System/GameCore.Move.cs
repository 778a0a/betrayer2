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
        // ロード処理の都合でプレーヤーは最初に処理する。
        var bosses = World.Castles
            .Select(c => c.Boss)
            .Where(b => b != null && b.StrategyActionGauge >= 100)
            .Shuffle()
            .OrderByDescending(b => b.IsPlayer);
        foreach (var boss in bosses)
        {
            await DoStrategyAction(boss);
            boss.StrategyActionGauge = 0;
        }

        // 個人フェイズの処理を行う。
        // ロード処理の都合でプレーヤーは最初に処理する。
        var charas = World.Characters
            .Where(c => c.PersonalActionGauge >= 100)
            .Shuffle()
            .OrderByDescending(c => c.IsPlayer);
        foreach (var chara in charas)
        {
            await DoPersonalAction(chara);
            chara.PersonalActionGauge = 0;
        }
    }

    /// <summary>
    /// 個人フェイズを実行します。
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
        MainUI.ActionScreen.ActivatePhase(chara, Phase.Personal);
        AutoSaveForPhaseStart(Phase.Personal);
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
        MainUI.ActionScreen.ActivatePhase(chara, Phase.Strategy);
        AutoSaveForPhaseStart(Phase.Strategy);
        await Booter.HoldIfNeeded();
    }

    private void AutoSaveForPhaseStart(Phase phase)
    {
        if (IsRestoring) return;
        if (SystemSetting.Instance.AutoSaveFrequency != AutoSaveFrequency.EveryPhase) return;

        // 重いので非同期で実行する。
        Booter.StartCoroutine(AutoSaveCoroutine());
        IEnumerator AutoSaveCoroutine()
        {
            yield return null; // 1フレーム待機してから実行する。
            Debug.Log("オートセーブを実行します。");
            SaveDataManager.Instance.Save(SaveDataManager.AutoSaveDataSlotNo, this, phase);
        }
    }

    private void AutoSaveForPeriods()
    {
        if (IsRestoring) return;
        var frequency = SystemSetting.Instance.AutoSaveFrequency;
        var date = World.GameDate;
        var shouldSave = false;
        switch (frequency)
        {
            case AutoSaveFrequency.EveryTwoYears:
                shouldSave = date.Year % 2 == 0 && date.Month == 1 && date.Day == 1;
                break;
            case AutoSaveFrequency.EveryYear:
                shouldSave = date.Month == 1 && date.Day == 1;
                break;
            case AutoSaveFrequency.EverySixMonths:
                shouldSave = (date.Month == 1 || date.Month == 7) && date.Day == 1;
                break;
            case AutoSaveFrequency.EveryThreeMonths:
                shouldSave = (date.Month - 1) % 3 == 0 && date.Day == 1;
                break;
            default:
                shouldSave = false;
                break;
        }
        if (!shouldSave) return;
        
        // ゲーム開始直後は除外する。
        if (date.IsGameFirstDay) return;

        Debug.Log("オートセーブを実行します。");
        SaveDataManager.Instance.Save(SaveDataManager.AutoSaveDataSlotNo, this, Phase.Progress);
    }

    public void Pause()
    {
        Booter.hold = true;
    }
}