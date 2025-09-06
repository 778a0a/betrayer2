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
        public override string Description => L["臣下に褒賞を与えます。"];

        public ActionArgs Args(Character actor, Character target) => new(actor, targetCharacter: target);

        private const int GoldCostPerTarget = 10;
        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, GoldCostPerTarget);
        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var actor = args.actor;
            var targets = new List<Character>();
            if (args.targetCharacter != null) targets.Add(args.targetCharacter);

            // プレーヤーの場合
            if (actor.IsPlayer)
            {
                var candidateMembers = args.selectedTile.Castle.Members.Except(new[] { actor }).ToList();

                // 複数キャラ選択画面を表示する。
                targets = await UI.SelectCharacterScreen.SelectMultiple(
                    "褒賞を与えるキャラクターを選択してください",
                    "決定",
                    "キャンセル",
                    candidateMembers,
                    _ => true,
                    selectedList =>
                    {
                        var goldCost = selectedList.Count * GoldCostPerTarget;
                        var message = $"コスト: {goldCost} / {actor.Castle.Gold}";
                        var ng = actor.Castle.Gold < goldCost;
                        if (ng)
                        {
                            message += " <color=red>資金不足</color>";
                        }
                        UI.SelectCharacterScreen.labelDescription.text = message;
                        UI.SelectCharacterScreen.buttonConfirm.enabledSelf = !ng;
                    }
                );

                if (targets == null || targets.Count == 0)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }
            }

            var messages = new List<string>();
            foreach (var target in targets)
            {
                if (target == null) continue;
                
                var oldLoyalty = target.Loyalty;
                target.Gold += GoldCostPerTarget;
                target.Contribution += 5;
                target.Loyalty = (target.Loyalty + 10).MaxWith(110);

                Debug.Log($"{args.actor.Name} が {target} に褒賞を与えました。(忠誠 {oldLoyalty} -> {target.Loyalty})");
                
                messages.Add($"{target.Name}: 忠誠度 {oldLoyalty.MaxWith(100)} → {target.Loyalty.MaxWith(100)}");
                
                if (target.IsPlayer)
                {
                    await MessageWindow.Show($"{actor.Name} から褒賞を貰いました！\n所持金 +{GoldCostPerTarget}");
                }
            }

            // PayCost(args); が使えないので自前で処理を行う。
            actor.Castle.Gold -= GoldCostPerTarget * targets.Count;
            actor.ActionPoints -= 1;

            if (actor.IsPlayer && messages.Count > 0)
            {
                var message = $"{targets.Count}名に褒賞を与えました。\n\n" + string.Join("\n", messages);
                await MessageWindow.Show(message);
            }

            actor.Contribution += 5 * targets.Count;
        }
    }
}