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
    /// ゴールド収入を改善します。
    /// </summary>
    public DevelopAction Develop { get; } = new();
    public class DevelopAction : PersonalActionBase
    {
        public override string Label => L["商業"];
        public override string Description => L["ゴールド収入を改善します。"];

        public override ActionCost Cost(ActionArgs args) => 2;

        protected override bool CanDoCore(ActionArgs args) => args.targetTown.GoldIncome < args.targetTown.GoldIncomeMax;

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
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
}