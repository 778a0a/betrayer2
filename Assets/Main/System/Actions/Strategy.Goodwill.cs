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
        protected override ActionRequirements Requirements => ActionRequirements.Ruler;

        // 他国の城でのみ表示する。
        protected override bool VisibleCore(Character actor, GameMapTile tile) =>
            tile.Castle != null &&
            actor.Country != tile.Castle?.Country;

        public ActionArgs Args(Character actor, Country target) => new(actor, targetCountry: target);

        public override ActionCost Cost(ActionArgs args)
        {
            Debug.Log($"GoodwillAction.Cost: {args}");
            var targetCountry = args.targetCountry ?? args.selectedTile?.Castle?.Country;
            if (targetCountry == null) return ActionCost.Variable;

            // 自国と他国の城の数に応じて金額を計算する。
            var myCastles = args.actor.Country.Castles.Count;
            var targetCastles = targetCountry.Castles.Count;
            var goldCost = (myCastles + targetCastles) * 5;
            return ActionCost.Of(0, 5, goldCost);
        }

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
                var ok = await MessageWindow.ShowOkCancel($"{args.targetCountry.Ruler.Name}と関係改善します。\nよろしいですか？");
                if (!ok)
                {
                    Debug.Log("親善がキャンセルされました。");
                    return;
                }
            }

            var target = args.targetCountry;
            // 受け取る金額は支払う金額の半分とする。
            var giftAmount = Cost(args).castleGold / 2;

            // targetがプレイヤーの場合
            var accepted = true;
            if (target.Ruler.IsPlayer)
            {
                // プレイヤーに選択させる。
                var message = $"{actor.Name}からゴールドが贈られました。\n金額: {giftAmount}\n受け取りますか？";
                accepted = await MessageWindow.ShowYesNo(message);
            }
            // AIの場合
            else
            {
                // 友好度50以下の場合は、1につき2%の確率で拒否される。
                var relation = target.GetRelation(actor.Country);
                var prob = (relation >= 50) ? 1 : Mathf.Clamp01(relation * 2 / 100f) + 0.01f;
                accepted = prob.Chance();
                Debug.Log($"{actor.Name}->{target.Ruler.Name} 親善受諾確率: {prob} ({relation})");
            }

            // 拒否された場合は関係悪化して終了。
            if (!accepted)
            {
                var rel = actor.Country.GetRelation(target);
                var newRel = (rel - 5).MinWith(0);
                actor.Country.SetRelation(target, newRel);

                Debug.Log($"{actor.Country.Ruler.Name} が {target.Ruler.Name} に贈り物を贈りましたが拒否されました。（{rel} -> {newRel}）");
                if (actor.IsPlayer)
                {
                    await MessageWindow.Show($"{target.Ruler.Name}は関係改善を拒否しました。\n関係度: {rel} → {newRel}");
                }
                // コストはAPのみにする。
                actor.ActionPoints -= Cost(args).actionPoints;
                return;
            }

            // コストを支払う
            PayCost(args);
            // 相手国に贈り物として一部のゴールドを加算する。
            target.Ruler.Castle.Gold += giftAmount;

            // 同盟済みの場合
            if (actor.Country.IsAlly(target))
            {
                Debug.Log($"{actor.Country.Ruler.Name} と {target.Ruler.Name} が関係改善しました（既に同盟済み）");
                if (actor.IsPlayer)
                {
                    await MessageWindow.Show($"{target.Ruler.Name}との関係が改善しました。\n（既に同盟済み）");
                }
            }
            // 同盟未満の場合
            else
            {
                var rel = actor.Country.GetRelation(target);
                var newRel = Mathf.Min(Country.AllyRelation - 1, rel + 10);
                actor.Country.SetRelation(target, newRel);
                
                Debug.Log($"{actor.Country.Ruler.Name} と {target.Ruler.Name} が関係改善しました（{rel} -> {newRel}）");
                
                if (actor.IsPlayer)
                {
                    var message = $"{target.Ruler.Name}との関係が改善しました。\n関係度: {rel} → {newRel}";
                    await MessageWindow.Show(message);
                }
            }
        }
    }

}