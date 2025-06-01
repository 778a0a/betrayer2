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
    /// 配下を雇います。
    /// </summary>
    public HireVassalAction HireVassal { get; } = new();
    public class HireVassalAction : StrategyActionBase
    {
        public override string Label => L["人材募集"];
        public override string Description => L["配下を雇います。"];

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 10);

        public ActionArgs Args(Character actor, Character target) =>
            new(actor, targetCharacter: target);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            PayCost(args);

            var chara = args.actor;

            var target = args.targetCharacter;

            //// 対象がプレイヤーの場合は選択肢を表示する。
            //var country = World.CountryOf(chara);
            //if (target.IsPlayer)
            //{
            //    var ok = await UI.ShowRespondJobOfferScreen(country, World);
            //    //UI.HideAllUI();
            //    Util.Todo();
            //    if (!ok)
            //    {
            //        return;
            //    }
            //}

            //// プレーヤの場合は、恨みがあれば断られる場合もある。
            //if (chara.IsPlayer && target.Urami > 0)
            //{
            //    if ((target.Urami / 100f * 10).Chance())
            //    {
            //        await MessageWindow.Show(L["拒否されました。"]);
            //        target.AddUrami(-1);
            //        return;
            //    }
            //}

            var targetCastle = target.Castle;
            target.ChangeCastle(chara.Castle, false);

            Debug.Log($"{chara.Name}: {target.Name}を配下にしました。");

            //if (chara.IsPlayer)
            //{
            //    await MessageWindow.Show(L["{0}を配下にしました。", target.Name]);
            //}

            return default;
        }
    }
}