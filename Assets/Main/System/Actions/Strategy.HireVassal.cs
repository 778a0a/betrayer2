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

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(10, 0, 0);

        public ActionArgs Args(Character actor, Character target) =>
            new(actor, targetCharacter: target);

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            PayCost(args);

            var actor = args.actor;
            if (actor.IsPlayer)
            {
                var cands = World.Characters
                    .Where(c => c.IsFree && c != actor)
                    .ToArray();
                if (cands.Length == 0)
                {
                    Debug.Log("雇用可能なキャラクターがいません。");
                    return;
                }

                // SelectCharacterPanelでキャラクターを選択
                args.targetCharacter = await UI.SelectCharacterScreen.Show(
                    "雇用するキャラクターを選択してください",
                    "キャンセル",
                    cands,
                    c => c.IsFree
                );

                if (args.targetCharacter == null)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }
            }
            var target = args.targetCharacter;

            target.Contribution /= 2;
            target.IsImportant = false;
            target.OrderIndex = actor.Country.Members.Max(m => m.OrderIndex) + 1;
            target.Loyalty = 80 + target.Fealty * 2;
            target.ChangeCastle(actor.Castle, false);
            Debug.Log($"{target} が {actor.Castle} に採用されました。");
        }
    }
}