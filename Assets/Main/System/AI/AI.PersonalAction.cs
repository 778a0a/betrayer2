using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

partial class AI
{
    public async ValueTask DoPersonalAction(Character chara)
    {
        // 未所属の場合
        if (chara.IsFree)
        {
            // ランダムに拠点を移動する。
            if (0.2f.Chance())
            {
                var oldCastle = chara.Castle;
                var newCastle = oldCastle.Neighbors.RandomPick();
                chara.ChangeCastle(newCastle, true);
                //Debug.Log($"{chara.Name}が{oldCastle}から{newCastle}に移動しました。");
            }

            // TODO 奪取を行う。

            // 所持金が無くなるまで雇兵・訓練を行う。
            var args = new ActionArgs() { actor = chara };
            while (true)
            {
                var action = (ActionBase)(chara.Soldiers.HasEmptySlot ?
                    PersonalActions.HireSoldier :
                    PersonalActions.TrainSoldiers);
                if (!action.CanDo(args)) break;
                await action.Do(args);
            }
        }
        // 所属ありの場合
        else
        {
            // TODO 反乱を起こすか判定する。

            // 出撃中の場合
            if (chara.IsMoving)
            {
                var force = chara.Force;

                // 救援モードの場合
                if (force.Mode == ForceMode.Reinforcement)
                {
                    // 救援が完了した場合は帰還する。
                    if (force.Destination.Position == force.Position &&
                        force.ReinforcementWaitDays <= 0)
                    {
                        Debug.Log($"軍勢 救援が完了したため帰還します。 {force}");
                        await PersonalActions.BackToCastle.Do(new(chara));
                        return;
                    }

                    // 救援先が危険でなくなったら本拠地に戻る。
                    var home = chara.Castle;
                    var targetCastle = (Castle)force.Destination;
                    if (!targetCastle.DangerForcesExists && targetCastle != home)
                    {
                        Debug.Log($"軍勢 救援先が危険でなくなったため帰還します。 {force}");
                        await PersonalActions.BackToCastle.Do(new(chara));
                        return;
                    }
                }

                return;
            }

            // 後方から移動する（適当）TODO
            var castle = chara.Castle;
            var isSafe = castle.Neighbors.All(n => !castle.IsAttackable(n)) && !castle.DangerForcesExists;
            if (isSafe && castle.Members.Count > 2)
            {
                var cands = castle.Members
                    .Where(m => m != chara)
                    .Where(m => m.IsDefendable)
                    .ToList();
                var moveTarget = cands.RandomPickDefault();
                var moveCastle = castle.Neighbors.Where(n => castle.IsSelf(n)).RandomPickDefault();
                if (moveTarget != null && moveCastle != null && 0.5f.Chance())
                {
                    var move = StrategyActions.Deploy;
                    var moveArgs = move.Args(chara, moveTarget, moveCastle);
                    await move.Do(moveArgs);
                }
            }

            var args = new ActionArgs();
            args.actor = chara;
            args.targetCastle = chara.Castle;

            // 所持金がなくなるまでアクションを実行する。
            while (chara.Gold > 0)
            {
                var action = SelectPersonalAction(chara, args);
                if (action == null || !action.CanDo(args)) break;
                await action.Do(args);
            }
        }
    }

        /// <summary>
    /// 個人アクションを選択します。
    /// </summary>
    private ActionBase SelectPersonalAction(Character chara, ActionArgs args)
    {
        // 空きスロットがあれば雇兵する。
        if (chara.Soldiers.HasEmptySlot)
        {
            return PersonalActions.HireSoldier;
        }

        // 忠実なら80%の確率で方針通りの行動を行う。
        var prob = chara.Fealty.MinWith(chara.Ambition) / 10f - 0.2f;
        if (prob.Chance())
        {
            switch (chara.Castle.Objective)
            {
                case CastleObjective.Fortify:
                    return PersonalActions.Fortify;
                case CastleObjective.Develop:
                case CastleObjective.Transport:
                    var investProb = Mathf.Pow(chara.Castle.GoldIncomeProgress, 2);
                    return investProb.Chance() ? PersonalActions.Invest : PersonalActions.Develop;
                case CastleObjective.Train:
                case CastleObjective.Attack:
                    return PersonalActions.TrainSoldiers;
                case CastleObjective.None:
                default:
                    break;
            }
        }
        // ランダムに行動を選ぶ。
        return vassalActions.Value.Where(a => a.CanDo(args)).RandomPickDefault();
    }

    /// <summary>
    /// CPUが通常実行するアクション
    /// </summary>
    private readonly Lazy<ActionBase[]> vassalActions = new(() => new ActionBase[]
    {
        GameCore.Instance.PersonalActions.Develop,
        GameCore.Instance.PersonalActions.Fortify,
        GameCore.Instance.PersonalActions.TrainSoldiers,
        GameCore.Instance.PersonalActions.Invest,
    });
}
