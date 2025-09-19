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
    /// 城のゴールド収入を改善します。
    /// </summary>
    public DevelopAction Develop { get; } = new();
    public class DevelopAction : PersonalActionBase
    {
        public override string Label => L["内政"];
        public override string Description => L["城のゴールド収入を改善します。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMovingAndNotFree;

        public override ActionCost Cost(ActionArgs args) => 2;

        protected override bool CanDoCore(ActionArgs args) => args.actor.Castle.GoldIncome < args.actor.Castle.GoldIncomeMax;

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var chara = args.actor;
            var town = chara.Castle.Towns.Where(t => t.GoldIncome < t.GoldIncomeMax).RandomPick();

            // 25G～40G使うと1G給料(四半期で3G)が増えるので、
            // 30G/3=10G使うと1G収入改善する感じにしてみる。
            // -> 上がりすぎなので15Gぐらいにする

            var adj = 1 + (chara.Governing - 75) / 100f;
            if (chara.Traits.HasFlag(Traits.Merchant)) adj += 0.15f;
            var adjDim = town.GoldImproveAdj;
            var adjImp = chara.IsImportant || chara.IsPlayer ? 1 : 0.5f;
            var adjCount = chara.IsPlayer ? 1 : Mathf.Pow(0.9f, (chara.Castle.Members.Count - 3).MinWith(0));
            var adjBase = 2f / 30 * 3;
            town.GoldIncome = (town.GoldIncome + adj * adjDim * adjImp * adjCount * adjBase).MaxWith(town.GoldIncomeMax);

            // 内政は功績を貯まりやすくする。
            chara.Contribution += adj * 2;
            PayCost(args);

            return default;
        }
    }
}