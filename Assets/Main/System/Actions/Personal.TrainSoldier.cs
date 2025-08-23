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
    /// 兵士を訓練します。
    /// </summary>
    public TrainSoldiersAction TrainSoldiers { get; } = new();
    public class TrainSoldiersAction : PersonalActionBase
    {
        public override string Label => L["訓練"];
        public override string Description => L["兵士を訓練します。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMoving;

        public override ActionCost Cost(ActionArgs args)
        {
            var chara = args.actor;

            var averageLevel = (float)chara.Soldiers.Average(s => s.Level);
            return (int)(1 + averageLevel / 2);
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var chara = args.actor;

            var drillMasterExists = chara.Castle.Members.Any(m => m.Traits.HasFlag(Traits.Drillmaster));
            var isObjective = chara.Castle.Objective is CastleObjective.Train || chara.Castle.Objective is CastleObjective.Attack;
            foreach (var soldier in chara.Soldiers)
            {
                if (soldier.IsEmptySlot) continue;
                soldier.AddExperience(chara, true, drillMasterExists, isObjective);
            }

            PayCost(args);

            return default;
        }
    }
}