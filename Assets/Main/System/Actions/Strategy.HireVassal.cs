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
        public override string Label => L["探索"];
        public override string Description => L["配下を雇います。"];

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 8, 0);

        public ActionArgs Args(Character actor, Character target) =>
            new(actor, targetCharacter: target);

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            // 探索は成否にかかわらずコストを消費する。
            PayCost(args);

            var actor = args.actor;
            if (actor.IsPlayer)
            {
                // ランダムに所属なしのキャラを選ぶ。
                var frees = World.Characters.Where(c => c.IsFree).ToList();
                var candidates = new List<Character>();
                var candCount = (int)MathF.Max(1, MathF.Ceiling(actor.Intelligence / 10) - 5);
                for (int i = 0; i < candCount; i++)
                {
                    if (frees.Count == 0) break;
                    var cand = frees.RandomPick();
                    candidates.Add(cand);
                    frees.Remove(cand);
                }

                if (candidates.Count == 0)
                {
                    await MessageWindow.Show("雇用可能な人材が見つかりませんでした。");
                    return;
                }

                // プレーヤーに選択させる。
                args.targetCharacter = await UI.SelectCharacterScreen.Show(
                    "採用するキャラクターを選択してください",
                    "採用しない",
                    candidates,
                    _ => true
                );

                if (args.targetCharacter == null)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }
            }

            // TODO targetがプレーヤーの場合

            var target = args.targetCharacter;
            target.IsImportant = false;
            target.OrderIndex = actor.Country.Members.Max(m => m.OrderIndex) + 1;
            target.Loyalty = 80 + target.Fealty * 2;
            target.ChangeCastle(actor.Castle, false);
            Debug.Log($"{target} が {actor.Castle} に採用されました。");
        }
    }
}