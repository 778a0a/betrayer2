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
    /// 引出
    /// </summary>
    public WithdrawCastleGoldAction WithdrawCastleGold { get; } = new();
    public class WithdrawCastleGoldAction : StrategyActionBase
    {
        public override string Label => L["引出"];
        public override string Description => L["城資金から所持金へゴールドを10移動します。"];

        public ActionArgs Args(Character actor, int gold) => new(actor, gold: gold);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 0, 0);

        override protected bool CanDoCore(ActionArgs args)
        {
            // デフォルトは10とする。
            if (args.gold == 0) args.gold = 10;

            return args.actor.Castle.Gold >= args.gold;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            // デフォルトは10とする。
            if (args.gold == 0) args.gold = 10;

            args.actor.Gold += args.gold;
            args.actor.Castle.Gold -= args.gold;

            PayCost(args);

            Debug.Log($"{args.actor.Name} が城から {args.gold}G 引き出しました。");
            return default;
        }
    }
}