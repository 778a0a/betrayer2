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
    /// 勢力を捨てて放浪します
    /// </summary>
    public ResignAction Resign { get; } = new();
    public class ResignAction : StrategyActionBase
    {
        public override string Label => L["放浪"];
        public override string Description => L["勢力を捨てて放浪します"];

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 5, 0);

        public ActionArgs Args(Character actor) => new(actor);

        public override bool CanUIEnable(Character actor)
        {
            return actor.CanPay(Cost(new(actor, estimate: true))) &&
                // 配下がいる場合のみ有効
                (actor.Country?.Members.Where(m => m != actor).Any() ?? false);
        }

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var actor = args.actor;

            // プレーヤーの場合
            if (actor.IsPlayer)
            {
                // 確認する。
                var ok = await MessageWindow.ShowOkCancel("勢力を捨てて放浪します。\nよろしいですか？");
                if (!ok) return;
            }

            // キャラを浪士にする。
            var isRuler = actor.IsRuler;
            var oldCountry = actor.Country;
            actor.ChangeCastle(actor.Castle, true);
            actor.Contribution /= 2;
            actor.IsImportant = false;
            actor.OrderIndex = -1;
            actor.Loyalty = 0;

            // 君主だったの場合は、序列2位を新たな君主にする。
            var additionalMessage = "";
            if (isRuler)
            {
                var newRuler = oldCountry.Members.OrderBy(m => m.OrderIndex).First();
                oldCountry.Ruler = newRuler;

                var actorCap = actor.TotalCapability / 4;
                var newRulerCap = newRuler.TotalCapability / 4;
                // 能力差に応じて忠誠度ペナルティを与える。
                var loyaltyPenaltyBase = actorCap - newRulerCap;
                foreach (var member in oldCountry.Members)
                {
                    var penalty = loyaltyPenaltyBase;
                    
                    // 自分より能力が低い君主になった場合はさらに5減らす。
                    var memberCap = member.TotalCapability / 4;
                    if (memberCap > newRulerCap) penalty += 5;
                    // 戦力が低い場合もさらに5減らす。
                    if (member.Power > newRuler.Power) penalty += 5;
                    // Bossの場合は無条件で10減らす。
                    if (member.IsBoss) penalty += 10;
                    // 野心に応じてさらに減らす。
                    penalty += member.Ambition;

                    member.Loyalty = (member.Loyalty - penalty).Clamp(0, 110);
                }
                Debug.Log($"君主変動 {actor.Name} -> {newRuler.Name} | 忠誠度ペナルティ {loyaltyPenaltyBase:0}");
                additionalMessage = $"\n{newRuler.Name}が新たな君主になりました。";
            }

            Debug.Log($"{actor.Name}が勢力を捨てて放浪しました。");
            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"浪士になりました。{additionalMessage}");
            }
            if (oldCountry.Members.Any(m => m.IsPlayer))
            {
                await MessageWindow.Show($"{actor.Name}が勢力を去りました。{additionalMessage}");
            }

            PayCost(args);
        }
    }

}