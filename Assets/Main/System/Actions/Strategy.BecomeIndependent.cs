using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;
using RebelAction = PersonalActions.RebelAction;

partial class StrategyActions
{
    /// <summary>
    /// 独立します。
    /// </summary>
    public BecomeIndependentAction BecomeIndependent { get; } = new();
    public class BecomeIndependentAction : StrategyActionBase
    {
        public override string Label => L["独立"];
        public override string Description => L["独立します。"];
        protected override ActionRequirements Requirements => ActionRequirements.BossNotRuler;

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 10, 0);

        public ActionArgs Args(Character chara)
        {
            return new ActionArgs(chara);
        }

        public bool IsCancelled { get; set; }

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            IsCancelled = false;

            var actor = args.actor;
            if (actor.IsPlayer)
            {
                // 確認する。
                var ok = await MessageWindow.ShowOkCancel("本当に独立しますか？");
                if (!ok)
                {
                    IsCancelled = true;
                    return;
                }
            }
            PayCost(args);

            // 同じ城のメンバーが反乱に参加するかを判定する。
            var betrayers = new List<Character> { actor };
            var opponents = actor.Castle.Members.Where(m => m != actor).ToList();
            var asked = false;
            foreach (var member in opponents.ToList())
            {
                // プレーヤーの場合は確認する。
                if (member.IsPlayer)
                {
                    var ok = await MessageWindow.ShowYesNo($"{actor.Name}が独立しようとしています。\n新勢力に参加しますか？");
                    if (ok)
                    {
                        betrayers.Add(member);
                        opponents.Remove(member);
                    }
                    asked = true;
                    continue;
                }

                var prob = RebelAction.BetrayalProbability(actor, member);
                Debug.Log($"独立参加判定 {member.Name} | 忠誠 {member.Loyalty:0} | 参加確率 {prob:0.00}");
                if (prob.Chance())
                {
                    betrayers.Add(member);
                    opponents.Remove(member);
                }
            }

            Debug.LogWarning($"{actor.Castle.Name}で独立発生" +
                $"独立側: {string.Join(", ", betrayers.Select(b => b.Name))} | " +
                $"祖国側: {string.Join(", ", opponents.Select(o => o.Name))}");

            // メッセージを表示する。他国でも通知を受け取る。
            if (!asked)
            {
                await MessageWindow.Show($"{actor.Name}が{actor.Country.Ruler.Name}から独立しました！");
            }

            var oldCountry = actor.Country;
            var newCountry = RebelAction.CreateNewCountry(actor, World);

            // 城を奪取する。
            await RebelAction.IndependenceSucceeded(actor.Castle, betrayers, newCountry, oldCountry, World);

            // 元の国のキャラの忠誠を最大5下げる。
            foreach (var chara in oldCountry.Members)
            {
                if (chara.IsRuler) continue;
                chara.Loyalty = (chara.Loyalty - Random.Range(1, 5)).MinWith(0);
            }
            // 反乱側のキャラの忠誠を5上げる。
            foreach (var chara in newCountry.Members)
            {
                if (chara.IsRuler) continue;
                chara.Loyalty = (chara.Loyalty + 5).MaxWith(110);
            }

            // 他の城主について、どちらの勢力につくか判定する。
            foreach (var otherCastle in oldCountry.Castles.Where(c => !c.Boss?.IsRuler ?? false).ToList())
            {
                var askedOnCastle = false;
                var betray = false;
                // プレーヤーの場合は確認する。
                if (otherCastle.Boss.IsPlayer)
                {
                    betray = await MessageWindow.ShowYesNo($"{actor.Name}の勢力に参加しますか？");
                    askedOnCastle = true;
                }
                else
                {
                    var prob = RebelAction.BetrayalProbability(actor, otherCastle.Boss);
                    betray = prob.Chance();
                    Debug.Log($"他城独立参加判定 {otherCastle} | 忠誠 {otherCastle.Boss.Loyalty:0} | 参加確率 {prob:0.00} | 参加 {betray}");
                }
                // 裏切らない場合はスキップ
                if (!betray) continue;

                // 独立側につく場合
                // プレーヤーの場合は帰順を許可するか確認する。
                var country = newCountry;
                var actorOnCastle = actor;
                if (actor.IsPlayer)
                {
                    var ok = await MessageWindow.ShowYesNo($"{otherCastle.Name}の城主{otherCastle.Boss.Name}が帰順を申し入れてきました。\n受け入れますか？");
                    // 拒否された場合は単独で独立する。
                    if (!ok)
                    {
                        country = RebelAction.CreateNewCountry(otherCastle.Boss, World);
                        actorOnCastle = otherCastle.Boss;
                    }
                    askedOnCastle = true;
                }

                var betrayersOnCastle = new List<Character> { otherCastle.Boss };
                var opponentsOnCastle = otherCastle.Members.Where(m => m != otherCastle.Boss).ToList();
                foreach (var member in opponentsOnCastle.ToList())
                {
                    // プレーヤーの場合は確認する。
                    if (member.IsPlayer)
                    {
                        var ok = await MessageWindow.ShowYesNo($"{otherCastle.Boss.Name}が独立勢力に参加しようとしています。\n独立勢力に参加しますか？");
                        if (ok)
                        {
                            betrayersOnCastle.Add(member);
                            opponentsOnCastle.Remove(member);
                        }
                        askedOnCastle = true;
                        continue;
                    }
                    var prob = RebelAction.BetrayalProbability(actorOnCastle, member);
                    Debug.Log($"他城独立参加判定 {member.Name} | 忠誠 {member.Loyalty:0} | 参加確率 {prob:0.00}");
                    if (prob.Chance())
                    {
                        betrayersOnCastle.Add(member);
                        opponentsOnCastle.Remove(member);
                    }
                }
                Debug.LogWarning($"{otherCastle.Name}が連鎖的に独立しました。" +
                    $"独立側: {string.Join(", ", betrayersOnCastle.Select(b => b.Name))} | " +
                    $"祖国側: {string.Join(", ", opponentsOnCastle.Select(o => o.Name))}");

                await RebelAction.IndependenceSucceeded(otherCastle, betrayersOnCastle, country, oldCountry, World);

                // 帰順したキャラの忠誠を5上げる。
                foreach (var chara in otherCastle.Members)
                {
                    if (chara.IsRuler) continue;
                    chara.Loyalty = (chara.Loyalty + 5).MaxWith(110);
                }

                if (!askedOnCastle)
                {
                    if (country == newCountry)
                    {
                        await MessageWindow.Show($"{otherCastle.Name}の城主{otherCastle.Boss.Name}が\n{actor.Name}に寝返りました！");
                    }
                    else
                    {
                        await MessageWindow.Show($"{otherCastle.Name}の城主{otherCastle.Boss.Name}が\n連鎖的に独立しました！");
                    }

                }
            }

            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"独立成功！新しい君主になりました。");
            }
        }
    }

}