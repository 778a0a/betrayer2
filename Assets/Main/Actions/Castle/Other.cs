using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class CastleActions
{
    /// <summary>
    /// 人物一覧を表示します。
    /// </summary>
    public ShowMembersAction ShowMembers { get; } = new();
    public class ShowMembersAction : CastleActionBase
    {
        public override string Label => L["人物一覧"];
        public override string Description => L["人物一覧を表示します。"];

        public override ActionCost Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }
}
