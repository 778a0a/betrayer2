using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

partial class CastleActions
{
    /// <summary>
    /// ゴールド収入を改善します。
    /// </summary>
    public ImproveGoldIncomeAction ImproveGoldIncome { get; } = new();
    public class ImproveGoldIncomeAction : CastleActionBase
    {
        public override string Description => L["ゴールド収入を改善します。"];

        public override int Cost(Character chara) => 2;

        public override ValueTask Do(Character chara)
        {
            Assert.IsTrue(CanDo(chara));

            var catsle = World.CastleOf(chara);
            var town = catsle.Towns.RandomPick();
            town.GoldIncome += 0.1f * chara.Governing / 100f;

            chara.Contribution += 1;
            PayCost(chara);

            return default;
        }
    }

    /// <summary>
    /// 食料収入を改善します。
    /// </summary>
    public ImproveFoodIncomeAction ImproveFoodIncome { get; } = new();
    public class ImproveFoodIncomeAction : CastleActionBase
    {
        public override string Description => L["食料収入を改善します。"];

        public override int Cost(Character chara) => 2;

        public override ValueTask Do(Character chara)
        {
            Assert.IsTrue(CanDo(chara));

            var catsle = World.CastleOf(chara);
            var town = catsle.Towns.RandomPick();
            town.FoodIncome += 10 * chara.Governing / 100f;

            chara.Contribution += 1;
            PayCost(chara);

            return default;
        }
    }

    /// <summary>
    /// 城の強度を改善します。
    /// </summary>
    public ImproveCastleStrengthAction ImproveCastleStrength { get; } = new();
    public class ImproveCastleStrengthAction : CastleActionBase
    {
        public override string Description => L["城の強度を改善します。"];

        public override int Cost(Character chara) => 3;

        public override ValueTask Do(Character chara)
        {
            Assert.IsTrue(CanDo(chara));

            var castle = World.CastleOf(chara);
            castle.Strength += 0.5f * chara.Governing / 100f;

            chara.Contribution += 1;
            PayCost(chara);

            return default;
        }
    }

    /// <summary>
    /// 兵士を雇います。
    /// </summary>
    public HireSoldierAction HireSoldier { get; } = new();
    public class HireSoldierAction : CastleActionBase
    {
        public override string Description => L["兵士を雇います。"];

        public override int Cost(Character chara) => 2;
        protected override bool CanDoCore(Character chara) => chara.Force.HasEmptySlot;

        public override ValueTask Do(Character chara)
        {
            Assert.IsTrue(CanDo(chara));

            var targetSlot = chara.Force.Soldiers.First(s => s.IsEmptySlot);
            targetSlot.IsEmptySlot = false;
            targetSlot.Level = 1;
            targetSlot.Experience = 0;
            targetSlot.Hp = targetSlot.MaxHp;
            chara.Contribution += 1;

            PayCost(chara);

            return default;
        }
    }

    /// <summary>
    /// 兵士を訓練します。
    /// </summary>
    public TrainSoldiersAction TrainSoldiers { get; } = new();
    public class TrainSoldiersAction : CastleActionBase
    {
        public override string Description => L["兵士を訓練します。"];

        public override int Cost(Character chara)
        {
            var averageLevel = chara.Force.Soldiers.Average(s => s.Level);
            return Mathf.Max(1, (int)averageLevel);
        }

        public override ValueTask Do(Character chara)
        {
            Assert.IsTrue(CanDo(chara));
            foreach (var soldier in chara.Force.Soldiers)
            {
                if (soldier.IsEmptySlot) continue;
                soldier.AddExperience(chara);
            }

            PayCost(chara);

            return default;
        }
    }
}
