using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class PersonalActions
{
    /// <summary>
    /// 勢力を捨てて放浪します。
    /// </summary>
    public ResignAction Resign { get; } = new();
    public class ResignAction : PersonalActionBase
    {
        public override string Label => L["放浪"];
        public override string Description => L["勢力を捨てて放浪します。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMovingAndVassalNotBoss;

        public override ActionCost Cost(ActionArgs args) => 1;

        public ActionArgs Args(Character chara)
        {
            return new ActionArgs(chara);
        }

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var actor = args.actor;

            if (actor.IsPlayer)
            {
                // 確認する。
                var ok = await MessageWindow.ShowOkCancel("勢力を捨てて放浪します。\nよろしいですか？");
                if (!ok) return;
            }

            // キャラを浪士にする。
            var oldCountry = actor.Country;
            actor.ChangeCastle(actor.Castle, true);
            actor.Contribution /= 2;
            actor.IsImportant = false;
            actor.OrderIndex = -1;
            actor.Loyalty = 0;

            Debug.Log($"{actor.Name}が{oldCountry.Ruler.Name}軍を去りました。");
            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"浪士になりました。");
            }
            if (actor.Castle.Boss.IsPlayer || oldCountry.Ruler.IsPlayer)
            {
                await MessageWindow.Show($"{actor.Name}が勢力を去りました。");
            }

            PayCost(args);
        }
    }
}