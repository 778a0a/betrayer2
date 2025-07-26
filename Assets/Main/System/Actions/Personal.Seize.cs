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
    /// 放浪時のみ利用可能。城を攻撃して成功すれば勢力を旗揚げする。
    /// </summary>
    public SeizeAction Seize { get; } = new();
    public class SeizeAction : PersonalActionBase
    {
        public override string Label => L["奪取"];
        public override string Description => L["城を攻撃して勢力を旗揚げします。"];
        protected override ActionRequirements Requirements => ActionRequirements.Free;

        public override ActionCost Cost(ActionArgs args) => 5;

        protected override bool CanDoCore(ActionArgs args)
        {
            // TODO: 放浪状態の判定と攻撃対象の条件チェックを実装
            return false;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            // TODO: 奪取処理を実装

            PayCost(args);

            return default;
        }
    }

}