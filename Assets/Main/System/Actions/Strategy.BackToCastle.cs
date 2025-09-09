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
    /// 出撃中のみ利用可能。自身の城に撤退します。
    /// </summary>
    public BackToCastleAction BackToCastle { get; } = new();
    public class BackToCastleAction : PersonalActionBase
    {
        public override string Label => L["撤退"];
        public override string Description => L["自身の城へ撤退します。"];
        protected override ActionRequirements Requirements => ActionRequirements.Moving;

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var done = PersonalActions.BackToCastleAction.DoCore(args.actor, args.targetCharacter);
            if (!done) return default;

            PayCost(args);

            return default;
        }
    }
}