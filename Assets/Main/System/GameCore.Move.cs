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
                chara.ChangeCastle(newCastle, true);
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
                    foreach (var castle in country.Castles)
                    {
                        // 各城の方針を設定する。
                        var prevObjective = castle.Objective;
                        castle.Objective = AI.SelectCastleObjective(chara, castle);
                        // TODO 攻撃の場合は目標城を設定する。
                        if (prevObjective != castle.Objective)
                        {
                            //Debug.Log($"方針: 更新 {castle.Objective} <- {prevObjective} at {castle}");
                        }
                        else
                        {
                            //Debug.Log($"方針継続: {castle.Objective} at {castle}");
                        }

                        // 物資が不足しているなら余っている城から補給する。
                    }
                    foreach (var castle in country.Castles)
                    {
                        // 物資が余っているなら開発度の強化を行う。
                        if (castle.GoldIncomeProgress > 0.75f && castle.FoodIncomeProgress > 0.75f)
                        {
                            var goldSurplus = castle.GoldSurplus;
                            var foodSurplus = castle.FoodSurplus;
                            if (goldSurplus > 0 && foodSurplus > 0)
                            {
                                var act = default(ActionBase);
                                // 開発
                                var act1 = CastleActions.Develop;
                                act = act1;
                                var args = act1.Args(chara, castle);
                                // 町建設
                                var cands = castle.NewTownCandidates(World).ToList();
                                if (cands.Count > 0)
                                {
                                    var act2 = CastleActions.BuildTown;
                                    var goodCand = cands.OrderByDescending(c =>
                                        World.Economy.GetGoldAmount(Town.TileFoodMax(c)) +
                                        Town.TileGoldMax(c))
                                        .First();
                                    var args2 = act2.Args(chara, castle, goodCand.Position);
                                    
                                    var cost1 = act1.Cost(args);
                                    var cost2 = act2.Cost(args2);
                                    if (cost1.castleGold > cost2.castleGold)
                                    {
                                        act = act2;
                                        args = args2;
                                    }
                                }

                                if (act.CanDo(args))
                                {
                                    await act.Do(args);
                                    Debug.LogError($"{castle}の開発を行いました。({act})");
                                    Pause();
                                }
                            }
                        }

                        // 城塞レベルの強化を行う。
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

                void ProcessRainforcements(Country reinSourceCountry)
                {
                    var isAlly = castle.Country != reinSourceCountry;
                    // 隣接する城が危険でなければ、援軍を送る。
                    // 救援出撃して戦闘せず帰還している軍勢があれば優先して向かわせる。
                    var candForces = castle.Neighbors
                        .Where(n => n.Country == reinSourceCountry)
                        .Where(n => !n.DangerForcesExists)
                        .SelectMany(n => n.Members)
                        // 帰還中
                        .Where(m =>
                            m.IsMoving &&
                            m.Force.Mode == ForceMode.Reinforcement &&
                            m.Force.Destination.Position == m.Castle.Position)
                        // 損耗が少ない
                        .Where(m => m.Soldiers.AttritionRate < 0.5f)
                        // 救援先の近くにいる。
                        .Where(m =>
                            World.Forces.ETADays(m, m.Force.Position, castle, ForceMode.Reinforcement) <
                            World.Forces.ETADays(m, m.Castle.Position, castle, ForceMode.Reinforcement))
                        .ToList();
                    foreach (var f in candForces)
                    {
                        Debug.LogError($"救援帰還中の{f.Name}が{castle}へ援軍として転向します。{(isAlly ? "(同盟国)" :"")}");
                        f.Force.ReinforcementOriginalTarget = castle;
                        f.Force.SetDestination(castle);
                        f.Force.ReinforcementWaitDays = 90;
                        defPower += f.Power;
                    }
                }

                async Task ProcessDefendables(Country reinSourceCountry)
                {
                    var isAlly = castle.Country != reinSourceCountry;
                    // 城内にいるキャラ
                    var cands = castle.Neighbors
                        .Where(n => n.Country == reinSourceCountry)
                        .Where(n => !n.DangerForcesExists)
                        .Where(n => n.Members.Count(m => m.IsDefendable) > 1)
                        .SelectMany(n => n.Members)
                        .Where(m => m.IsDefendable)
                        .Select(m => (chara: m, eta: World.Forces.ETADays(m, m.Castle.Position, castle, ForceMode.Reinforcement)))
                        .ToList();
                    // 援軍候補がない場合は何もしない。
                    if (cands.Count == 0) return;

                    var maxETA = cands.Select(x => x.eta).Max();
                    //Debug.LogWarning($"cands:\n{string.Join("\n", cands)}");
                    while (dangerPower > defPower && cands.Count > 0)
                    {
                        var target = cands.RandomPickWeighted(x => Mathf.Pow(10 + maxETA - x.eta, 2), true);
                        var (member, eta) = target;
                        var defendables = member.Castle.Members.Count(m => m.IsDefendable);
                        if (defendables <= 1)
                        {
                            cands.Remove(target);
                            continue;
                        }
                        var action = CastleActions.MoveAsReinforcement;
                        var args = action.Args(reinSourceCountry.Ruler, member, castle);
                        if (action.CanDo(args))
                        {
                            await action.Do(args);
                            defPower += member.Power;
                            Debug.LogWarning($"{member.Name}が{castle}へ援軍として出撃しました。{(isAlly ? "(同盟国)" : "")}");
                        }
                        cands.Remove(target);
                    }
                }

                // 救援から帰還中の軍勢
                ProcessRainforcements(castle.Country);
                if (dangerPower < defPower) continue;
                // 駐在中のキャラ
                await ProcessDefendables(castle.Country);
                if (dangerPower < defPower) continue;
                
                // まだ足りない場合は同盟国から援軍を出す。
                var allyCountries = castle.Country.Neighbors
                    .Where(n => n.IsAlly(castle.Country))
                    .Distinct()
                    // ただし危険軍勢がすべて同盟国の同盟国なら出さない。
                    .Where(n => !dangerForces.All(d => n.IsAlly(d.Country)))
                    .ToList();
                foreach (var ally in allyCountries)
                {
                    if (dangerPower < defPower) break;
                    ProcessRainforcements(ally);
                }
                foreach (var ally in allyCountries)
                {
                    if (dangerPower < defPower) break;
                    await ProcessDefendables(ally);
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

                    // 食糧不足・借金の解消を行う。
                    AI.TradeNeeds(castle);

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
                    // 基本的には方針通りの行動を行う。
                    if (0.75f.Chance())
                    {
                        switch (chara.Castle.Objective)
                        {
                            case CastleObjective.CastleStrength:
                                action = CastleActions.ImproveCastleStrength;
                                break;
                            case CastleObjective.Stability:
                                action = CastleActions.ImproveStability;
                                break;
                            case CastleObjective.Commerce:
                                action = TownActions.ImproveGoldIncome;
                                break;
                            case CastleObjective.Agriculture:
                                action = TownActions.ImproveFoodIncome;
                                break;
                            case CastleObjective.None:
                            case CastleObjective.Attack:
                            case CastleObjective.Train:
                            default:
                                break;
                        }
                    }
                    // アクション未選択か、選択したアクションが実行不可ならランダムに行動する。
                    if (!action?.CanDo(args) ?? true)
                    {
                        action ??= vassalActions.Value.Where(a => a.CanDo(args)).RandomPickDefault();
                    }
                }
                if (chara.Soldiers.HasEmptySlot)
                {
                    // 危険軍勢がいるか、食料収支がプラスなら兵士を採用する。
                    if (chara.Castle.DangerForcesExists || chara.Castle.FoodBalance > 200)
                    {
                        action = CastleActions.HireSoldier;
                    }
                }

                var budget = Math.Min(chara.Gold, Math.Max(chara.Gold - chara.Salary, 0) + chara.Salary / 6);
                if (action == CastleActions.HireSoldier)
                {
                    budget = chara.Gold;
                }

                // できることがないなら何もしない。
                if (action == null)
                {
                    Debug.LogWarning($"{chara.Name} は行動できません。");
                    continue;
                }

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
        Instance.CastleActions.ImproveStability,
        Instance.CastleActions.TrainSoldiers,
    });
}