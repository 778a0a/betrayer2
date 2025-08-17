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
        public override string Description => L["町に投資を行い、城のゴールド収入上限を増やします。"];

        private const int GoldCost = 10;
        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, castleGold: GoldCost);

        public ActionArgs Args(Character actor) => new(actor);

        protected override bool CanDoCore(ActionArgs args) => true;

        public override ValueTask Do(ActionArgs args)
        {
            // 処理は個人アクション版と共通。
            return PersonalActions.InvestAction.DoCore(this, args);
        }
    }
}