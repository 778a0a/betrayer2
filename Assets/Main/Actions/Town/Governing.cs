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

        public override ActionCost Cost(ActionArgs args) => 2;

        protected override bool CanDoCore(ActionArgs args) => args.targetTown.GoldIncome < args.targetTown.GoldIncomeMax;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));
            var chara = args.actor;
            var town = args.targetTown;

            // 能力値75なら2年で回収できる程度。
            var adj = 1 + (chara.Governing - 75) / 100f;
            if (chara.Traits.HasFlag(Traits.Merchant)) adj += 0.1f;
            var adjDim = town.GoldImproveAdj;
            var adjImp = chara.IsImportant ? 1 : 0.5f;
            var adjCount = Mathf.Pow(0.9f, (chara.Castle.Members.Count - 3).MinWith(0));
            town.GoldIncome = (town.GoldIncome + adj * adjDim * adjImp * adjCount / 8).MaxWith(town.GoldIncomeMax);

            var contribAdj = town.Castle.Objective == CastleObjective.Commerce ? 1.5f : 1;
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
