using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class CastleActions
{
    public CastleActionBase[] Martials => new CastleActionBase[]
    {
        Move,
        HireSoldier,
        TrainSoldiers,
        Rebel,
        BecomIndependent,
    };

    /// <summary>
    /// 指定した場所へ進軍します。
    /// </summary>
    public MoveAction Move { get; } = new();
    public class MoveAction : CastleActionBase
    {
        public override string Label => L["進軍"];
        public override string Description => L["進軍します。"];

        protected override bool CanDoCore(ActionArgs args)
        {
            var chara = args.Actor;
            // すでに軍勢を率いている場合は不可。
            if (chara.IsMoving)
            {
                return false;
            }

            return true;
        }

        public override ActionCost Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            var force = new Force(World, args.Actor, args.TargetCastle.Position);

            var nears = World.Castles
                .Where(c => c != args.TargetCastle)
                .OrderBy(c => c.DistanceTo(args.TargetCastle))
                .Take(3)
                .ToArray();

            force.SetDestination(nears.RandomPick());
            World.Forces.Register(force);

            PayCost(args);
            return default;
        }
    }

    /// <summary>
    /// 兵士を雇います。
    /// </summary>
    public HireSoldierAction HireSoldier { get; } = new();
    public class HireSoldierAction : CastleActionBase
    {
        public override string Label => L["雇兵"];
        public override string Description => L["兵士を雇います。"];

        public override ActionCost Cost(ActionArgs args) => 2;
        protected override bool CanDoCore(ActionArgs args) => args.Actor.Soldiers.HasEmptySlot;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));
            var chara = args.Actor;

            var targetSlot = chara.Soldiers.First(s => s.IsEmptySlot);
            targetSlot.IsEmptySlot = false;
            targetSlot.Level = 1;
            targetSlot.Experience = 0;
            targetSlot.Hp = targetSlot.MaxHp;
            chara.Contribution += 1;

            PayCost(args);

            return default;
        }
    }

    /// <summary>
    /// 兵士を訓練します。
    /// </summary>
    public TrainSoldiersAction TrainSoldiers { get; } = new();
    public class TrainSoldiersAction : CastleActionBase
    {
        public override string Label => L["訓練"];
        public override string Description => L["兵士を訓練します。"];

        public override ActionCost Cost(ActionArgs args)
        {
            var chara = args.Actor;

            var averageLevel = chara.Soldiers.Average(s => s.Level);
            return Mathf.Max(1, (int)averageLevel);
        }

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));
            var chara = args.Actor;

            foreach (var soldier in chara.Soldiers)
            {
                if (soldier.IsEmptySlot) continue;
                soldier.AddExperience(chara);
            }

            PayCost(args);

            return default;
        }
    }

    /// <summary>
    /// 反乱を起こします。
    /// </summary>
    public RebelAction Rebel { get; } = new();
    public class RebelAction : CastleActionBase
    {
        public override string Label => L["反乱"];
        public override string Description => L["反乱を起こします。"];

        public override ActionCost Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }

    /// <summary>
    /// 独立します。
    /// </summary>
    public BecomIndependentAction BecomIndependent { get; } = new();
    public class BecomIndependentAction : CastleActionBase
    {
        public override string Label => L["独立"];
        public override string Description => L["独立します。"];

        public override ActionCost Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }
}
