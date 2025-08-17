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

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 10);
        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var actor = args.actor;

            // プレーヤーの場合
            if (actor.IsPlayer)
            {
                // キャラ選択画面を表示する。
                var vassals = actor.IsRuler ?
                    actor.Country.Members.Where(m => m != actor).ToList() :
                    actor.Castle.Members.Where(m => m != actor).ToList();
                args.targetCharacter = await UI.SelectCharacterScreen.Show(
                    "褒賞を与えるキャラクターを選択してください",
                    "キャンセル",
                    vassals,
                    _ => true
                );

                if (args.targetCharacter == null)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }
            }

            PayCost(args);
            var target = args.targetCharacter;
            var gold = Cost(args).castleGold;
            var oldLoyalty = target.Loyalty;
            target.Gold += gold;
            target.Contribution += 5;
            target.Loyalty = (target.Loyalty + 10).MaxWith(110);

            Debug.Log($"{args.actor.Name} が {target} に褒賞を与えました。(忠誠 {oldLoyalty} -> {target.Loyalty})");

            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"{target.Name}に褒賞を与えました。\n忠誠度 {oldLoyalty.MaxWith(100)} -> {target.Loyalty.MaxWith(100)}");
            }
            if (target.IsPlayer)
            {
                await MessageWindow.Show($"{actor.Name} から褒賞を貰いました！\n所持金 +{gold}");
            }

            actor.Contribution += 5;
        }
    }
}