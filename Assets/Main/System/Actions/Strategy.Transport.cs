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

        // 君主なら自国の城、城主なら自分の城でのみ表示する。
        protected override bool VisibleCore(Character actor, GameMapTile tile) =>
            (actor.IsRuler && actor.Country == tile.Castle?.Country) ||
            (actor.IsBoss && actor.Castle.Tile == tile);

        public ActionArgs Args(Character actor, Castle c, Castle c2, float gold) =>
            new(actor, targetCastle: c, targetCastle2: c2, gold: gold);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 2, 0);

        override protected bool CanDoCore(ActionArgs args)
        {
            return args.gold <= args.targetCastle.Gold;
        }

        public override bool Enabled(Character actor, GameMapTile tile)
        {
            return actor.CanPay(Cost(new(actor, estimate: true))) &&
                // 他に拠点が存在する場合のみ有効
                actor.Country.Castles.Count > 1;
        }

        public bool NeedPayCost { get; set; } = true;

        public override async ValueTask Do(ActionArgs args)
        {
            var actor = args.actor;

            // プレーヤーの場合
            if (actor.IsPlayer)
            {
                args.targetCastle = args.selectedTile.Castle;

                // 自国の他の拠点を取得する。
                var targetCastles = actor.Country.Castles
                    .Where(c => c != args.targetCastle)
                    .ToList();

                var (castle, amount) = await UI.TransportScreen.Show(
                    targetCastles,
                    args.targetCastle.Gold.MaxWith(10), 
                    args.targetCastle.Gold);

                if (castle == null)
                {
                    Debug.Log("輸送がキャンセルされました。");
                    return;
                }

                args.targetCastle2 = castle;
                args.gold = amount;
            }
            Util.IsTrue(CanDo(args));

            // 輸送を行う。
            args.targetCastle.Gold -= args.gold;
            args.targetCastle2.Gold += args.gold;
            //if (!args.actor.IsRuler)
            //{
            //    args.actor.Contribution += args.gold / 10f;
            //}

            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"{args.targetCastle2.Name}へ金{args.gold}を輸送しました。");
            }
            else if (args.targetCastle2.Boss.IsPlayer)
            {
                await MessageWindow.Show($"{args.targetCastle.Name}から金{args.gold}が輸送されました。");
            }

            if (NeedPayCost)
            {
                PayCost(args);
            }
            Debug.Log($"{args.actor.Name} が {args.targetCastle} から {args.targetCastle2} へ {args.gold}G 運びました。");
        }
    }
}