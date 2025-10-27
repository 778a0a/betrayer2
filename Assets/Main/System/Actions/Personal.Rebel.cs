using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class PersonalActions
{
    /// <summary>
    /// 反乱を起こします。
    /// </summary>
    public RebelAction Rebel { get; } = new();
    public class RebelAction : PersonalActionBase
    {
        public override string Label => L["反乱"];
        public override string Description => L["反乱を起こします。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMovingAndVassalNotBoss;

        public int GoldCost => 5;
        public override ActionCost Cost(ActionArgs args) => GoldCost;

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
                var ok = await MessageWindow.ShowOkCancel("本当に反乱を起こしますか？");
                if (!ok)
                {
                    IsCancelled = true;
                    return;
                }
            }
            PayCost(args);

            // 同じ城のメンバーが反乱に参加するかを判定する。
            var betrayers = new List<Character> { actor };
            var opponents = actor.Castle.Members.Where(m => !m.IsMoving && m != actor).ToList();
            var movings = actor.Castle.Members.Except(opponents).Except(betrayers).ToList();
            var asked = false;
            foreach (var member in opponents.ToList())
            {
                if (member == actor) continue;
                if (member.IsRuler) continue;

                // プレーヤーの場合は確認する。
                if (member.IsPlayer)
                {
                    var ok = await MessageWindow.ShowYesNo($"{actor.Name}が反乱を起こしました！\n反乱に参加しますか？");
                    if (ok)
                    {
                        betrayers.Add(member);
                        opponents.Remove(member);
                    }
                    asked = true;
                    continue;
                }

                var prob = BetrayalProbability(actor, member);
                Debug.Log($"反乱参加判定 {member.Name} | 忠誠 {member.Loyalty:0} | 参加確率 {prob:0.00}");
                if (prob.Chance())
                {
                    betrayers.Add(member);
                    opponents.Remove(member);
                }
            }

            Debug.LogWarning($"{actor.Castle.Name}で反乱発生" +
                $"反乱側: {string.Join(", ", betrayers.Select(b => b.Name))} | " +
                $"鎮圧側: {string.Join(", ", opponents.Select(o => o.Name))}");

            // メッセージを表示する。他国でも通知を受け取る。
            if (!asked)
            {
                var betrayerNames = string.Join(", ", betrayers.Select((b, i) => i % 6 == 0 ? "\n" + b.Name : b.Name)).Trim();
                var opponentNames = string.Join(", ", opponents.Select((o, i) => i % 6 == 0 ? "\n" + o.Name : o.Name)).Trim();

                await MessageWindow.Show(
                    $"{actor.Castle.Name}で{actor.Name}が\n" +
                    $"{actor.Country.Ruler.Name}に対して反乱を起こしました！\n" +
                    $"\n反乱側: {betrayers.Count}人\n{betrayerNames}\n\n鎮圧側: {opponents.Count}人\n{opponentNames}");
            }

            var oldCountry = actor.Country;
            Country newCountry = CreateNewCountry(actor, World);

            // 反乱成功なら城を奪取する。
            if (betrayers.Sum(m => m.Power) >= opponents.Sum(m => m.Power))
            {
                // 出撃中のキャラをどちらに所属させるか判定する。
                foreach (var chara in movings)
                {
                    if (chara.IsRuler) continue;
                    if (chara.IsPlayer)
                    {
                        var ok = await MessageWindow.ShowYesNo($"反乱は成功し、{actor.Name}が城を奪取しました。\n反乱側に加わりますか？");
                        if (ok)
                        {
                            betrayers.Add(chara);
                        }
                        else
                        {
                            opponents.Add(chara);
                        }
                        continue;
                    }

                    // 忠誠95以上または忠誠90以上で忠実さが8以上なら旧国側に残る。
                    if (chara.Loyalty >= 95 || (chara.Loyalty >= 90 && chara.Fealty >= 8))
                    {
                        opponents.Add(chara);
                    }
                    else
                    {
                        betrayers.Add(chara);
                    }
                }

                // 反乱成功の処理を行う。
                await IndependenceSucceeded(actor.Castle, betrayers, newCountry, oldCountry, World);

                // 元の国のキャラの忠誠を5下げる。
                foreach (var chara in oldCountry.Members)
                {
                    if (chara.IsRuler) continue;
                    chara.Loyalty = (chara.Loyalty - 5).MinWith(0);
                }
                // 反乱側のキャラの忠誠を5上げる。
                foreach (var chara in newCountry.Members)
                {
                    if (chara.IsRuler) continue;
                    chara.Loyalty = (chara.Loyalty + 5).MaxWith(110);
                }

                if (actor.IsPlayer)
                {
                    await MessageWindow.Show($"反乱成功！新しい君主になりました。");
                }
            }
            // 反乱失敗なら、反乱側は勢力を追放される。
            else
            {
                World.Countries.Remove(newCountry);
                foreach (var chara in betrayers)
                {
                    chara.ChangeCastle(chara.Castle, true);
                    chara.Contribution /= 2;
                    chara.IsImportant = false;
                    chara.OrderIndex = -1;
                    chara.Loyalty = 0;

                    if (chara.IsPlayer)
                    {
                        await MessageWindow.Show($"反乱は失敗し、勢力を追放されました。");
                    }
                }
                
                // 城のキャラの忠誠を5下げる。
                foreach (var chara in actor.Castle.Members)
                {
                    if (chara.IsRuler) continue;
                    chara.Loyalty = (chara.Loyalty - 5).MinWith(0);
                }
            }
        }

        public static async ValueTask IndependenceSucceeded(
            Castle castle,
            List<Character> betrayers,
            Country newCountry,
            Country oldCountry,
            WorldData world)
        {
            // 所属を変更する。
            foreach (var chara in betrayers)
            {
                chara.Country = newCountry;
                if (chara.Force != null)
                {
                    chara.Force.Country = chara.Country;
                    world.Map.GetTile(chara.Force).Refresh();
                }
            }
            // 序列を更新する。
            world.Countries.UpdateRanking(newCountry);

            // 落城処理との共通化のために簡易的に軍勢を作る。
            var force = new Force(world, betrayers.First(), castle.Tile.Neighbors.RandomPick().Position);
            force.SetDestination(castle);
            force.TileMoveRemainingDays = 0;
            world.Forces.Register(force);

            // 落城処理で問題になるため、一度反乱側のキャラを城から離す。
            foreach (var chara in betrayers.ToList())
            {
                chara.ChangeCastle(castle.Neighbors.First(), false);
            }

            // 落城処理を実行する。
            await world.Forces.OnCastleFall(world, force, castle);
            
            // 所属をもとに戻す。
            foreach (var chara in betrayers.ToList())
            {
                chara.ChangeCastle(castle, false);
            }

            // 反乱側の外交関係を設定する。
            foreach (var c in world.Countries.Where(c => c != newCountry))
            {
                // 旧国とは敵対関係にする。
                if (c == oldCountry)
                {
                    newCountry.SetEnemy(c);
                    continue;
                }

                // 他は旧国の関係値をベースとする。
                var rel = oldCountry.GetRelation(c);
                // 50以上なら反転させる。
                if (rel >= 50)
                {
                    rel = 100 - rel;
                }
                // 50未満なら10だけ改善する。
                else
                {
                    rel = (rel + 10).MaxWith(50);
                }
                newCountry.SetRelation(c, rel);
            }
        }

        public static Country CreateNewCountry(Character actor, WorldData world)
        {
            var newCountry = new Country
            {
                Id = world.Countries.Max(c => c.Id) + 1,
                Ruler = actor,
                Objective = new CountryObjective.StatusQuo(),
                ColorIndex = Enumerable.Range(0, Static.CountrySpriteCount).Except(world.Countries.Select(c => c.ColorIndex)).RandomPick(),
            };
            actor.Country = newCountry;
            world.Countries.Add(newCountry);
            return newCountry;
        }

        public static float BetrayalProbability(Character actor, Character member)
        {
            var prob = 0.5f;
            // 忠誠90以上なら1あたり20%減少
            if (member.Loyalty >= 90) prob -= (member.Loyalty - 90) * 0.2f;
            // 忠誠90以下なら1あたり4%増加
            if (member.Loyalty < 90) prob += (90 - member.Loyalty) * 0.04f;

            // 首謀者が強いなら確率を上げる。
            prob += actor.Power > member.Power ? 0.1f : -0.05f;
            prob += actor.Power > actor.Country.Ruler.Power ? 0.2f : -0.05f;
            prob += actor.TotalCapability > member.TotalCapability ? 0.1f : -0.05f;
            prob += actor.TotalCapability > actor.Country.Ruler.TotalCapability ? 0.2f : -0.05f;

            // 首謀者が国主で、memberのBossまたは隣接城のBossなら確率を上げる
            if (actor.IsRegionBoss && (actor == member.Castle.Boss || actor.Castle.Neighbors.Contains(member.Castle)))
            {
                prob += 0.3f;
            }

            // 忠実さ
            if (member.Fealty > 7) prob *= 1f / (member.Fealty - 6);
            if (member.Fealty < 7) prob *= 1f + (7 - member.Fealty) * 0.1f;
            // 野心
            if (member.Ambition >= 7) prob *= 1f + (member.Ambition - 6) * 0.1f;

            return prob;
        }
    }
}