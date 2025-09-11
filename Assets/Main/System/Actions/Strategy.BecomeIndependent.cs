using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class StrategyActions
{
    /// <summary>
    /// 独立します。
    /// </summary>
    public BecomeIndependentAction BecomeIndependent { get; } = new();
    public class BecomeIndependentAction : StrategyActionBase
    {
        public override string Label => L["独立"];
        public override string Description => L["独立します。"];

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 10, 0);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }

}