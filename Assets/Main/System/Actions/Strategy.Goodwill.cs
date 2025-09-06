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

        public override ActionCost Cost(ActionArgs args)
        {
            if (args.estimate)
            {
                // 推定値の場合は最低コストを返す。
                return ActionCost.Of(0, 1, 10);
            }
            // 自国と他国の城の数に応じて金額を計算する。
            var myCastles = args.actor.Country.Castles.Count;
            var targetCastles = args.targetCountry?.Castles.Count ?? 0;
            var goldCost = (myCastles + targetCastles) * 5;
            return ActionCost.Of(0, 1, goldCost);
        }

        public override bool CanUIEnable(Character actor)
        {
            return actor.CanPay(Cost(new(actor, estimate: true))) &&
                // 他国が存在する場合のみ有効
                World.Countries.Any(c => c != actor.Country);
        }

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var actor = args.actor;

            // プレイヤーの場合は君主選択画面を表示
            if (actor.IsPlayer)
            {
                // 自城で実行された場合は、まず国を選択してもらう。
                if (args.selectedTile.Castle == actor.Castle)
                {
                    // 自国以外の君主一覧を取得する。
                    var otherRulers = World.Countries
                        .Where(c => c != actor.Country)
                        .OrderByDescending(c =>
                        {
                            var rel = actor.Country.GetRelation(c);
                            if (rel > 50) rel += 1000;
                            else if (rel < 50) rel += 500;
                            var isNeighbor = actor.Country.Neighbors.Contains(c);
                            if (isNeighbor) rel += 200;
                            return rel;
                        })
                        .ToList();

                    if (otherRulers.Count == 0)
                    {
                        await MessageWindow.Show("親善を行える相手がいません。");
                        return;
                    }
                    args.targetCountry = await UI.SelectCountryScreen.Show(
                        "親善を行う相手を選択してください",
                        "キャンセル",
                        otherRulers,
                        _ => true
                    );

                    if (args.targetCountry == null)
                    {
                        Debug.Log("キャラクター選択がキャンセルされました。");
                        return;
                    }
                }
                // 自城以外で実行された場合は、その城の国を対象とする。
                else
                {
                    args.targetCountry = args.selectedTile.Castle.Country;
                    var ok = await MessageWindow.ShowOkCancel($"{args.targetCountry.Ruler.Name} と関係改善します。\nよろしいですか？");
                    if (!ok)
                    {
                        Debug.Log("親善がキャンセルされました。");
                        return;
                    }
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
                var message = $"{actor.Name} からゴールドが贈られました。\n金額: {giftAmount}\n受け取りますか？";
                accepted = await MessageWindow.ShowYesNo(message);
            }
            else
            {
                // AIの場合はとりあえず常に受け入れる。
                // TODO 恨みがあれば拒否する？
                accepted = true;
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
                    await MessageWindow.Show($"{target.Ruler.Name} は関係改善を拒否しました。\n関係度: {rel} → {newRel}");
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