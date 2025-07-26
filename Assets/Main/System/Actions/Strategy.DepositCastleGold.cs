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
    /// 預け入れ
    /// </summary>
    public DepositCastleGoldAction DepositCastleGold { get; } = new();
    public class DepositCastleGoldAction : StrategyActionBase
    {
        public override string Label => L["預入"];
        public override string Description => L["所持金から城の軍資金へ資金を移動します。"];

        public ActionArgs Args(Character actor, int gold) => new(actor, gold: gold);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        override protected bool CanDoCore(ActionArgs args)
        {
            return args.actor.Gold >= args.gold;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            args.actor.Gold -= args.gold;
            args.actor.Castle.Gold += args.gold;

            PayCost(args);
            Debug.Log($"{args.actor.Name} が城に {args.gold}G 預け入れました。");
            return default;
        }
    }
}