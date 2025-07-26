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
    /// 町に投資を行い、ゴールド収入上限を増やします。
    /// </summary>
    public InvestAction Invest { get; } = new();
    public class InvestAction : StrategyActionBase
    {
        public override string Label => L["投資"];
        public override string Description => L["ゴールド収入上限を増やします。"];

        private const int GoldCost = 20;
        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, castleGold: GoldCost);

        public ActionArgs Args(Character actor) => new(actor);

        protected override bool CanDoCore(ActionArgs args) => true;

        public override ValueTask Do(ActionArgs args)
        {
            return PersonalActions.InvestAction.DoCore(this, args);
        }
    }
}