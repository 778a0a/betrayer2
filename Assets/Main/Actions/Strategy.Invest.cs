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

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, countryGold: GoldCost);

        public ActionArgs Args(Character actor) => new(actor);
        public ActionArgs Args(Character actor, Town town) => new(actor, targetTown: town);

        protected override bool CanDoCore(ActionArgs args) => true;

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var chara = args.actor;
            var town = args.targetTown;
            if (town == null)
            {
                var maxInvestment = chara.Castle.Towns.Max(t => t.TotalInvestment);
                town = chara.Castle.Towns.RandomPickWeighted(t => 100 + (maxInvestment - t.TotalInvestment));
            }

            var adj = 1 + (chara.Governing - 75) / 100f;
            if (chara.Traits.HasFlag(Traits.Merchant)) adj += 0.1f;
            town.TotalInvestment += GoldCost * adj;

            var contribAdj = town.Castle.Objective == CastleObjective.Commerce ? 1.5f : 1;
            chara.Contribution += adj * contribAdj;
            PayCost(args);

            return default;
        }
    }
}