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
    /// 別の城へ物資を輸送します。
    /// </summary>
    public TranspotAction Transpot { get; } = new();
    public class TranspotAction : StrategyActionBase
    {
        public override string Label => L["輸送"];
        public override string Description => L["別の城へ物資を輸送します。"];

        public ActionArgs Args(Character actor, Castle c, Castle c2, float gold) =>
            new(actor, targetCastle: c, targetCastle2: c2, gold: gold);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        override protected bool CanDoCore(ActionArgs args)
        {
            return args.gold <= args.targetCastle.Gold;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            args.targetCastle.Gold -= args.gold;
            args.targetCastle2.Gold += args.gold;
            if (!args.actor.IsRuler)
            {
                args.actor.Contribution += args.gold / 10f;
            }

            PayCost(args);
            Debug.Log($"{args.actor.Name} が {args.targetCastle} から {args.targetCastle2} へ {args.gold}G 運びました。");
            return default;
        }
    }
}