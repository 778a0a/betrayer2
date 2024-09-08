using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class TownActions
{
    public TownActionBase[] Governing => new TownActionBase[]
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

        public override int Cost(ActionArgs args) => 2;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));
            var chara = args.Character;
            var town = args.Town;

            town.GoldIncome += 0.1f * chara.Governing / 100f;

            chara.Contribution += 1;
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

        public override int Cost(ActionArgs args) => 2;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));
            var chara = args.Character;
            var town = args.Town;

            town.FoodIncome += 10 * chara.Governing / 100f;

            chara.Contribution += 1;
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

        public override int Cost(ActionArgs args) => 40;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }
}
