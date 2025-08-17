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

        public ActionArgs Args(Character actor, Country target) => new(actor, targetCountry: target);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 10);

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
                // TODO 専用画面・マップから選択可能にする

                // 自国以外の君主一覧を取得する。
                var otherRulers = World.Countries
                    .Where(c => c != actor.Country)
                    .Select(c => c.Ruler)
                    .ToList();

                if (otherRulers.Count == 0)
                {
                    await MessageWindow.Show("同盟を行える相手がいません。");
                    return;
                }
                var targetRuler = await UI.SelectCharacterScreen.Show(
                    "同盟を行う相手を選択してください",
                    "キャンセル",
                    otherRulers,
                    _ => true
                );

                if (targetRuler == null)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }

                args.targetCountry = targetRuler.Country;
            }

            // 成否にかかわらずコストを消費する。
            PayCost(args);

            var target = args.targetCountry;

            var accepted = true;
            // targetがプレイヤーの場合
            if (target.Ruler.IsPlayer)
            {
                // プレイヤーに選択させる。
                var message = $"{target.Ruler.Name} から同盟を申し込まれました。受諾しますか？";
                accepted = await MessageWindow.ShowYesNo(message);
            }
            // AIの場合
            else
            {
                var rel = actor.Country.GetRelation(target);
                var prob = Mathf.Pow((rel - 50) / 50, 2);
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
                await MessageWindow.Show($"{target.Ruler.Name} と同盟を結びました。");
            }
        }
    }

}