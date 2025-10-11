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
    /// 配下を解雇します。
    /// </summary>
    public FireVassalAction FireVassal { get; } = new();
    public class FireVassalAction : StrategyActionBase
    {
        public override string Label => L["解雇"];
        public override string Description => L["配下を解雇します。"];

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 4, 0);

        public ActionArgs Args(Character actor, Character target) =>
            new(actor, targetCharacter: target);

        public override bool Enabled(Character actor, GameMapTile tile)
        {
            return actor.CanPay(Cost(new(actor, estimate: true))) &&
                // 配下がいる場合のみ有効
                (actor.Castle?.Members.Where(m => m != actor).Any() ?? false);
        }

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var actor = args.actor;

            // プレーヤーの場合
            if (actor.IsPlayer)
            {
                // キャラ選択画面を表示する。
                var vassals = actor.Castle.Members.Where(m => m != actor).ToList();
                args.targetCharacter = await UI.SelectCharacterScreen.Show(
                    "解雇するキャラクターを選択してください",
                    "キャンセル",
                    vassals,
                    _ => true
                );

                if (args.targetCharacter == null)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }
            }
            PayCost(args);

            var target = args.targetCharacter;

            // targetがプレーヤーの場合
            var denied = false;
            if (target.IsPlayer)
            {
                // 拒否するか確認する。
                denied = await MessageWindow.ShowYesNo($"{actor.Name}から解雇を通知されました！\n従いますか？");
            }
            // AIの場合
            else
            {
                var denyProb = target.Loyalty > (95 - target.Fealty) ? 0 : 0.5f;
                denyProb += Mathf.Lerp(0, 0.5f, (100 - target.Loyalty) / 50f);
                denyProb += target.Power > actor.Power ? 0.2f : 0;
                denied = denyProb.Chance();
            }

            // 拒否された場合
            if (denied)
            {
                if (actor.IsPlayer)
                {
                    var prevLoyalty = target.Loyalty;
                    target.Loyalty = (target.Loyalty - 50).MinWith(0);
                    await MessageWindow.Show($"{target.Name}は解雇を拒否しました！\n忠誠: {prevLoyalty} → {target.Loyalty}");
                    // 同じ城のキャラの忠誠も下げる。
                    foreach (var mate in actor.Castle.Members.Where(m => m != target && m != actor))
                    {
                        mate.Loyalty = (mate.Loyalty - 3).MinWith(0);
                    }
                }
                return;
            }

            // キャラを浪士にする。
            // 軍勢があれば削除する。
            if (actor.Force != null)
            {
                World.Forces.Unregister(actor.Force);
            }
            target.ChangeCastle(target.Castle, true);
            target.Contribution /= 2;
            target.IsImportant = false;
            target.OrderIndex = -1;
            target.Loyalty = 0;

            Debug.Log($"{target.Name} が {actor.Castle} から解雇されました。");
            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"{target.Name}を解雇しました。");
            }
        }
    }

}