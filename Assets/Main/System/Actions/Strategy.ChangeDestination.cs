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
    /// 出撃中のみ利用可能。進軍先を変更します。
    /// </summary>
    public ChangeDestinationAction ChangeDestination { get; } = new();
    public class ChangeDestinationAction : PersonalActionBase
    {
        public override string Label => L["撤退"];
        public override string Description => L["自身の城へ撤退します。"];
        protected override ActionRequirements Requirements => ActionRequirements.Moving;

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var done = await PersonalActions.ChangeDestinationAction.DoCore(args.actor, args.targetCharacter);
            if (!done) return;

            PayCost(args);
        }
    }
}