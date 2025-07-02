using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class CommonActions
{
    /// <summary>
    /// 自分のフェイズを終了します。
    /// </summary>
    public FinishTurnAction FinishTurn { get; } = new();
    public class FinishTurnAction : CommonActionBase
    {
        public override string Description => L["次のフェイズに進みます。"];

        public override async ValueTask Do(ActionArgs args)
        {
            //Test.Instance.hold = false;
        }
    }
}
