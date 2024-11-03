using System;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using System.Linq;

partial class GameCore
{
    /// <summary>
    /// キャラクターの行動を行う。
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private async Awaitable OnCharacterMove(Character player)
    {
        // 収入月の場合は未所属キャラを移動させる。
        if (GameDate.IsIncomeMonth && GameDate.Day == 1)
        {
            foreach (var chara in World.Characters)
            {
                if (chara == player) continue;
                if (!chara.IsFree) continue;

                // ランダムに拠点を移動する。
                var oldCastle = chara.Castle;
                var newCastle = oldCastle.Neighbors.RandomPick();
                oldCastle.Frees.Remove(chara);
                newCastle.Frees.Add(chara);
                //Debug.Log($"{chara.Name}が{oldCastle}から{newCastle}に移動しました。");
            }
        }

        // 君主の月毎アクションを行う。
        if (GameDate.Day == 1)
        {
            foreach (var country in World.Countries)
            {
                var chara = country.Ruler;
                if (chara == player) continue;

                // 収入月の場合
                if (GameDate.IsIncomeMonth)
                {
                    // 各城の方針を設定する。
                    foreach (var castle in country.Castles)
                    {
                        var prevObjective = castle.Objective;
                        castle.Objective = AI.SelectCastleObjective(chara, castle);
                        if (prevObjective != castle.Objective)
                        {
                            //Debug.Log($"方針: 更新 {castle.Objective} <- {prevObjective} at {castle}");
                        }
                        else
                        {
                            //Debug.Log($"方針継続: {castle.Objective} at {castle}");
                        }
                    }
                }

                // 外交を行う。
                await AI.Diplomacy(country);
            }
        }

        // 各キャラの月毎アクションを行う。
        // プレーヤー君主の行動を反映させるため、2日目に行う。
        if (GameDate.Day == 2)
        {
            foreach (var chara in World.Characters)
            {
                if (chara == player) continue;
                if (chara.IsFree) continue;

                // 所属ありの場合
                var castle = chara.Castle;

                // 城主の場合（君主も含む）
                if (castle.Boss == chara)
                {
                    // 採用を行うか判定する。
                    // 追放を行うか判定する。
                    // 町建設・城増築・投資を行うか判定する。
                    // 君主でない場合反乱を起こすか判定する。
                    // 進軍を行うか判定する。
                    // 売買を行うか判定する。
                    // 挑発を行うか判定する。
                    // 反乱を起こすか判定する。
                }
                // 配下の場合
                else
                {
                    // 反乱を起こすか判定する。
                }
            }
        }

        // 15日毎に行動を行う。
        if (GameDate.Day == 15 || GameDate.Day == 30)
        {
            // 収入の1/6分、農業・商業・築城・訓練をランダムに行う。
            var args = new ActionArgs();
            foreach (var chara in World.Characters)
            {
                if (chara == player) continue;
                if (chara.IsMoving) continue;

                args.Actor = chara;

                var action = default(ActionBase);
                if (chara.IsFree)
                {
                    args.TargetCastle = null;
                    args.TargetTown = null;
                    action = CastleActions.TrainSoldiers;
                }
                else
                {
                    args.TargetCastle = chara.Castle;
                    args.TargetTown = args.TargetCastle?.Towns.RandomPick();
                    action = vassalActions.Value.RandomPick();
                }

                var budget = Math.Min(chara.Gold, chara.Salary / 6);
                do
                {
                    if (!action.CanDo(args)) break;
                    budget -= action.Cost(args).actorGold;
                    await action.Do(args);
                }
                while (budget > 0);
            }
        }
    }

    private readonly Lazy<ActionBase[]> vassalActions = new(() => new ActionBase[]
    {
        Instance.TownActions.ImproveGoldIncome,
        Instance.TownActions.ImproveFoodIncome,
        Instance.CastleActions.ImproveCastleStrength,
        Instance.CastleActions.TrainSoldiers,
    });
}