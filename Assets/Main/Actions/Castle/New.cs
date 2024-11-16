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
            args.actor.Country.SetAlly(target);
            Debug.Log($"{args.actor.Country} と {target} が同盟しました。");

            return default;
        }
    }

    /// <summary>
    /// 他勢力との関係を改善します。
    /// </summary>
    public GoodwillAction Goodwill { get; } = new();
    public class GoodwillAction : CastleActionBase
    {
        public override string Label => L["親善"];
        public override string Description => L["他勢力との関係を改善します。"];

        public ActionArgs Args(Character actor, Country target) => new(actor, targetCountry: target);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 10);

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            var target = args.targetCountry;
            // TODO 思考処理
            if (args.actor.Country.IsAlly(target))
            {
                // TODO
                Debug.Log($"{args.actor.Country} と {target} が関係改善しました（同盟済み）");
            }
            else
            {
                var rel = args.actor.Country.GetRelation(target);
                var newRel = Mathf.Min(Country.AllyRelation - 1, rel + 10);
                args.actor.Country.SetRelation(target, newRel);
                Debug.Log($"{args.actor.Country} と {target} が関係改善しました（{rel} -> {newRel}）");
            }

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

            var force = new Force(World, args.targetCharacter, args.targetCharacter.Castle.Position);

            force.SetDestination(args.targetCastle);
            World.Forces.Register(force);

            Debug.Log($"{force} が出撃しました。");

            PayCost(args);
            return default;
        }
    }

    /// <summary>
    /// 指定した場所へ援軍として進軍します。
    /// </summary>
    public MoveAsReinforcementAction MoveAsReinforcement { get; } = new();
    public class MoveAsReinforcementAction : CastleActionBase
    {
        public override string Label => L["援軍"];
        public override string Description => L["援軍として出撃します。"];

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

            var force = new Force(World, args.targetCharacter, args.targetCharacter.Castle.Position, ForceMode.Reinforcement);

            force.SetDestination(args.targetCastle);
            World.Forces.Register(force);

            Debug.Log($"{force} が援軍として出撃しました。");

            PayCost(args);
            return default;
        }
    }

    /// <summary>
    /// 配下を雇います。
    /// </summary>
    public HireVassalAction HireVassal { get; } = new();
    public class HireVassalAction : CastleActionBase
    {
        public override string Label => L["人材募集"];
        public override string Description => L["配下を雇います。"];

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 10);

        public ActionArgs Args(Character actor, Character target) =>
            new(actor, targetCharacter: target);

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            var chara = args.actor;

            var target = args.targetCharacter;

            //// 対象がプレイヤーの場合は選択肢を表示する。
            //var country = World.CountryOf(chara);
            //if (target.IsPlayer)
            //{
            //    var ok = await UI.ShowRespondJobOfferScreen(country, World);
            //    //UI.HideAllUI();
            //    Util.Todo();
            //    if (!ok)
            //    {
            //        return;
            //    }
            //}

            //// プレーヤの場合は、恨みがあれば断られる場合もある。
            //if (chara.IsPlayer && target.Urami > 0)
            //{
            //    if ((target.Urami / 100f * 10).Chance())
            //    {
            //        await MessageWindow.Show(L["拒否されました。"]);
            //        target.AddUrami(-1);
            //        return;
            //    }
            //}

            var targetCastle = target.Castle;
            target.ChangeCastle(chara.Castle, false);

            Debug.Log($"{chara.Name}: {target.Name}を配下にしました。");

            //if (chara.IsPlayer)
            //{
            //    await MessageWindow.Show(L["{0}を配下にしました。", target.Name]);
            //}

            return default;
        }
    }

}
