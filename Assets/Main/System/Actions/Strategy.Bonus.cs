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

        // 君主なら自国の城、城主なら自分の城でのみ表示する。
        protected override bool VisibleCore(Character actor, GameMapTile tile) =>
            (actor.IsRuler && actor.Country == tile.Castle?.Country) ||
            (actor.IsBoss && actor.Castle.Tile == tile);

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

                // プレーヤーが君主で、本拠地で褒賞を実行しているなら、全臣下をリスト化する。
                // そうでないなら、対象の城のメンバーをリスト化する。
                var cands = actor.IsRuler && selectedCastle == actor.Castle ?
                    actor.Country.Members.Where(c => c != actor) :
                    selectedCastle.Members.Where(c => c != actor);
                var candList = cands
                    .OrderBy(c => c.Loyalty)
                    .ThenByDescending(c => c.Contribution)
                    .ToList();

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
                    },
                    // 実行ボタン押下時
                    async selectedList =>
                    {
                        if (selectedList.Count == 0) return;
                        await SendBonus(selectedList);
                    });

                if (targets == null || targets.Count == 0)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }
            }
            else
            {
                await SendBonus(targets);
            }

            async ValueTask SendBonus(List<Character> targets)
            {
                foreach (var target in targets)
                {
                    if (target == null) continue;
                    if (actor.ActionPoints < APCostUnit) break;

                    var oldLoyalty = target.Loyalty;
                    //target.Contribution += 5;
                    target.Loyalty = (target.Loyalty + 5).MaxWith(110);

                    Debug.Log($"{args.actor.Name} が {target} に褒賞を与えました。(忠誠 {oldLoyalty} -> {target.Loyalty})");

                    actor.ActionPoints -= APCostUnit;

                    if (target.IsPlayer)
                    {
                        await MessageWindow.Show($"{actor.Name} から褒賞を貰いました！");
                    }
                }
            }
        }
    }
}
