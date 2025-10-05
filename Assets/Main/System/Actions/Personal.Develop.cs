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
            var town = chara.Castle.Towns.RandomPickWeighted(t => chara.Castle.GoldIncomeMax - t.GoldIncome);

            // 25G～40G使うと1G給料(四半期で3G)が増えるので、
            // 30G/3=10G使うと1G収入改善する感じにしてみる。
            // -> 上がりすぎなので15Gぐらいにする

            var adj = 1 + (chara.Governing - 75) / 100f;
            if (chara.Traits.HasFlag(Traits.Merchant)) adj += 0.15f;
            var adjDim = 1 + 0.5f * (chara.Castle.GoldIncomeMax - chara.Castle.GoldIncome) / (float)chara.Castle.GoldIncomeMax;
            var adjDim2 = 1 + (0.5f * ((chara.Castle.GoldIncomeMax / 2) - town.GoldIncome) / (chara.Castle.GoldIncomeMax / 2f)).MinWith(0);
            var adjImp = chara.IsImportant || chara.IsPlayer ? 1 : 0.8f;
            var adjCount = chara.IsPlayer ? 1 : Mathf.Pow(0.9f, (chara.Castle.Members.Count - 3).MinWith(0));
            var adjBase = 3f / 30 * 3;
            town.GoldIncome += adj * adjDim * adjDim2 * adjImp * adjCount * adjBase;

            var overAmount = chara.Castle.GoldIncome - chara.Castle.GoldIncomeMax;
            if (overAmount > 0)
            {
                town.GoldIncome -= overAmount;
                Debug.Log($"{chara.Name} の内政で {chara.Castle.Name} の収入が上限を超えたため、{overAmount} 減少しました。");
            }

            // 内政は功績を貯まりやすくする。
            var contribAdj = chara.Salary < 30 ? 2 : 0.5f;
            chara.Contribution += adjImp * adj * contribAdj;
            PayCost(args);

            return default;
        }
    }
}