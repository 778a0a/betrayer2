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
    /// 配下を討伐します。
    /// </summary>
    public SubdueAction Subdue { get; } = new();
    public class SubdueAction : StrategyActionBase
    {
        public override string Label => L["討伐"];
        public override string Description => L["配下を討伐し、勢力から追い出します。"];

        protected override ActionRequirements Requirements => ActionRequirements.Boss;

        protected override bool VisibleCore(Character actor, GameMapTile tile) =>
            actor.Castle.Tile == tile;

        public override bool Enabled(Character actor, GameMapTile tile)
        {
            return actor.CanPay(Cost(new(actor, estimate: true))) &&
                // 配下がいる場合のみ有効
                actor.Castle.Members.Where(m => m != actor && !m.IsMoving).Any();
        }

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 4, 0);

        protected override bool CanDoCore(ActionArgs args) => true;

        public ActionArgs Args(Character actor, Character target) =>
            new(actor, targetCharacter: target);

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var actor = args.actor;

            // プレーヤーの場合
            if (actor.IsPlayer)
            {
                // キャラ選択画面を表示する。
                var vassals = actor.Castle.Members.Where(m => m != actor && !m.IsMoving).ToList();
                args.targetCharacter = await UI.SelectCharacterScreen.Show(
                    "討伐する配下を選択してください",
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

            if (target.IsPlayer)
            {
                await MessageWindow.Show($"{actor.Name}があなたを討伐しようとしています！");
            }

            // 野戦を開始する
            // 討伐対象を仮の軍勢として準備する
            var targetForce = new Force(World, target, target.Castle.Position);
            var attackerForce = new Force(World, actor, actor.Castle.Position);

            var battle = BattleManager.PrepareFieldBattle(attackerForce, targetForce);
            battle.Title = $"{target.Name}の討伐";
            var (result, _) = await battle.Do();

            var win = result == BattleResult.AttackerWin;

            // 勝利した場合
            if (win)
            {
                // targetを勢力から追放する（浪士にする）

                target.ChangeCastle(target.Castle, true);
                target.Contribution /= 2;
                target.IsImportant = false;
                target.OrderIndex = -1;
                target.Loyalty = 0;

                Debug.Log($"{target.Name}を討伐し、追放しました。");
                if (actor.IsPlayer)
                {
                    await MessageWindow.Show($"討伐成功！\n{target.Name}を追放しました。");
                }
                else if (target.IsPlayer)
                {
                    await MessageWindow.Show($"勢力を追放されました！");
                }
            }
            // 敗北した場合
            else
            {
                Debug.Log($"{actor.Name}は討伐戦に敗北しました。");
                if (actor.IsPlayer)
                {
                    await MessageWindow.Show($"討伐失敗...\n配下に動揺が広がっています。");
                }

                // 討伐に失敗した場合、対象の忠誠が大幅に下がる
                target.Loyalty = (target.Loyalty - 50).MinWith(0);
            }

            // 同じ城のキャラの忠誠を下げる。
            foreach (var mate in actor.Castle.Members)
            {
                mate.Loyalty = (mate.Loyalty - 5).MinWith(0);
            }
            // 君主の場合は国全体の忠誠も下げる。
            if (actor.IsRuler)
            {
                foreach (var c in World.Characters)
                {
                    c.Loyalty = (c.Loyalty - 5).MinWith(0);
                }
            }
        }
    }
}