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
    /// 兵士を雇います。
    /// </summary>
    public HireSoldierAction HireSoldier { get; } = new();
    public class HireSoldierAction : PersonalActionBase
    {
        public override string Label => L["雇兵"];
        public override string Description => L["兵士を雇います。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMoving;

        public override ActionCost Cost(ActionArgs args) => 2;
        protected override bool CanDoCore(ActionArgs args) => args.actor.Soldiers.HasEmptySlot;

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var chara = args.actor;

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
}