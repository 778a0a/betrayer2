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

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 20);
        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var target = args.targetCharacter;

            var oldLoyalty = target.Loyalty;
            target.Gold += 10;
            target.Loyalty = (target.Loyalty + 10).MaxWith(110);
            args.actor.Castle.Gold -= 20;

            PayCost(args);
            Debug.Log($"{args.actor.Name} が {target} に褒賞を与えました。(忠誠 {oldLoyalty} -> {target.Loyalty})");
            return default;
        }
    }
}