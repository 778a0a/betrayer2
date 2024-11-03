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
    /// 他勢力と同盟します。
    /// </summary>
    public AllyAction Ally { get; } = new();
    public class AllyAction : CastleActionBase
    {
        public override string Label => L["同盟"];
        public override string Description => L["他勢力と同盟します。"];

        public ActionArgs Args(Character actor, Country target) => new(actor, targetCountry: target);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 10);

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            var target = args.targetCountry;
            // TODO 思考処理
            World.Countries.SetRelation(args.actor.Country, target, Country.AllyRelation);
            Debug.Log($"{args.actor.Country} と {target} が同盟しました。");

            return default;
        }
    }


    /// <summary>
    /// 指定した場所へ進軍します。
    /// </summary>
    public MoveAction Move { get; } = new();
    public class MoveAction : CastleActionBase
    {
        public override string Label => L["進軍"];
        public override string Description => L["進軍します。"];

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

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            var force = new Force(World, args.targetCharacter, args.actor.Castle.Position);

            force.SetDestination(args.targetCastle);
            World.Forces.Register(force);

            Debug.Log($"{force} が出撃しました。");

            PayCost(args);
            return default;
        }
    }

}
