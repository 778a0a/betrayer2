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
    /// 他勢力との同盟を破棄します。
    /// </summary>
    public BreakAllianceAction BreakAlliance { get; } = new();
    public class BreakAllianceAction : StrategyActionBase
    {
        public override string Label => L["破棄"];
        public override string Description => L["他勢力との同盟を破棄します。"];
        protected override ActionRequirements Requirements => ActionRequirements.Ruler;

        // 同盟を結んでいる他国の城でのみ表示する。
        protected override bool VisibleCore(Character actor, GameMapTile tile) =>
            actor.Country != tile.Castle?.Country && actor.Country.IsAlly(tile.Castle?.Country);


        public ActionArgs Args(Character actor, Country target) => new(actor, targetCountry: target);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 5, 0);

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
                var ok = await MessageWindow.ShowOkCancel($"{args.targetCountry.Ruler.Name}との同盟を破棄します。\nよろしいですか？");
                if (!ok)
                {
                    Debug.Log("破棄がキャンセルされました。");
                    return;
                }
            }
            PayCost(args);

            var target = args.targetCountry;
            if (target.Members.Any(m => m.IsPlayer))
            {
                await MessageWindow.Show($"{actor.Name}が一方的に同盟を破棄しました！");
            }
            else if (actor.Country.Members.Any(m => m.IsPlayer))
            {
                await MessageWindow.Show($"{target.Ruler.Name}との同盟を破棄しました。");
            }

            args.actor.Country.SetRelation(target, 20);
            Debug.Log($"{args.actor.Country} が {target} との同盟を破棄しました。");
        }
    }

}