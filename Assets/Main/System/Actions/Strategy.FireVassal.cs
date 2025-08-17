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

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        public ActionArgs Args(Character actor, Character target) =>
            new(actor, targetCharacter: target);

        public override bool CanUIEnable(Character actor)
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

            var target = args.targetCharacter;

            // TODO targetがプレーヤーの場合
            // TODO 拒否された場合

            // キャラを浪士にする。
            target.ChangeCastle(target.Castle, true);
            target.Contribution /= 2;
            target.IsImportant = false;
            target.OrderIndex = -1;
            target.Loyalty = 0;
            // TODO 恨み

            Debug.Log($"{target.Name} が {actor.Castle} から解雇されました。");
            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"{target.Name}を解雇しました。");
            }

            PayCost(args);
        }
    }

}