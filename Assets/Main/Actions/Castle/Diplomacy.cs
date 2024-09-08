using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class CastleActions
{
    public CastleActionBase[] Diplomacy => new CastleActionBase[]
    {
        Ally,
    };

    /// <summary>
    /// 他勢力と同盟します。
    /// </summary>
    public AllyAction Ally { get; } = new();
    public class AllyAction : CastleActionBase
    {
        public override string Label => L["人材募集"];
        public override string Description => L["他勢力と同盟します。"];

        public override int Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }
}
