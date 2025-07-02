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
    /// 反乱を起こします。
    /// </summary>
    public RebelAction Rebel { get; } = new();
    public class RebelAction : PersonalActionBase
    {
        public override string Label => L["反乱"];
        public override string Description => L["反乱を起こします。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMovingAndVassalNotBoss;

        public override ActionCost Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }
}