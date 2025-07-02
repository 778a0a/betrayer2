using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class PersonalActions
{
    /// <summary>
    /// 町に投資を行い、ゴールド収入上限を増やします。
    /// </summary>
    public InvestAction Invest { get; } = new();
    public class InvestAction : PersonalActionBase
    {
        public override string Label => L["投資"];
        public override string Description => L["町に投資を行い、城のゴールド収入上限を増やします。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMovingAndNotFree;

        private const int GoldCost = 5;
        public override ActionCost Cost(ActionArgs args) => GoldCost;

        public ActionArgs Args(Character actor) => new(actor);

        protected override bool CanDoCore(ActionArgs args) => true;

        public override ValueTask Do(ActionArgs args)
        {
            return DoCore(this, args);
        }

        public static ValueTask DoCore(ActionBase action, ActionArgs args)
        {
            Util.IsTrue(action.CanDo(args));
            var chara = args.actor;
            // ランダムに町を選ぶ。
            var maxInvestment = chara.Castle.Towns.Max(t => t.TotalInvestment);
            var town = chara.Castle.Towns.RandomPickWeighted(t => 100 + (maxInvestment - t.TotalInvestment));

            var adj = 1 + (chara.Governing - 75) / 100f;
            if (chara.Traits.HasFlag(Traits.Merchant)) adj += 0.1f;
            var adjTerrain = GameCore.Instance.World.Map.GetTile(town).Terrain switch
            {
                Terrain.River or Terrain.LargeRiver => 0.2f,
                Terrain.Mountain => 0.5f,
                Terrain.Hill => 0.8f,
                Terrain.Forest => 0.75f,
                _ => 1
            };
            town.TotalInvestment += GoldCost * adj;

            var contribAdj = town.Castle.Objective == CastleObjective.Commerce ? 1.5f : 1;
            chara.Contribution += adj * contribAdj;
            action.PayCost(args);

            return default;
        }
    }
}