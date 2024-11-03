using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class CastleActions
{
    public CastleActionBase[] Strategies => new CastleActionBase[]
    {
        FireVassal,
        Resign,
    };

    /// <summary>
    /// 配下を解雇します。
    /// </summary>
    public FireVassalAction FireVassal { get; } = new();
    public class FireVassalAction : CastleActionBase
    {
        public override string Label => L["追放"];
        public override string Description => L["配下を解雇します。"];

        public override ActionCost Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }

    /// <summary>
    /// 勢力を捨てて放浪します。
    /// </summary>
    public ResignAction Resign { get; } = new();
    public class ResignAction : CastleActionBase
    {
        public override string Label => L["放浪"];
        public override string Description => L["勢力を捨てて放浪します。"];

        public override ActionCost Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }
}
