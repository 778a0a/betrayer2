using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class StrategyActions
{
    /// <summary>
    /// 褒賞
    /// </summary>
    public BonusAction Bonus { get; } = new();
    public class BonusAction : StrategyActionBase
    {
        public override string Label => L["褒賞"];
        public override string Description => L["臣下に褒賞を与えます。(右クリックで忠誠下位5人に自動実行)"];

        protected override bool VisibleCore(Character actor, GameMapTile tile) => tile.Castle?.CanOrder ?? false;

        public ActionArgs Args(Character actor, Character target) => new(actor, targetCharacter: target);

        public static readonly int APCostUnit = 2;
        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, APCostUnit, 0);
        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var actor = args.actor;
            var targets = new List<Character>();
            if (args.targetCharacter != null) targets.Add(args.targetCharacter);

            // プレーヤーの場合
            if (actor.IsPlayer)
            {
                var selectedCastle = args.selectedTile?.Castle ?? actor.Castle;

                // プレーヤーが君主が本拠地で褒賞を実行しているなら、全臣下をリスト化する。
                // 国主で本拠地なら周辺の城の臣下もをリスト化する。
                // そうでないなら、対象の城のメンバーをリスト化する。
                var cands = selectedCastle.Members.Where(c => c != actor && c.CanOrder);
                if (actor.IsRuler && selectedCastle == actor.Castle)
                {
                    cands = actor.Country.Members.Where(c => c != actor);
                }
                else if (actor.IsRegionBoss && selectedCastle == actor.Castle)
                {
                    cands = cands.Concat(actor.Castle.Neighbors.Where(c => c.CanOrder).SelectMany(c => c.Members))
                        .Where(c => c != actor && c.CanOrder);
                }
                var candList = cands
                    .OrderBy(c => c.Loyalty)
                    .ThenByDescending(c => c.Contribution)
                    .ToList();

                // 特殊実行の場合は、画面を表示せずに忠誠下位5人に褒賞を与える。
                if (args.isSpecial)
                {
                    var sortedCharas = candList
                        .Where(c => (int)c.Loyalty <= 105)
                        .Take(5)
                        .ToList();
                    if (sortedCharas.Count == 0) return;
                    SendBonus(sortedCharas);
                    Debug.Log($"{actor.Name} が忠誠下位5人に褒賞を与えました。");
                    return;
                }

                // 複数キャラ選択画面を表示する。
                await UI.BonusScreen.Show(
                    actor,
                    candList,
                    // 選択変更時
                    selectedList =>
                    {
                        var apCost = APCostUnit * selectedList.Count;
                        var message = $"APコスト: {apCost} / {actor.ActionPoints}";
                        var ng = apCost > actor.ActionPoints;
                        if (ng)
                        {
                            message += " <color=red>AP不足</color>";
                        }
                        UI.BonusScreen.labelDescription.text = message;
                        UI.BonusScreen.buttonConfirm.enabledSelf = !ng && selectedList.Count > 0;
                        UI.BonusScreen.buttonSelectLowestLoyalty.enabledSelf = APCostUnit <= actor.ActionPoints;
                    },
                    // 実行ボタン押下時
                    selectedList =>
                    {
                        if (selectedList.Count == 0) return default;
                        var processed = SendBonus(selectedList);
                        foreach (var processedTarget in processed)
                        {
                            if (processedTarget != null && !targets.Contains(processedTarget))
                            {
                                targets.Add(processedTarget);
                            }
                        }
                        return default;
                    });

                if (targets == null || targets.Count == 0)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }
            }
            else
            {
                SendBonus(targets);
            }

            List<Character> SendBonus(List<Character> targetList)
            {
                var processed = new List<Character>();
                foreach (var target in targetList)
                {
                    if (target == null) continue;
                    if (actor.ActionPoints < APCostUnit) break;

                    var oldLoyalty = target.Loyalty;
                    //target.Contribution += 5;
                    target.Loyalty = (target.Loyalty + 5).MaxWith(110);

                    //Debug.Log($"{args.actor.Name} が {target} に褒賞を与えました。(忠誠 {oldLoyalty} -> {target.Loyalty})");

                    actor.ActionPoints -= APCostUnit;

                    if (target.IsPlayer)
                    {
                        //await MessageWindow.Show($"{actor.Name} から褒賞を貰いました！");
                    }

                    processed.Add(target);
                }

                return processed;
            }
        }
    }
}
