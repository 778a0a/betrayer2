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
    /// 指定した場所へ援軍として進軍します。
    /// </summary>
    public DeployAsReinforcementAction DeployAsReinforcement { get; } = new();
    public class DeployAsReinforcementAction : StrategyActionBase
    {
        public override string Label => L["援軍"];
        public override string Description => L["援軍として出撃します。"];

        protected override bool VisibleCore(Character actor, GameMapTile tile) => tile.Castle?.CanOrder ?? false;

        public ActionArgs Args(Character actor, Character attacker, Castle target) =>
            new(actor, targetCharacter: attacker, targetCastle: target);

        protected override bool CanDoCore(ActionArgs args)
        {
            var chara = args.targetCharacter;
            if (chara.IsMoving || chara.IsIncapacitated)
            {
                return false;
            }

            return true;
        }

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 3, 0);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var force = new Force(World, args.targetCharacter, args.targetCharacter.Castle.Position, ForceMode.Reinforcement);
            force.ReinforcementOriginalTarget = args.targetCastle;
            force.IsPlayerDirected = args.actor.IsPlayer;
            force.SetDestination(args.targetCastle);
            World.Forces.Register(force);

            Debug.Log($"{force} が援軍として出撃しました。");

            PayCost(args);
            return default;
        }
    }
}