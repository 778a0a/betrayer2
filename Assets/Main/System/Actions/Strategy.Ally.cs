using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;

partial class StrategyActions
{
    /// <summary>
    /// 他勢力と同盟します。
    /// </summary>
    public AllyAction Ally { get; } = new();
    public class AllyAction : StrategyActionBase
    {
        public override string Label => L["同盟"];
        public override string Description => L["他勢力と同盟します。"];
        protected override ActionRequirements Requirements => ActionRequirements.Ruler;

        // 同盟を結んでいない他国の城でのみ表示する。
        protected override bool VisibleCore(Character actor, GameMapTile tile) =>
            actor.Country != tile.Castle?.Country &&
            !actor.Country.IsAlly(tile.Castle?.Country);

        public ActionArgs Args(Character actor, Country target) => new(actor, targetCountry: target);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 10, 30);

        public override bool Enabled(Character actor, GameMapTile tile)
        {
            return actor.CanPay(Cost(new(actor, estimate: true))) &&
                // 他国が存在する場合のみ有効
                World.Countries.Any(c => c != actor.Country);
        }

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var actor = args.actor;

            // プレイヤーの場合
            if (actor.IsPlayer)
            {
                // 対象の国がセットされているのでその国を取得する。
                args.targetCountry = args.selectedTile.Castle.Country;
                var ok = await MessageWindow.ShowOkCancel($"{args.targetCountry.Ruler.Name}に同盟を申し込みます。\nよろしいですか？");
                if (!ok)
                {
                    Debug.Log("同盟がキャンセルされました。");
                    return;
                }
            }

            // 成否にかかわらずコストを消費する。
            PayCost(args);

            var target = args.targetCountry;

            var accepted = true;
            // targetがプレイヤーの場合
            if (target.Ruler.IsPlayer)
            {
                // プレイヤーに選択させる。
                var message = $"{actor.Name}から同盟を申し込まれました。\n受諾しますか？";
                accepted = await MessageWindow.ShowYesNo(message);
            }
            // AIの場合
            else
            {
                var rel = actor.Country.GetRelation(target);
                var actorAllyCount = World.Countries.Where(c => c != actor.Country).Count(c => c.IsAlly(actor.Country));
                var targetAllyCount = World.Countries.Where(c => c != target).Count(c => c.IsAlly(target));
                var tooFar = !target.Neighbors.Concat(target.Neighbors.SelectMany(n => n.Neighbors)).Contains(actor.Country);
                var prob = (rel - actorAllyCount * 10) / (1 + targetAllyCount) / (tooFar ? 2 : 1) / (rel <= 50 ? 5 : 1) / 100;
                Debug.Log($"{actor.Name}->{target.Ruler.Name} 同盟受諾確率: {prob} ({rel})");
                accepted = prob.Chance();
            }

            // 拒否された場合は関係悪化して終了。
            if (!accepted)
            {
                var rel = actor.Country.GetRelation(target);
                var newRel = (rel - 10).MinWith(0);
                actor.Country.SetRelation(target, newRel);

                Debug.Log($"{actor.Country.Ruler.Name} が {target.Ruler.Name} に同盟を申し込みましたが拒否されました。\n（{rel} -> {newRel}）");
                if (actor.IsPlayer)
                {
                    await MessageWindow.Show($"{target.Ruler.Name} は同盟を拒否しました。\n関係度: {rel} → {newRel}");
                }
                return;
            }
            
            args.actor.Country.SetAlly(target);
            Debug.Log($"{args.actor.Country} と {target} が同盟しました。");
            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"{target.Ruler.Name}と同盟を結びました。");
            }
        }
    }

}