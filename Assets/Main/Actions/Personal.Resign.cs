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
    /// 勢力を捨てて放浪します。
    /// </summary>
    public ResignAction Resign { get; } = new();
    public class ResignAction : PersonalActionBase
    {
        public override string Label => L["放浪"];
        public override string Description => L["勢力を捨てて放浪します。"];

        public override ActionCost Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }

}