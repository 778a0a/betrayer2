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
    /// 放浪時のみ利用可能。他の城に移動する。
    /// </summary>
    public MoveAction Move { get; } = new();
    public class MoveAction : PersonalActionBase
    {
        public override string Label => L["転居"];
        public override string Description => L["放浪時のみ利用可能。他の城に移動する"];

        public override ActionCost Cost(ActionArgs args) => 5;

        protected override bool CanDoCore(ActionArgs args)
        {
            // TODO: 放浪状態の判定と移動先の条件チェックを実装
            return false;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            // TODO: 転居処理を実装

            PayCost(args);

            return default;
        }
    }

}