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
    private async ValueTask OnCharacterMove(Character player)
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

        // 危険軍勢の対処を行う。
        if (GameDate.Day % 5 == 0)
        {
            foreach (var castle in World.Castles.Where(c => c.DangerForcesExists))
            {
                var dangerForces = castle.DangerForces(World.Forces).ToList();
                if (dangerForces.Count == 0)
                {
                    castle.DangerForcesExists = false;
                    continue;
                }
                var dangerPower = dangerForces.Sum(f => f.Character.Power);
                var defPower = castle.DefenceAndReinforcementPower(World.Forces);
                // 防衛兵力が十分なら何もしない。
                if (dangerPower < defPower) continue;

                // 隣接する城が危険でなければ、援軍を送る。
                var cands = castle.Neighbors
                    .Where(n => n.Country == castle.Country)
                    .Where(n => !n.DangerForcesExists)
                    .Where(n => n.Members.Count(m => m.IsDefendable) > 1)
                    .SelectMany(n => n.Members
                        .Where(m => m.IsDefendable)
                        .Select(m => (n, m, World.Forces.ETADays(m, n.Position, castle, ForceMode.Reinforcement))))
                    .ToList();
                // 援軍候補がない場合は何もしない。
                if (cands.Count == 0) continue;

                var dispatched = false;
                var maxETA = cands.Select(x => x.Item3).Max();
                Debug.LogWarning($"cands:\n{string.Join("\n", cands)}");
                while (dangerPower > defPower && cands.Count > 0)
                {
                    var target = cands.RandomPickWeighted(x => Mathf.Pow(10 + maxETA - x.Item3, 2), true);
                    var (neighbor, member, eta) = target;
                    var defendables = neighbor.Members.Count(m => m.IsDefendable);
                    if (defendables <= 1)
                    {
                        cands.Remove((neighbor, member, eta));
                        continue;
                    }
                    var action = CastleActions.MoveAsReinforcement;
                    var args = action.Args(castle.Country.Ruler, member, castle);
                    if (action.CanDo(args))
                    {
                        await action.Do(args);
                        defPower += member.Power;
                        Debug.LogWarning($"{member.Name}が{castle}へ援軍として出撃しました。");
                        dispatched = true;
                    }
                    cands.Remove(target);
                }
                if (dispatched)
                {
                    //Pause();
                }
                // まだ戦力が足りない場合は同盟国に援軍を要請する。
                if (dangerPower > defPower)
                {
                    castle.Neighbors.Where(n => n.Country.IsAlly(castle));
                }
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
                    // TODO 経済の仕組みを更新してから実装する
                    // 追放を行うか判定する。
                    // 町建設・城増築・投資を行うか判定する。
                    // 売買を行うか判定する。

                    // 採用を行うか判定する。
                    AI.HireVassal(castle);

                    // 進軍を行うか判定する。
                    AI.Deploy(castle);

                    // 後方から移動する（適当）
                    if (castle.Members.Count > 2 && castle.Neighbors.All(n => !castle.IsAttackable(n)))
                    {
                        if (castle.DangerForcesExists) continue;

                        var cands = castle.Members
                            .Where(m => m != chara)
                            .Where(m => m.IsDefendable)
                            .ToList();
                        var moveTarget = cands.RandomPickDefault();
                        var moveCastle = castle.Neighbors.Where(n => castle.IsSelf(n)).RandomPickDefault();
                        if (moveTarget != null && moveCastle != null && 0.5f.Chance())
                        {
                            var action = CastleActions.Move;
                            var args = action.Args(chara, moveTarget, moveCastle);
                            await action.Do(args);
                        }
                    }

                    // 挑発を行うか判定する。

                    // 君主でない場合反乱を起こすか判定する。
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
                if (chara.IsMoving || chara.IsIncapacitated) continue;

                args.actor = chara;

                var action = default(ActionBase);
                if (chara.IsFree)
                {
                    args.targetCastle = null;
                    args.targetTown = null;
                    action = CastleActions.TrainSoldiers;
                }
                else
                {
                    args.targetCastle = chara.Castle;
                    args.targetTown = args.targetCastle?.Towns.RandomPick();
                    action = vassalActions.Value.RandomPick();
                }
                if (chara.Soldiers.HasEmptySlot)
                {
                    action = CastleActions.HireSoldier;
                }

                var budget = Math.Min(chara.Gold, Math.Max(chara.Gold - chara.Salary, 0) + chara.Salary / 6);
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

    public void Pause()
    {
        test.hold = true;
    }

    private readonly Lazy<ActionBase[]> vassalActions = new(() => new ActionBase[]
    {
        Instance.TownActions.ImproveGoldIncome,
        Instance.TownActions.ImproveFoodIncome,
        Instance.CastleActions.ImproveCastleStrength,
        Instance.CastleActions.TrainSoldiers,
    });
}