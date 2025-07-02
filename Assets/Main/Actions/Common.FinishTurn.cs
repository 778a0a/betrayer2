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
    /// ターンを終了します。
    /// </summary>
    public FinishTurnAction FinishTurn { get; } = new();
    public class FinishTurnAction : CommonActionBase
    {
        public override string Description => L["自分のターンを終了します。"];

        public override async ValueTask Do(ActionArgs args)
        {
            GameCore.Instance.test.hold = false;
        }
    }
}
