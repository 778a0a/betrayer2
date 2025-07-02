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
    /// 勝手に出撃する。隣接する他国の城を攻撃する。
    /// </summary>
    public DeployAction Deploy { get; } = new();
    public class DeployAction : PersonalActionBase
    {
        public override string Label => L["出撃"];
        public override string Description => L["勝手に出撃します。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMovingAndVassalNotBoss;

        public override ActionCost Cost(ActionArgs args) => 5;

        protected override bool CanDoCore(ActionArgs args)
        {
            return true;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            // TODO: 出撃処理を実装

            PayCost(args);

            return default;
        }
    }

}