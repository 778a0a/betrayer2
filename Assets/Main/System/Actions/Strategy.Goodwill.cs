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
    /// 他勢力との関係を改善します。
    /// </summary>
    public GoodwillAction Goodwill { get; } = new();
    public class GoodwillAction : StrategyActionBase
    {
        public override string Label => L["親善"];
        public override string Description => L["他勢力との関係を改善します。"];

        public ActionArgs Args(Character actor, Country target) => new(actor, targetCountry: target);

        public override ActionCost Cost(ActionArgs args) =>
            ActionCost.Of(0, 1, args.actor.Country.Castles.Count.MinWith(args.targetCountry.Castles.Count) * 20);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            PayCost(args);

            var target = args.targetCountry;
            target.Ruler.Castle.Gold += Cost(args).castleGold / 2;

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

}