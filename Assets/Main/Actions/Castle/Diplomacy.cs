using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class CastleActions
{
    public CastleActionBase[] Diplomacies => new CastleActionBase[]
    {
        Ally,
    };

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

            var target = args.TargetCountry;
            // TODO 思考処理
            World.Countries.SetRelation(args.Actor.Country, target, Country.AllyRelation);
            Debug.Log($"{args.Actor.Country} と {target} が同盟しました。");

            return default;
        }
    }
}
