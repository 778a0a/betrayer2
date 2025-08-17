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
    /// 別の城へ物資を輸送します。
    /// </summary>
    public TranspotAction Transport { get; } = new();
    public class TranspotAction : StrategyActionBase
    {
        public override string Label => L["輸送"];
        public override string Description => L["別の城へ物資を輸送します。"];

        public ActionArgs Args(Character actor, Castle c, Castle c2, float gold) =>
            new(actor, targetCastle: c, targetCastle2: c2, gold: gold);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        override protected bool CanDoCore(ActionArgs args)
        {
            return args.gold <= args.targetCastle.Gold;
        }

        public override bool CanUIEnable(Character actor)
        {
            return actor.CanPay(Cost(new(actor, estimate: true))) &&
                // 他に拠点が存在する場合のみ有効
                actor.Country.Castles.Any(c => c.Boss != actor);
        }


        public override async ValueTask Do(ActionArgs args)
        {
            var actor = args.actor;

            // プレーヤーの場合
            if (actor.IsPlayer)
            {
                // TODO 専用画面・マップから選択可能にする
                args.gold = 10;
                args.targetCastle = actor.Castle;

                // 自国の他の拠点を取得する。
                var bosses = actor.Country.Castles
                    .Select(c => c.Boss)
                    .Where(m => m != actor)
                    .ToList();
                args.targetCastle2 = (await UI.SelectCharacterScreen.Show(
                    "輸送先を選択してください",
                    "キャンセル",
                    bosses,
                    _ => true
                )).Castle;

                if (args.targetCastle2 == null)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }
            }
            Util.IsTrue(CanDo(args));

            args.targetCastle.Gold -= args.gold;
            args.targetCastle2.Gold += args.gold;
            if (!args.actor.IsRuler)
            {
                args.actor.Contribution += args.gold / 10f;
            }

            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"{args.targetCastle2.Name}へ金{args.gold}を輸送しました。");
            }
            else if (args.targetCastle2.Boss.IsPlayer)
            {
                await MessageWindow.Show($"{args.targetCastle.Name}から金{args.gold}が輸送されました。");
            }

            PayCost(args);
            Debug.Log($"{args.actor.Name} が {args.targetCastle} から {args.targetCastle2} へ {args.gold}G 運びました。");
        }
    }
}