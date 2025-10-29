using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

partial class PersonalActions
{
    /// <summary>
    /// 兵士の配置を入れ替えます。
    /// </summary>
    public RearrangeAction Rearrange { get; } = new();
    public class RearrangeAction : PersonalActionBase
    {
        public override string Label => L["再編"];
        public override string Description => L["兵士の配置をランダムに入れ替えます。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMoving;

        public override ActionCost Cost(ActionArgs args) => 1;

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            // 兵士の配列をランダムにシャッフルする。
            args.actor.Soldiers.SoldierArray.ShuffleInPlace();

            PayCost(args);

            return default;
        }
    }
}
