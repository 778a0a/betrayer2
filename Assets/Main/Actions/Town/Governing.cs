using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class TownActions
{
    public TownActionBase[] Governings => new TownActionBase[]
    {
        ImproveGoldIncome,
        ImproveFoodIncome,
        DestroyTown,
    };

    /// <summary>
    /// ゴールド収入を改善します。
    /// </summary>
    public ImproveGoldIncomeAction ImproveGoldIncome { get; } = new();
    public class ImproveGoldIncomeAction : TownActionBase
    {
        public override string Label => L["商業"];
        public override string Description => L["ゴールド収入を改善します。"];

        public override ActionCost Cost(ActionArgs args) => (ActionCost)args.targetTown.GoldImproveCost();

        protected override bool CanDoCore(ActionArgs args) => args.targetTown.GoldIncome < args.targetTown.GoldIncomeMax;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));
            var chara = args.actor;
            var town = args.targetTown;

            // コスト2で能力値50なら3年で回収できる程度。100なら1.5倍の効果で、2年で回収できる程度。
            var adj = 1 + (chara.Governing - 75) / 100f;
            if (chara.Traits.HasFlag(Traits.Merchant)) adj += 0.1f;
            town.GoldIncome = Mathf.Min(town.GoldIncomeMax, town.GoldIncome + adj / 8);

            var contribAdj = town.Castle.Objective == CastleObjective.Commerce ? 1.5f : 1;
            chara.Contribution += adj * contribAdj;
            PayCost(args);

            return default;
        }
    }

    /// <summary>
    /// 食料収入を改善します。
    /// </summary>
    public ImproveFoodIncomeAction ImproveFoodIncome { get; } = new();
    public class ImproveFoodIncomeAction : TownActionBase
    {
        public override string Label => L["開墾"];
        public override string Description => L["食料収入を改善します。"];

        public override ActionCost Cost(ActionArgs args) => (ActionCost)args.targetTown.FoodImproveCost();

        protected override bool CanDoCore(ActionArgs args) => args.targetTown.FoodIncome < args.targetTown.FoodIncomeMax;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));
            var chara = args.actor;
            var town = args.targetTown;

            // コスト2で能力値50なら3年で回収できる程度。100なら1.5倍の効果で、2年で回収できる程度。
            var adj = 1 + (chara.Governing - 75) / 100f;
            if (chara.Traits.HasFlag(Traits.Merchant)) adj += 0.1f;
            town.FoodIncome = Mathf.Min(town.FoodIncomeMax, town.FoodIncome + adj * 50 / 8);

            var contribAdj = town.Castle.Objective == CastleObjective.Agriculture ? 1.5f : 1;
            chara.Contribution += adj * contribAdj;
            PayCost(args);

            return default;
        }
    }

    /// <summary>
    /// 町を破棄します。
    /// </summary>
    public DestroyTownAction DestroyTown { get; } = new();
    public class DestroyTownAction : TownActionBase
    {
        public override string Label => L["破棄"];
        public override string Description => L["町を破棄します。"];

        public override ActionCost Cost(ActionArgs args) => 40;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }
}
